using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

using Xunit;

namespace NexusLabs.Framework.Analyzers.Tests;

public sealed class TryPatternAnalyzerTests
{
    [Fact]
    public async Task MethodWithEntireTryCatchBlock_ReportsDiagnostic()
    {
        var source =
            """
            using System;
            using System.Threading.Tasks;
            namespace Test
            {
                public class TestClass
                {
                    public async Task {|#0:TestMethodAsync|}()
                    {
                        try
                        {
                            await DoSomethingAsync();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                        }
                    }
                    private Task DoSomethingAsync() => Task.CompletedTask;
                }
            }
            """;

        var expected = new DiagnosticResult("NLF0006", DiagnosticSeverity.Warning)
            .WithLocation(0)
            .WithArguments("TestMethodAsync");
        await VerifyAsync(source, expected, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task SynchronousMethodWithEntireTryCatchBlock_NoDiagnostic()
    {
        var source =
            """
            using System;
            namespace Test
            {
                public class TestClass
                {
                    public string TryNormalize(string input)
                    {
                        try
                        {
                            if (string.IsNullOrWhiteSpace(input))
                            {
                                throw new ArgumentException("Input cannot be null or whitespace");
                            }
                            return input.ToUpperInvariant();
                        }
                        catch (Exception ex)
                        {
                            return ex.Message;
                        }
                    }
                }
            }
            """;

        await VerifyAsync(source, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task SynchronousMethodReturningTriedEx_WithEntireTryCatchBlock_NoDiagnostic()
    {
        var source =
            """
            using System;
            using NexusLabs.Framework;
            namespace Test
            {
                public class TestClass
                {
                    public TriedEx<string> TryNormalize(string input)
                    {
                        try
                        {
                            if (string.IsNullOrWhiteSpace(input))
                            {
                                return new ArgumentException("Input cannot be null or whitespace");
                            }
                            return input.ToUpperInvariant();
                        }
                        catch (Exception ex)
                        {
                            return ex;
                        }
                    }
                }
            }
            """;

        await VerifyAsync(source, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task MethodWithMultipleStatements_NoDiagnostic()
    {
        var source =
            """
            using System;
            using System.Threading.Tasks;
            namespace Test
            {
                public class TestClass
                {
                    public async Task TestMethodAsync()
                    {
                        Console.WriteLine("Before");
                        try
                        {
                            await DoSomethingAsync();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                        }
                    }
                    private Task DoSomethingAsync() => Task.CompletedTask;
                }
            }
            """;

        await VerifyAsync(source, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task MethodWithTryCatchAndOtherStatements_NoDiagnostic()
    {
        var source =
            """
            using System;
            using System.Threading.Tasks;
            namespace Test
            {
                public class TestClass
                {
                    public async Task TestMethodAsync()
                    {
                        try
                        {
                            await DoSomethingAsync();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                        }
                        Console.WriteLine("After");
                    }
                    private Task DoSomethingAsync() => Task.CompletedTask;
                }
            }
            """;

        await VerifyAsync(source, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task TryAsyncMethodScoped_WithoutLogger_ReportsDiagnostic()
    {
        var source =
            """
            using System;
            using System.Threading.Tasks;
            using NexusLabs.Framework;
            namespace Test
            {
                public class TestClass
                {
                    public async Task<Exception?> TestMethodAsync() => await
                    {|#0:Try.Async(async () =>
                    {
                        await DoSomethingAsync();
                    })|};
                    private Task DoSomethingAsync() => Task.CompletedTask;
                }
            }
            """;

        var expected = new DiagnosticResult("NLF0007", DiagnosticSeverity.Warning)
            .WithLocation(0)
            .WithArguments("TestMethodAsync");
        await VerifyAsync(source, expected, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task TryAsyncMethodScoped_WithLogger_NoDiagnostic()
    {
        var source =
            """
            using System;
            using System.Threading.Tasks;
            using Microsoft.Extensions.Logging;
            using NexusLabs.Framework;
            namespace Test
            {
                public class TestClass
                {
                    private readonly ILogger<TestClass> _logger;
                    public TestClass(ILogger<TestClass> logger) { _logger = logger; }
                    public async Task<Exception?> TestMethodAsync() => await
                    Try.Async(_logger, async () =>
                    {
                        await DoSomethingAsync();
                    });
                    private Task DoSomethingAsync() => Task.CompletedTask;
                }
            }
            """;

        await VerifyAsync(source, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task TryGetAsyncMethodScoped_WithoutLogger_ReportsDiagnostic()
    {
        var source =
            """
            using System;
            using System.Threading.Tasks;
            using NexusLabs.Framework;
            namespace Test
            {
                public class TestClass
                {
                    public async Task<TriedEx<string>> TestMethodAsync() => await
                    {|#0:Try.GetAsync<string>(async () =>
                    {
                        return "result";
                    })|};
                }
            }
            """;

        var expected = new DiagnosticResult("NLF0007", DiagnosticSeverity.Warning)
            .WithLocation(0)
            .WithArguments("TestMethodAsync");
        await VerifyAsync(source, expected, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task TryGetAsyncMethodScoped_WithLogger_NoDiagnostic()
    {
        var source =
            """
            using System;
            using System.Threading.Tasks;
            using Microsoft.Extensions.Logging;
            using NexusLabs.Framework;
            namespace Test
            {
                public class TestClass
                {
                    private readonly ILogger<TestClass> _logger;
                    public TestClass(ILogger<TestClass> logger) { _logger = logger; }
                    public async Task<TriedEx<string>> TestMethodAsync() => await
                    Try.GetAsync<string>(_logger, async () =>
                    {
                        return "result";
                    });
                }
            }
            """;

        await VerifyAsync(source, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task TryAsyncNonMethodScoped_WithoutLogger_NoDiagnostic()
    {
        var source =
            """
            using System;
            using System.Threading.Tasks;
            using NexusLabs.Framework;
            namespace Test
            {
                public class TestClass
                {
                    public async Task TestMethodAsync()
                    {
                        Console.WriteLine("Before");
                        var error = await Try.Async(async () =>
                        {
                            await DoSomethingAsync();
                        });
                        if (error != null)
                        {
                            Console.WriteLine(error);
                        }
                    }
                    private Task DoSomethingAsync() => Task.CompletedTask;
                }
            }
            """;

        await VerifyAsync(source, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ThrowInsideTryAsync_ReportsDiagnostic()
    {
        var source =
            """
            using System;
            using System.Threading.Tasks;
            using Microsoft.Extensions.Logging;
            using NexusLabs.Framework;
            namespace Test
            {
                public class TestClass
                {
                    private readonly ILogger<TestClass> _logger;
                    public TestClass(ILogger<TestClass> logger) { _logger = logger; }
                    public async Task<Exception?> TestMethodAsync() => await
                    Try.Async(_logger, async () =>
                    {
                        if (true)
                        {
                            {|#0:throw new InvalidOperationException("Error");|}
                        }
                    });
                }
            }
            """;

        var expected = new DiagnosticResult("NLF0008", DiagnosticSeverity.Warning)
            .WithLocation(0)
            .WithArguments("TestMethodAsync");
        await VerifyAsync(source, expected, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ThrowInsideTryGetAsync_ReportsDiagnostic()
    {
        var source =
            """
            using System;
            using System.Threading.Tasks;
            using Microsoft.Extensions.Logging;
            using NexusLabs.Framework;
            namespace Test
            {
                public class TestClass
                {
                    private readonly ILogger<TestClass> _logger;
                    public TestClass(ILogger<TestClass> logger) { _logger = logger; }
                    public async Task<TriedEx<string>> TestMethodAsync() => await
                    Try.GetAsync<string>(_logger, async () =>
                    {
                        {|#0:throw new InvalidOperationException("Error");|}
                    });
                }
            }
            """;

        var expected = new DiagnosticResult("NLF0008", DiagnosticSeverity.Warning)
            .WithLocation(0)
            .WithArguments("TestMethodAsync");
        await VerifyAsync(source, expected, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ReturnExceptionInsideTryAsync_NoDiagnostic()
    {
        var source =
            """
            using System;
            using System.Threading.Tasks;
            using Microsoft.Extensions.Logging;
            using NexusLabs.Framework;
            namespace Test
            {
                public class TestClass
                {
                    private readonly ILogger<TestClass> _logger;
                    public TestClass(ILogger<TestClass> logger) { _logger = logger; }
                    public async Task<Exception?> TestMethodAsync() => await
                    Try.Async(_logger, async () =>
                    {
                        await Task.CompletedTask;
                        return new InvalidOperationException("Error");
                    });
                }
            }
            """;

        await VerifyAsync(source, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task TryGetOrNullAsyncMethodScoped_WithoutLogger_ReportsDiagnostic()
    {
        var source =
            """
            using System;
            using System.Threading.Tasks;
            using NexusLabs.Framework;
            namespace Test
            {
                public class TestClass
                {
                    public async Task<TriedNullEx<string?>> TestMethodAsync() => await
                    {|#0:Try.GetOrNullAsync<string?>(async () =>
                    {
                        return "result";
                    })|};
                }
            }
            """;

        var expected = new DiagnosticResult("NLF0007", DiagnosticSeverity.Warning)
            .WithLocation(0)
            .WithArguments("TestMethodAsync");
        await VerifyAsync(source, expected, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task TryGetOrNullAsyncMethodScoped_WithLogger_NoDiagnostic()
    {
        var source =
            """
            using System;
            using System.Threading.Tasks;
            using Microsoft.Extensions.Logging;
            using NexusLabs.Framework;
            namespace Test
            {
                public class TestClass
                {
                    private readonly ILogger<TestClass> _logger;
                    public TestClass(ILogger<TestClass> logger) { _logger = logger; }
                    public async Task<TriedNullEx<string?>> TestMethodAsync() => await
                    Try.GetOrNullAsync<string?>(_logger, async () =>
                    {
                        return "result";
                    });
                }
            }
            """;

        await VerifyAsync(source, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task MethodWithOnlyTryFinally_NoDiagnostic()
    {
        var source =
            """
            using System;
            using System.Threading.Tasks;
            namespace Test
            {
                public class TestClass
                {
                    public async Task TestMethodAsync()
                    {
                        try
                        {
                            await DoSomethingAsync();
                        }
                        finally
                        {
                            Console.WriteLine("Finally");
                        }
                    }
                    private Task DoSomethingAsync() => Task.CompletedTask;
                }
            }
            """;

        await VerifyAsync(source, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ThrowOutsideTryAsync_NoDiagnostic()
    {
        var source =
            """
            using System;
            using System.Threading.Tasks;
            using Microsoft.Extensions.Logging;
            using NexusLabs.Framework;
            namespace Test
            {
                public class TestClass
                {
                    private readonly ILogger<TestClass> _logger;
                    public TestClass(ILogger<TestClass> logger) { _logger = logger; }
                    public async Task<Exception?> TestMethodAsync()
                    {
                        var error = await Try.Async(_logger, async () =>
                        {
                            await Task.CompletedTask;
                        });
                        if (error != null)
                        {
                            throw new InvalidOperationException("Error outside Try.Async");
                        }
                        return null;
                    }
                }
            }
            """;

        await VerifyAsync(source, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task NestedTryAsyncInsideMethodScopedTryGetAsync_WithoutLogger_NoDiagnostic()
    {
        var source =
            """
            using System;
            using System.Data;
            using System.Threading.Tasks;
            using Microsoft.Extensions.Logging;
            using NexusLabs.Framework;
            namespace Test
            {
                public class TestClass
                {
                    private readonly ILogger<TestClass> _logger;
                    public TestClass(ILogger<TestClass> logger) { _logger = logger; }
                    public async Task<TriedEx<bool>> TryAddIfNotExistsAsync() => await
                    Try.GetAsync<bool>(_logger, async () =>
                    {
                        var connection = await GetConnectionAsync();
                        var transaction = connection.BeginTransaction();
                        var addError = await Try.Async(async () =>
                        {
                            await AddToRepositoryAsync(transaction);
                            transaction.Commit();
                        });
                        if (addError is not null)
                        {
                            transaction.Rollback();
                            if (addError.Message.Contains("Duplicate entry"))
                            {
                                return false;
                            }
                            return addError;
                        }
                        return true;
                    });
                    private Task<IDbConnection> GetConnectionAsync() => Task.FromResult<IDbConnection>(null!);
                    private Task AddToRepositoryAsync(IDbTransaction transaction) => Task.CompletedTask;
                }
            }
            """;

        await VerifyAsync(source, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ExtensionMethod_WithMethodScopedTryGetAsync_WithoutLogger_NoDiagnostic()
    {
        var source =
            """
            using System.Threading;
            using System.Threading.Tasks;
            using NexusLabs.Framework;
            namespace Test
            {
                public static class RestClientExtensions
                {
                    public static async Task<TriedEx<string>> TryGetAsync(
                        this IRestClient restClient,
                        string request,
                        CancellationToken cancellationToken) => await
                    Try.GetAsync<string>(async () =>
                    {
                        var response = await restClient.ExecuteAsync(request, cancellationToken);
                        return response;
                    });
                }
                public interface IRestClient
                {
                    Task<string> ExecuteAsync(string request, CancellationToken cancellationToken);
                }
            }
            """;

        await VerifyAsync(source, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ExtensionMethod_WithMethodScopedTryAsync_WithoutLogger_NoDiagnostic()
    {
        var source =
            """
            using System;
            using System.Threading;
            using System.Threading.Tasks;
            using NexusLabs.Framework;
            namespace Test
            {
                public static class StringExtensions
                {
                    public static async Task<Exception?> TryProcessAsync(
                        this string value,
                        CancellationToken cancellationToken) => await
                    Try.Async(async () =>
                    {
                        await Task.Delay(100, cancellationToken);
                    });
                }
            }
            """;

        await VerifyAsync(source, TestContext.Current.CancellationToken);
    }

    private static Task VerifyAsync(
        string source,
        CancellationToken cancellationToken)
        => VerifyAsync(source, [], cancellationToken);

    private static Task VerifyAsync(
        string source,
        DiagnosticResult expected,
        CancellationToken cancellationToken)
        => VerifyAsync(source, [expected], cancellationToken);

    private static async Task VerifyAsync(
        string source,
        DiagnosticResult[] expected,
        CancellationToken cancellationToken)
    {
        var test = new CSharpAnalyzerTest<TryPatternAnalyzer, DefaultVerifier>
        {
            TestCode = source,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
        };

        test.TestState.Sources.Add(("TryHelperStubs.cs", TestSources.TryHelperStubs));
        test.ExpectedDiagnostics.AddRange(expected);

        await test.RunAsync(cancellationToken);
    }
}
