# PostgreSQL vs. Oracle Database Engineering Reference

## 1. Dialect & Feature Comparison Matrix

| Feature | PostgreSQL | Oracle Database | ANSI / Cross-Database Best Practice |
| :--- | :--- | :--- | :--- |
| **UUID / GUID Storage** | `UUID` (native 16-byte) | `RAW(16)` (`SYS_GUID()`) | Map `Guid` via EF Core provider defaults |
| **Boolean Storage** | `BOOLEAN` (`true`/`false`) | `NUMBER(1)` (`1`/`0`) or `BOOLEAN` (23ai+) | Let EF Core map `bool` automatically |
| **String Types** | `VARCHAR(n)` / `TEXT` | `VARCHAR2(n CHAR)` / `CLOB` | Always specify `MaxLength` in EF Core Fluent API |
| **Null Coalescing** | `COALESCE(a, b)` | `COALESCE(a, b)` or `NVL(a, b)` | Always use ANSI `COALESCE(a, b)` |
| **Current UTC Timestamp** | `NOW()` or `CURRENT_TIMESTAMP` | `SYSTIMESTAMP AT TIME ZONE 'UTC'` | Pass `TimeProvider.GetUtcNow()` from .NET |
| **Pagination** | `LIMIT n OFFSET m` or `OFFSET m ROWS FETCH NEXT n ROWS ONLY` | `OFFSET m ROWS FETCH NEXT n ROWS ONLY` | Use ANSI `OFFSET ... FETCH NEXT` |
| **Empty String Handling** | `''` is distinct from `NULL` | `''` is treated as `NULL` in `VARCHAR2`! | Never rely on empty string vs `NULL` distinction in Oracle |
| **Identifier Case** | Unquoted identifiers fold to **lowercase** | Unquoted identifiers fold to **UPPERCASE** | Avoid quoted case-sensitive identifiers where possible |

---

## 2. Stored Procedures & Functions Comparison

### PostgreSQL Function (PL/pgSQL)
```sql
CREATE OR REPLACE FUNCTION get_company_application_stats(p_company_id UUID)
RETURNS TABLE (
    total_applications BIGINT,
    active_interviews BIGINT,
    max_offered_salary NUMERIC(12, 2)
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT
        COUNT(*)::BIGINT AS total_applications,
        COUNT(*) FILTER (WHERE ja.status = 'Interviewing')::BIGINT AS active_interviews,
        MAX(od.base_salary) AS max_offered_salary
    FROM job_applications ja
    LEFT JOIN offer_details od ON od.job_application_id = ja.id
    WHERE ja.company_id = p_company_id;
END;
$$;
```

### Oracle Stored Procedure (PL/SQL)
```sql
CREATE OR REPLACE PROCEDURE get_company_application_stats (
    p_company_id IN RAW,
    p_result OUT SYS_REFCURSOR
)
AS
BEGIN
    OPEN p_result FOR
        SELECT
            COUNT(*) AS total_applications,
            SUM(CASE WHEN ja.status = 'Interviewing' THEN 1 ELSE 0 END) AS active_interviews,
            MAX(od.base_salary) AS max_offered_salary
        FROM job_applications ja
        LEFT JOIN offer_details od ON od.job_application_id = ja.id
        WHERE ja.company_id = p_company_id;
END;
/
```

---

## 3. Locking & Transaction Isolation Levels
- **Read Committed**: Default in both PostgreSQL and Oracle. Readers never block writers and writers never block readers (via MVCC in PostgreSQL and UNDO segments in Oracle).
- **Repeatable Read / Serializable**:
  - PostgreSQL `REPEATABLE READ` and `SERIALIZABLE` use Snapshot Isolation / Serializable Snapshot Isolation (SSI) and may throw serialization failure (`SQLSTATE 40001`), which requires retry logic (`EnableRetryOnFailure()` in EF Core).
  - Oracle supports `READ COMMITTED` and `SERIALIZABLE` (`ORA-08177: can't serialize access for this transaction`).
- **Pessimistic Row Locking**:
  - Both support `SELECT ... FOR UPDATE` and `SELECT ... FOR UPDATE SKIP LOCKED` (ideal for background job queues).
