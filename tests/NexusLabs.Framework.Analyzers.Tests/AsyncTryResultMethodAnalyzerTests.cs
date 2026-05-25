using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

using Xunit;

namespace NexusLabs.Framework.Analyzers.Tests;

public sealed class AsyncTryResultMethodAnalyzerTests
{
    [Fact]
    public async Task AsyncMethodReturningTriedEx_WithoutTryPattern_ReportsDiagnostic()
    {
        var source =
            """
            using System.Threading.Tasks;
            using NexusLabs.Framework;
            namespace Test
            {
                public class TestClass
                {
                    public async Task<TriedEx<string>> {|#0:TryGetValueAsync|}()
                    {
                        var result = await GetSomeValueAsync();
                        if (result == null)
                        {
                            return new System.Exception("Value not found");
                        }
                        return result;
                    }
                    private Task<string?> GetSomeValueAsync() => Task.FromResult<string?>("test");
                }
            }
            """;

        var expected = new DiagnosticResult("NLF0009", DiagnosticSeverity.Warning)
            .WithLocation(0)
            .WithArguments("TryGetValueAsync", "Task<TriedEx<String>>");
        await VerifyAsync(source, expected);
    }

    [Fact]
    public async Task AsyncMethodReturningTriedNullEx_WithoutTryPattern_ReportsDiagnostic()
    {
        var source =
            """
            using System.Threading.Tasks;
            using NexusLabs.Framework;
            namespace Test
            {
                public class TestClass
                {
                    public async Task<TriedNullEx<string?>> {|#0:TryGetValueAsync|}()
                    {
                        var result = await GetSomeValueAsync();
                        return result;
                    }
                    private Task<string?> GetSomeValueAsync() => Task.FromResult<string?>(null);
                }
            }
            """;

        var expected = new DiagnosticResult("NLF0009", DiagnosticSeverity.Warning)
            .WithLocation(0)
            .WithArguments("TryGetValueAsync", "Task<TriedNullEx<String>>");
        await VerifyAsync(source, expected);
    }

    [Fact]
    public async Task AsyncMethodReturningTriedEx_WithTryGetAsync_NoDiagnostic()
    {
        var source =
            """
            using System.Threading.Tasks;
            using Microsoft.Extensions.Logging;
            using NexusLabs.Framework;
            namespace Test
            {
                public class TestClass
                {
                    private readonly ILogger<TestClass> _logger;
                    public TestClass(ILogger<TestClass> logger) { _logger = logger; }
                    public async Task<TriedEx<string>> TryGetValueAsync() => await
                    Try.GetAsync<string>(_logger, async () =>
                    {
                        var result = await GetSomeValueAsync();
                        if (result == null)
                        {
                            return new System.Exception("Value not found");
                        }
                        return result;
                    });
                    private Task<string?> GetSomeValueAsync() => Task.FromResult<string?>("test");
                }
            }
            """;

        await VerifyAsync(source);
    }

    [Fact]
    public async Task AsyncMethodReturningTriedNullEx_WithTryGetOrNullAsync_NoDiagnostic()
    {
        var source =
            """
            using System.Threading.Tasks;
            using Microsoft.Extensions.Logging;
            using NexusLabs.Framework;
            namespace Test
            {
                public class TestClass
                {
                    private readonly ILogger<TestClass> _logger;
                    public TestClass(ILogger<TestClass> logger) { _logger = logger; }
                    public async Task<TriedNullEx<string?>> TryGetValueAsync() => await
                    Try.GetOrNullAsync<string?>(_logger, async () =>
                    {
                        return await GetSomeValueAsync();
                    });
                    private Task<string?> GetSomeValueAsync() => Task.FromResult<string?>(null);
                }
            }
            """;

        await VerifyAsync(source);
    }

    [Fact]
    public async Task SynchronousMethodReturningTriedEx_NoDiagnostic()
    {
        var source =
            """
            using NexusLabs.Framework;
            namespace Test
            {
                public class TestClass
                {
                    public TriedEx<string> TryGetValue()
                    {
                        var result = GetSomeValue();
                        if (result == null)
                        {
                            return new System.Exception("Value not found");
                        }
                        return result;
                    }
                    private string? GetSomeValue() => "test";
                }
            }
            """;

        await VerifyAsync(source);
    }

    [Fact]
    public async Task ExtensionMethodReturningTriedEx_WithoutTryPattern_NoDiagnostic()
    {
        var source =
            """
            using System.Threading.Tasks;
            using NexusLabs.Framework;
            namespace Test
            {
                public static class StringExtensions
                {
                    public static async Task<TriedEx<string>> TryProcessAsync(this string value)
                    {
                        await Task.Delay(100);
                        if (string.IsNullOrWhiteSpace(value))
                        {
                            return new System.Exception("Value is null or whitespace");
                        }
                        return value.ToUpperInvariant();
                    }
                }
            }
            """;

        await VerifyAsync(source);
    }

    [Fact]
    public async Task AsyncMethodReturningPlainTask_NoDiagnostic()
    {
        var source =
            """
            using System.Threading.Tasks;
            namespace Test
            {
                public class TestClass
                {
                    public async Task ProcessAsync()
                    {
                        await Task.Delay(100);
                    }
                }
            }
            """;

        await VerifyAsync(source);
    }

    [Fact]
    public async Task AsyncMethodReturningTaskOfString_NoDiagnostic()
    {
        var source =
            """
            using System.Threading.Tasks;
            namespace Test
            {
                public class TestClass
                {
                    public async Task<string> GetValueAsync()
                    {
                        await Task.Delay(100);
                        return "test";
                    }
                }
            }
            """;

        await VerifyAsync(source);
    }

    [Fact]
    public async Task ExpressionBodiedMethod_WithTryGetAsync_NoDiagnostic()
    {
        var source =
            """
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using Microsoft.Extensions.Logging;
            using NexusLabs.Framework;
            namespace Test
            {
                public class TestClass
                {
                    private readonly ILogger<TestClass> _logger;
                    public TestClass(ILogger<TestClass> logger) { _logger = logger; }
                    public async Task<TriedEx<IReadOnlyList<string>>> TryGetItemsAsync(
                        CancellationToken cancellationToken) => await
                    Try.GetAsync<IReadOnlyList<string>>(_logger, async () =>
                    {
                        var items = new List<string>();
                        await Task.Delay(100, cancellationToken);
                        items.Add("test");
                        return items;
                    });
                }
            }
            """;

        await VerifyAsync(source);
    }

    [Fact]
    public async Task MethodDirectlyReturningAnotherTryResult_NoDiagnostic()
    {
        var source =
            """
            using System.Threading;
            using System.Threading.Tasks;
            using NexusLabs.Framework;
            namespace Test
            {
                public class TestClass
                {
                    private readonly IDataService _dataService;
                    public TestClass(IDataService dataService) { _dataService = dataService; }
                    public async Task<TriedNullEx<string?>> TryGetDataAsync(
                        int id,
                        CancellationToken cancellationToken)
                    {
                        return await _dataService.TryFetchDataAsync(id, cancellationToken);
                    }
                }
                public interface IDataService
                {
                    Task<TriedNullEx<string?>> TryFetchDataAsync(int id, CancellationToken cancellationToken);
                }
            }
            """;

        await VerifyAsync(source);
    }

    [Fact]
    public async Task MethodDirectlyReturningAnotherTryResultWithAwait_NoDiagnostic()
    {
        var source =
            """
            using System.Threading;
            using System.Threading.Tasks;
            using NexusLabs.Framework;
            namespace Test
            {
                public class PermalinkHandler
                {
                    private readonly IPermalinkService _permalinkService;
                    public PermalinkHandler(IPermalinkService permalinkService) { _permalinkService = permalinkService; }
                    public async Task<TriedNullEx<string?>> TryGetPostUrlAsync(
                        string accountId,
                        string postId,
                        CancellationToken cancellationToken)
                    {
                        return await _permalinkService.TryGetPermalinkAsync(
                            accountId,
                            postId,
                            cancellationToken);
                    }
                }
                public interface IPermalinkService
                {
                    Task<TriedNullEx<string?>> TryGetPermalinkAsync(
                        string accountId,
                        string postId,
                        CancellationToken cancellationToken);
                }
            }
            """;

        await VerifyAsync(source);
    }

    [Fact]
    public async Task AsyncMethodReturningTriedNullEx_WithTryGetOrNullAsyncInsideTracer_NoDiagnostic()
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
                    public async Task<TriedNullEx<string?>> TryGetValueAsync() => await
                    Tracer.Default.WithTracingAsync(async () => await
                    Try.GetOrNullAsync<string?>(_logger, async () =>
                    {
                        return await GetSomeValueAsync();
                    }));
                    private Task<string?> GetSomeValueAsync() => Task.FromResult<string?>(null);
                }
            }
            """;

        await VerifyAsync(source);
    }

    [Fact]
    public async Task AsyncMethodReturningTriedEx_WithTryGetAsyncInsideTracer_NoDiagnostic()
    {
        var source =
            """
            using System;
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using Microsoft.Extensions.Logging;
            using NexusLabs.Framework;
            namespace Test
            {
                public class TestClass
                {
                    private readonly ILogger<TestClass> _logger;
                    public TestClass(ILogger<TestClass> logger) { _logger = logger; }
                    public async Task<TriedEx<IReadOnlyList<long>>> TryGetItemsAsync() => await
                    Tracer.Default.WithTracingAsync(async () => await
                    Try.GetAsync<IReadOnlyList<long>>(_logger, async () =>
                    {
                        return new List<long> { 1, 2, 3 };
                    }));
                }
            }
            """;

        await VerifyAsync(source);
    }

    private static async Task VerifyAsync(string source, params DiagnosticResult[] expected)
    {
        var test = new CSharpAnalyzerTest<AsyncTryResultMethodAnalyzer, DefaultVerifier>
        {
            TestCode = source,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
        };

        test.TestState.Sources.Add(("TryHelperStubs.cs", TestSources.TryHelperStubs));
        test.ExpectedDiagnostics.AddRange(expected);

        await test.RunAsync();
    }
}
