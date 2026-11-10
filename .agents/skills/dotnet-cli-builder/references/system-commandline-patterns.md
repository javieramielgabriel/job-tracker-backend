# System.CommandLine Patterns & Unit Testing Reference

## 1. Custom Input Validators in `System.CommandLine`
Validate arguments and options during parsing so invalid invocations automatically display an error message and return a non-zero exit code without invoking the handler:

```csharp
var inputFileArgument = new Argument<FileInfo>(
    name: "input-file",
    description: "Path to the JSON file containing job applications to import.");

inputFileArgument.AddValidator(result =>
{
    var file = result.GetValueForArgument(inputFileArgument);
    if (!file.Exists)
    {
        result.ErrorMessage = $"Input file '{file.FullName}' does not exist.";
    }
    else if (!string.Equals(file.Extension, ".json", StringComparison.OrdinalIgnoreCase))
    {
        result.ErrorMessage = $"Input file '{file.Name}' must have a .json extension.";
    }
});

var batchSizeOption = new Option<int>(
    aliases: ["--batch-size", "-b"],
    getDefaultValue: () => 50,
    description: "Number of records to import per transaction batch (1-500).");

batchSizeOption.AddValidator(result =>
{
    var size = result.GetValueForOption(batchSizeOption);
    if (size is < 1 or > 500)
    {
        result.ErrorMessage = "--batch-size must be between 1 and 500.";
    }
});
```

---

## 2. Unit Testing CLI Commands with `TestConsole`
```csharp
using System.CommandLine;
using System.CommandLine.IO;
using JobTracker.Application.Interfaces;
using JobTracker.Cli.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace JobTracker.Cli.Tests;

public sealed class ExportApplicationsCommandTests
{
    [Fact]
    public async Task InvokeAsync_ValidOutputFile_WritesJsonAndReturnsZero()
    {
        // Arrange
        var exportServiceMock = new Mock<IApplicationExportService>();
        exportServiceMock
            .Setup(s => s.ExportAsync("Interviewing", It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var services = new ServiceCollection()
            .AddSingleton(exportServiceMock.Object)
            .AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance)
            .BuildServiceProvider();

        var command = ExportApplicationsCommand.Create(services);
        var console = new TestConsole();
        var tempFile = Path.Combine(Path.GetTempPath(), $"export-{Guid.NewGuid():N}.json");

        try
        {
            // Act
            var exitCode = await command.InvokeAsync(
                ["export", tempFile, "--status", "Interviewing"],
                console);

            // Assert
            Assert.Equal(ExitCodes.Success, exitCode);
            Assert.True(File.Exists(tempFile));
            Assert.Contains("\"exportedCount\": 0", console.Out.ToString());
            Assert.Equal(string.Empty, console.Error.ToString());
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }
}
```
