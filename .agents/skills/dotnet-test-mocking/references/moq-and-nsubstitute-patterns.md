# Moq and NSubstitute Patterns Reference

## 1. Side-by-Side Comparison: Moq vs. NSubstitute

| Operation | Moq | NSubstitute |
| :--- | :--- | :--- |
| **Create Mock** | `var mock = new Mock<IRepo>();` | `var sub = Substitute.For<IRepo>();` |
| **Pass Instance** | `mock.Object` | `sub` |
| **Setup Async Return** | `mock.Setup(x => x.GetAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);` | `sub.GetAsync(id, Arg.Any<CancellationToken>()).Returns(entity);` |
| **Setup Exception** | `mock.Setup(x => x.SaveAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new TimeoutException());` | `sub.SaveAsync(Arg.Any<CancellationToken>()).ThrowsAsync(new TimeoutException());` |
| **Verify Call Once** | `mock.Verify(x => x.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);` | `await sub.Received(1).SaveAsync(Arg.Any<CancellationToken>());` |
| **Verify Never Called** | `mock.Verify(x => x.DeleteAsync(It.IsAny<Guid>()), Times.Never);` | `await sub.DidNotReceive().DeleteAsync(Arg.Any<Guid>());` |

---

## 2. Verifying `ILogger<T>` Calls in Moq (When Required for Audit/Security Logs)
Because `logger.LogInformation(...)` is an extension method, Moq must verify the underlying `ILogger.Log<TState>` interface method:

```csharp
using Microsoft.Extensions.Logging;
using Moq;

public static class LoggerMockExtensions
{
    public static void VerifyLog<T>(
        this Mock<ILogger<T>> loggerMock,
        LogLevel expectedLevel,
        string expectedMessageSubstring,
        Times times)
    {
        loggerMock.Verify(
            x => x.Log(
                expectedLevel,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) =>
                    state.ToString()!.Contains(expectedMessageSubstring, StringComparison.OrdinalIgnoreCase)),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            times);
    }
}
```
