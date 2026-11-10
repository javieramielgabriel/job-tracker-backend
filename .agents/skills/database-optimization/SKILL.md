---
name: database-optimization
description: >-
  Use this skill when designing relational database schemas, writing raw or
  analytical SQL, creating indexes/constraints, analyzing query execution plans,
  or optimizing database performance for PostgreSQL and Oracle Database.
  Provides side-by-side PostgreSQL and Oracle syntax, indexing strategies, CTEs,
  window functions, stored procedures, locking, and isolation levels.
---

# Database Development and Optimization (`database-optimization`)

## Overview
Provides relational database engineering, SQL authoring, and performance optimization expertise for **PostgreSQL** and **Oracle Database** in the **Job Tracker Backend**, explicitly accounting for SQL dialect differences and EF Core provider constraints.

## Activation Instructions
Activate this skill whenever you are:
- Designing relational tables, normalization (3NF), primary/foreign keys, unique/check constraints, and composite/partial/function-based indexes.
- Writing or reviewing SQL queries (`SELECT`, `INSERT`, `UPDATE`, `DELETE`, `JOIN`, CTEs, window functions, aggregations, views, functions, or stored procedures).
- Analyzing query execution plans (`EXPLAIN ANALYZE` in PostgreSQL or `DBMS_XPLAN.DISPLAY_CURSOR` in Oracle), fixing slow queries, optimizing pagination, or tuning transaction isolation levels and locks.

## Specific Responsibilities
1. **Respect Dialect Boundaries**: Never assume PostgreSQL-specific syntax (`ILIKE`, `LIMIT/OFFSET`, `RETURNING`, `UUID`, `JSONB`, `::` casts) works in Oracle, or that Oracle-specific syntax (`SYSDATE`, `NVL`, `ROWNUM`, `DUAL`, `CONNECT BY`, `VARCHAR2`, `NUMBER`) works in PostgreSQL.
2. **Design Enforced Relational Integrity**: Define Primary Keys, Foreign Keys with appropriate `ON DELETE` rules, `UNIQUE` constraints, and `CHECK` constraints at the database level—not just in C# code.
3. **Optimize Indexing & Execution Plans**: Create targeted B-Tree indexes on foreign keys, filter predicates, and sort columns; eliminate unnecessary full table scans on large tables; and prefer keyset (seek) pagination for deep pages.

---

## Technology-Specific Implementation Guidance

### 1. Relational Schema Design & Constraints (PostgreSQL vs. Oracle)

#### PostgreSQL DDL
```sql
CREATE TABLE companies (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(150) NOT NULL,
    website_url VARCHAR(500),
    industry VARCHAR(100),
    created_at_utc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_companies_name UNIQUE (name)
);

CREATE TABLE job_applications (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id UUID NOT NULL,
    job_title VARCHAR(150) NOT NULL,
    status VARCHAR(32) NOT NULL,
    work_mode VARCHAR(32) NOT NULL,
    salary_min NUMERIC(12, 2),
    salary_max NUMERIC(12, 2),
    applied_at_utc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at_utc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT fk_job_applications_company
        FOREIGN KEY (company_id) REFERENCES companies (id) ON DELETE RESTRICT,
    CONSTRAINT chk_job_applications_salary_range
        CHECK (salary_min IS NULL OR salary_max IS NULL OR salary_max >= salary_min),
    CONSTRAINT chk_job_applications_status
        CHECK (status IN ('Draft', 'Applied', 'Interviewing', 'Offered', 'Accepted', 'Rejected', 'Withdrawn'))
);

-- Composite & Partial Indexes (PostgreSQL)
CREATE INDEX ix_job_applications_company_id ON job_applications (company_id);
CREATE INDEX ix_job_applications_status_applied_at
    ON job_applications (status, applied_at_utc DESC)
    INCLUDE (job_title, company_id, salary_max);
```

#### Oracle Database DDL (19c / 21c / 23ai)
```sql
CREATE TABLE companies (
    id RAW(16) DEFAULT SYS_GUID() NOT NULL,
    name VARCHAR2(150 CHAR) NOT NULL,
    website_url VARCHAR2(500 CHAR),
    industry VARCHAR2(100 CHAR),
    created_at_utc TIMESTAMP WITH TIME ZONE DEFAULT SYSTIMESTAMP NOT NULL,
    CONSTRAINT pk_companies PRIMARY KEY (id),
    CONSTRAINT uq_companies_name UNIQUE (name)
);

CREATE TABLE job_applications (
    id RAW(16) DEFAULT SYS_GUID() NOT NULL,
    company_id RAW(16) NOT NULL,
    job_title VARCHAR2(150 CHAR) NOT NULL,
    status VARCHAR2(32 CHAR) NOT NULL,
    work_mode VARCHAR2(32 CHAR) NOT NULL,
    salary_min NUMBER(12, 2),
    salary_max NUMBER(12, 2),
    applied_at_utc TIMESTAMP WITH TIME ZONE DEFAULT SYSTIMESTAMP NOT NULL,
    updated_at_utc TIMESTAMP WITH TIME ZONE DEFAULT SYSTIMESTAMP NOT NULL,
    CONSTRAINT pk_job_applications PRIMARY KEY (id),
    CONSTRAINT fk_job_applications_company
        FOREIGN KEY (company_id) REFERENCES companies (id),
    CONSTRAINT chk_job_applications_salary_range
        CHECK (salary_min IS NULL OR salary_max IS NULL OR salary_max >= salary_min),
    CONSTRAINT chk_job_applications_status
        CHECK (status IN ('Draft', 'Applied', 'Interviewing', 'Offered', 'Accepted', 'Rejected', 'Withdrawn'))
);

CREATE INDEX ix_job_apps_company_id ON job_applications (company_id);
CREATE INDEX ix_job_apps_status_applied ON job_applications (status, applied_at_utc DESC);
```

### 2. CTEs, Window Functions & Aggregations (ANSI SQL Compatible)
Use standard CTEs (`WITH`) and window functions (`ROW_NUMBER()`, `RANK()`, `AVG() OVER (...)`) that work cleanly across both PostgreSQL and Oracle:
```sql
WITH ranked_applications AS (
    SELECT
        ja.id,
        ja.job_title,
        ja.status,
        ja.salary_max,
        ja.applied_at_utc,
        c.name AS company_name,
        c.industry,
        ROW_NUMBER() OVER (
            PARTITION BY ja.company_id
            ORDER BY ja.applied_at_utc DESC
        ) AS company_application_rank,
        AVG(ja.salary_max) OVER (
            PARTITION BY c.industry
        ) AS industry_avg_max_salary
    FROM job_applications ja
    INNER JOIN companies c ON c.id = ja.company_id
    WHERE ja.status IN ('Applied', 'Interviewing', 'Offered')
)
SELECT
    id,
    job_title,
    company_name,
    status,
    salary_max,
    ROUND(industry_avg_max_salary, 2) AS industry_avg_max_salary
FROM ranked_applications
WHERE company_application_rank = 1
ORDER BY applied_at_utc DESC;
```

### 3. Pagination: Offset vs. Keyset (Seek) Pagination
- **Offset Pagination** (standard ANSI SQL:2008 `OFFSET ... FETCH NEXT`, supported in both PostgreSQL and Oracle 12c+):
  ```sql
  SELECT ja.id, ja.job_title, ja.applied_at_utc
  FROM job_applications ja
  WHERE ja.status = 'Interviewing'
  ORDER BY ja.applied_at_utc DESC, ja.id ASC
  OFFSET 20 ROWS FETCH NEXT 20 ROWS ONLY;
  ```
- **Keyset / Seek Pagination** (O(1) index lookup for deep pages):
  ```sql
  SELECT ja.id, ja.job_title, ja.applied_at_utc
  FROM job_applications ja
  WHERE ja.status = 'Interviewing'
    AND (ja.applied_at_utc < :last_applied_at
         OR (ja.applied_at_utc = :last_applied_at AND ja.id > :last_id))
  ORDER BY ja.applied_at_utc DESC, ja.id ASC
  FETCH NEXT 20 ROWS ONLY;
  ```

### 4. Query Execution Plan Analysis
- **PostgreSQL**:
  ```sql
  EXPLAIN (ANALYZE, BUFFERS, VERBOSE)
  SELECT ja.id, ja.job_title, c.name
  FROM job_applications ja
  JOIN companies c ON c.id = ja.company_id
  WHERE ja.status = 'Interviewing'
  ORDER BY ja.applied_at_utc DESC
  LIMIT 20;
  ```
  Look for `Index Scan` or `Index Only Scan`, high buffer hits (`shared hit`), and absence of `Seq Scan` on large tables or external disk sorts.
- **Oracle Database**:
  ```sql
  EXPLAIN PLAN FOR
  SELECT ja.id, ja.job_title, c.name
  FROM job_applications ja
  JOIN companies c ON c.id = ja.company_id
  WHERE ja.status = 'Interviewing'
  ORDER BY ja.applied_at_utc DESC
  FETCH FIRST 20 ROWS ONLY;

  SELECT * FROM TABLE(DBMS_XPLAN.DISPLAY(FORMAT => 'TYPICAL +PREDICATE +COST'));
  ```
  Look for `INDEX RANGE SCAN` vs `TABLE ACCESS FULL`, check `Predicate Information` (Access vs Filter predicates), and verify cardinality estimates (`E-Rows` vs `A-Rows`).

See [PostgreSQL vs. Oracle Guide Reference](./references/postgresql-vs-oracle-guide.md) for stored procedures, functions, locking, transaction isolation levels, and EF Core provider differences.

---

## Common Mistakes to Avoid
1. **Unindexed Foreign Keys**:Neither PostgreSQL nor Oracle automatically indexes foreign key columns! Missing FK indexes cause slow `JOIN` queries and table-level lock escalations during parent `DELETE` / `UPDATE`.
2. **Wrapping Indexed Columns in Functions**: Writing `WHERE LOWER(job_title) = 'backend engineer'` disables standard B-Tree indexes unless a function-based / expression index (`CREATE INDEX ix_job_title_lower ON job_applications (LOWER(job_title))`) exists.
3. **Leading Wildcard Searches (`LIKE '%term%'`)**: Causes full table/index scans. Use PostgreSQL `pg_trgm` GIN indexes / Full-Text Search or Oracle Text for substring searches on large tables.
4. **Mixing Provider Dialects**: Using `NVL` or `SYSDATE` in PostgreSQL, or `ILIKE` / `LIMIT` in Oracle. Use ANSI-standard `COALESCE`, `CASE WHEN`, and `OFFSET ... FETCH NEXT`.

---

## Verification Checklist
- [ ] Every foreign key column has a corresponding B-Tree index.
- [ ] All tables enforce `PRIMARY KEY`, `FOREIGN KEY`, `NOT NULL`, and domain `CHECK` constraints.
- [ ] Queries on non-trivial tables have been verified with `EXPLAIN (ANALYZE, BUFFERS)` (PostgreSQL) or `DBMS_XPLAN` (Oracle).
- [ ] Pagination queries include a deterministic tie-breaker column (e.g., `ORDER BY applied_at_utc DESC, id ASC`).
- [ ] SQL syntax matches the target EF Core database provider (`Npgsql` vs `Oracle`).

---

## Official Documentation References
- [PostgreSQL Documentation](https://www.postgresql.org/docs/)
- [PostgreSQL EXPLAIN Guide](https://www.postgresql.org/docs/current/using-explain.html)
- [Oracle Database Documentation](https://docs.oracle.com/en/database/)
- [Oracle SQL Tuning Guide](https://docs.oracle.com/en/database/oracle/oracle-database/19/tgsql/)
