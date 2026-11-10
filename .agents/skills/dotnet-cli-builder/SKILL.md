---
name: dotnet-cli-builder
description: >-
  Use this skill when building, refactoring, or testing .NET command-line (CLI)
  applications and developer automation tools using System.CommandLine. Covers
  commands, subcommands, arguments, options, input validation, Dependency
  Injection, configuration, logging, JSON input/output, file operations,
  environment variables, async execution, exit codes, help documentation, and
  CLI unit testing.
---

# .NET CLI Application Builder (`dotnet-cli-builder`)

## Overview
Provides architecture and implementation patterns for building robust, scriptable .NET command-line applications using **`System.CommandLine`** and **`Microsoft.Extensions.Hosting`** for the **Job Tracker** ecosystem (e.g., data import/export, seeding, report generation, and maintenance utilities).

## Activation Instructions
Activate this skill whenever you are:
- Creating a new .NET console/CLI tool or adding commands/subcommands to an existing CLI project.
- Defining `RootCommand`, `Command`, `Option<T>`, `Argument<T>`, custom validators, or help text with `System.CommandLine`.
- Wiring Dependency Injection, `IConfiguration`, `ILogger`, JSON output (`--format json`), file I/O, or standard exit codes in a CLI app.
- Writing unit tests for CLI command parsing, execution, and standard output/error streams.

## Specific Responsibilities
1. **Structure Hierarchical Commands Clearly**: Organize commands into cohesive verbs and nouns (`jobtracker applications export`, `jobtracker db seed`).
2. **Validate Inputs Before Execution**: Use `Option<T>` / `Argument<T>` custom validators for file existence, allowed values, and numeric ranges before invoking business services.
3. **Separate `stdout` and `stderr` Streams**: Write machine-readable results (such as JSON) to `stdout` and diagnostics/errors to `stderr` so CLI commands can be piped reliably in CI/CD scripts.
4. **Return Meaningful Exit Codes**: Use `0` for success, `1` for general runtime errors, `2` for invalid CLI usage/validation errors, and `130` for user cancellation (`OperationCanceledException`).

---

## Technology-Specific Implementation Guidance

### 1. Command Hierarchy, Options, Arguments & Validation
```csharp
using System.CommandLine;
using System.Text.Json;
using JobTracker.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace JobTracker.Cli.Commands;

public static class ExportApplicationsCommand
{
    public static Command Create(IServiceProvider serviceProvider)
    {
        var outputFileArgument = new Argument<FileInfo>(
            name: "output-file",
            description: "Destination file path for exported job applications.");

        var statusOption = new Option<string?>(
            aliases: ["--status", "-s"],
            description: "Filter applications by status (e.g., Applied, Interviewing, Offered).");

        var formatOption = new Option<string>(
            aliases: ["--format", "-f"],
            getDefaultValue: () => "json",
            description: "Output format: 'json' or 'summary'.");
        formatOption.FromAmong("json", "summary");

        var prettyOption = new Option<bool>(
            aliases: ["--pretty", "-p"],
            getDefaultValue: () => true,
            description: "Format JSON output with indentation.");

        var command = new Command("export", "Export job applications to a JSON file or stdout.")
        {
            outputFileArgument,
            statusOption,
            formatOption,
            prettyOption
        };

        command.SetHandler(async context =>
        {
            var outputFile = context.ParseResult.GetValueForArgument(outputFileArgument);
            var status = context.ParseResult.GetValueForOption(statusOption);
            var pretty = context.ParseResult.GetValueForOption(prettyOption);
            var ct = context.GetCancellationToken();

            using var scope = serviceProvider.CreateScope();
            var exporter = scope.ServiceProvider.GetRequiredService<IApplicationExportService>();
            var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
                .CreateLogger("JobTracker.Cli.Export");

            try
            {
                var items = await exporter.ExportAsync(status, ct);
                var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
                {
                    WriteIndented = pretty
                };

                outputFile.Directory?.Create();
                await using var fileStream = outputFile.Create();
                await JsonSerializer.SerializeAsync(fileStream, items, jsonOptions, ct);

                context.Console.Out.WriteLine(
                    JsonSerializer.Serialize(new { exportedCount = items.Count, path = outputFile.FullName }, jsonOptions));
                context.ExitCode = ExitCodes.Success;
            }
            catch (OperationCanceledException)
            {
                context.Console.Error.WriteLine("Operation canceled by user.");
                context.ExitCode = ExitCodes.Canceled;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to export job applications.");
                context.Console.Error.WriteLine($"Error: {ex.Message}");
                context.ExitCode = ExitCodes.RuntimeError;
            }
        });

        return command;
    }
}

public static class ExitCodes
{
    public const int Success = 0;
    public const int RuntimeError = 1;
    public const int ValidationError = 2;
    public const int Canceled = 130;
}
```

### 2. Dependency Injection, Configuration & Environment Variables
Build a lightweight host using `Host.CreateApplicationBuilder(args)` and direct `ILogger` output to `stderr` so `stdout` remains clean for JSON piping:
```csharp
using System.CommandLine;
using JobTracker.Cli.Commands;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables(prefix: "JOBTRACKER_");

builder.Logging.ClearProviders();
builder.Logging.AddConsole(options =>
{
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});

builder.Services.AddScoped<IApplicationExportService, ApplicationExportService>();

using var host = builder.Build();

var rootCommand = new RootCommand("Job Tracker CLI - Automation & Data Management Tool");
var applicationsCommand = new Command("applications", "Manage and export job applications");
applicationsCommand.AddCommand(ExportApplicationsCommand.Create(host.Services));
rootCommand.AddCommand(applicationsCommand);

return await rootCommand.InvokeAsync(args);
```

See [System.CommandLine Patterns Reference](./references/system-commandline-patterns.md) for custom input validators, JSON stdin reading, and unit testing CLI commands with `TestConsole`.

---

## CLI Commands
```powershell
# Display generated CLI help documentation
dotnet run --project src/JobTracker.Cli -- --help
dotnet run --project src/JobTracker.Cli -- applications export --help

# Run export command with options
dotnet run --project src/JobTracker.Cli -- applications export ./artifacts/applications.json --status Interviewing --pretty

# Pack CLI tool as a .NET local/global tool
dotnet pack src/JobTracker.Cli/JobTracker.Cli.csproj -c Release
```

---

## Common Mistakes to Avoid
1. **Polluting `stdout` with Logs**: Writing `ILogger` messages to `stdout` breaks downstream JSON parsers (`jq`, PowerShell `ConvertFrom-Json`) when the CLI is piped. Always configure `LogToStandardErrorThreshold = LogLevel.Trace`.
2. **Returning `0` on Failure**: Always set `context.ExitCode` (or return a non-zero `int` from `Main`) when an error occurs so CI/CD pipelines fail fast.
3. **Ignoring `CancellationToken` (`Ctrl+C`)**: Always retrieve `context.GetCancellationToken()` and pass it to file, HTTP, and database operations.
4. **Hardcoding Paths & Credentials**: Read connection strings and API URLs from `IConfiguration` and `JOBTRACKER_*` environment variables.

---

## Verification Checklist
- [ ] Every command, argument, and option includes clear `description` text for `--help`.
- [ ] All async handlers observe `CancellationToken` for graceful `Ctrl+C` termination.
- [ ] Standard output (`stdout`) emits clean data/JSON, while logs and errors go to `stderr`.
- [ ] Meaningful exit codes (`0`, `1`, `2`, `130`) are returned for all outcomes.
- [ ] Commands are unit-tested using `TestConsole` without touching real production files.

---

## Official Documentation References
- [System.CommandLine Overview](https://learn.microsoft.com/en-us/dotnet/standard/commandline/)
- [Define Commands, Options, and Arguments in System.CommandLine](https://learn.microsoft.com/en-us/dotnet/standard/commandline/define-commands)
- [.NET Generic Host in Console Apps](https://learn.microsoft.com/en-us/dotnet/core/extensions/generic-host)
