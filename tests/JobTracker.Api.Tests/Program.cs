using System.Reflection;
using Xunit;

namespace JobTracker.Api.Tests;

/// <summary>
/// Entry point allowing the xUnit test suite to be executed directly via <c>dotnet run</c>
/// in restricted/sandboxed environments as well as via standard <c>dotnet test</c>.
/// </summary>
public static class Program
{
    public static async Task<int> Main()
    {
        var assembly = typeof(Program).Assembly;
        var testTypes = assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .ToList();

        var passed = 0;
        var failed = 0;

        foreach (var testType in testTypes)
        {
            var methods = testType
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.GetCustomAttribute<FactAttribute>() is not null
                         || m.GetCustomAttribute<TheoryAttribute>() is not null)
                .ToList();

            foreach (var method in methods)
            {
                var inlineDataAttributes = method.GetCustomAttributes<InlineDataAttribute>().ToList();
                var invocations = inlineDataAttributes.Count > 0
                    ? inlineDataAttributes.Select(d => d.GetData(method).First()).ToList()
                    : [Array.Empty<object?>()];

                foreach (var args in invocations)
                {
                    object? instance = null;
                    try
                    {
                        instance = Activator.CreateInstance(testType);
                        var result = method.Invoke(instance, args);
                        if (result is Task task)
                        {
                            await task;
                        }

                        Console.WriteLine($"[PASS] {testType.Name}.{method.Name}");
                        passed++;
                    }
                    catch (Exception ex)
                    {
                        var actual = ex is TargetInvocationException tie && tie.InnerException is not null
                            ? tie.InnerException
                            : ex;
                        Console.Error.WriteLine($"[FAIL] {testType.Name}.{method.Name}: {actual.GetType().Name} - {actual.Message}");
                        failed++;
                    }
                    finally
                    {
                        if (instance is IDisposable disposable)
                        {
                            disposable.Dispose();
                        }
                    }
                }
            }
        }

        Console.WriteLine($"\nTest summary: total: {passed + failed}, failed: {failed}, succeeded: {passed}");
        return failed == 0 ? 0 : 1;
    }
}
