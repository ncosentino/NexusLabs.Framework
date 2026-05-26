using System.Threading.Tasks;

using NexusLabs.Framework.Analyzers;

using Xunit;

namespace NexusLabs.Framework.Analyzers.Tests;

public sealed class TriedDisposableUsageAnalyzerTests
{
    [Fact]
    public async Task NonDisposableTypeArgument_DoesNotReport()
    {
        var source =
            """
            using NexusLabs.Framework;
            namespace App
            {
                public class C
                {
                    public void M()
                    {
                        var result = new TriedEx<int> { Success = true, Value = 42 };
                    }
                }
            }
            """ + TestSources.TriedDisposableStubs;

        await AnalyzerVerifier<TriedDisposableUsageAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task DisposableTriedEx_NotConsumed_Reports()
    {
        var source =
            """
            using System.IO;
            using NexusLabs.Framework;
            namespace App
            {
                public class C
                {
                    public void M()
                    {
                        var {|#0:result|} = new TriedEx<MemoryStream> { Success = true, Value = new MemoryStream() };
                    }
                }
            }
            """ + TestSources.TriedDisposableStubs;

        var expected = AnalyzerVerifier<TriedDisposableUsageAnalyzer>
            .Diagnostic(DiagnosticDescriptors.TriedDisposableValueNotDisposed)
            .WithLocation(0)
            .WithArguments("result", "TriedEx", "System.IO.MemoryStream", "IDisposable and IAsyncDisposable");

        await AnalyzerVerifier<TriedDisposableUsageAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task DisposableTriedEx_UsingDeclaration_DoesNotReport()
    {
        var source =
            """
            using System.IO;
            using NexusLabs.Framework;
            namespace App
            {
                public class C
                {
                    public void M()
                    {
                        using var result = new TriedEx<MemoryStream> { Success = true, Value = new MemoryStream() };
                    }
                }
            }
            """ + TestSources.TriedDisposableStubs;

        await AnalyzerVerifier<TriedDisposableUsageAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task DisposableTriedEx_Returned_DoesNotReport()
    {
        var source =
            """
            using System.IO;
            using NexusLabs.Framework;
            namespace App
            {
                public class C
                {
                    public TriedEx<MemoryStream> M()
                    {
                        var result = new TriedEx<MemoryStream> { Success = true, Value = new MemoryStream() };
                        return result;
                    }
                }
            }
            """ + TestSources.TriedDisposableStubs;

        await AnalyzerVerifier<TriedDisposableUsageAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task DisposableTriedEx_PassedAsArgument_DoesNotReport()
    {
        var source =
            """
            using System.IO;
            using NexusLabs.Framework;
            namespace App
            {
                public class C
                {
                    public void M()
                    {
                        var result = new TriedEx<MemoryStream> { Success = true, Value = new MemoryStream() };
                        Consume(result);
                    }

                    private void Consume(TriedEx<MemoryStream> tried) { }
                }
            }
            """ + TestSources.TriedDisposableStubs;

        await AnalyzerVerifier<TriedDisposableUsageAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task DisposableTriedEx_DisposeInvoked_DoesNotReport()
    {
        var source =
            """
            using System.IO;
            using NexusLabs.Framework;
            namespace App
            {
                public class C
                {
                    public void M()
                    {
                        var result = new TriedEx<MemoryStream> { Success = true, Value = new MemoryStream() };
                        result.Dispose();
                    }
                }
            }
            """ + TestSources.TriedDisposableStubs;

        await AnalyzerVerifier<TriedDisposableUsageAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task DisposableTriedEx_AwaitDisposeAsync_DoesNotReport()
    {
        var source =
            """
            using System.IO;
            using System.Threading.Tasks;
            using NexusLabs.Framework;
            namespace App
            {
                public class C
                {
                    public async Task M()
                    {
                        var result = new TriedEx<MemoryStream> { Success = true, Value = new MemoryStream() };
                        await result.DisposeAsync();
                    }
                }
            }
            """ + TestSources.TriedDisposableStubs;

        await AnalyzerVerifier<TriedDisposableUsageAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task DisposableTriedEx_AssignedToField_DoesNotReport()
    {
        var source =
            """
            using System.IO;
            using NexusLabs.Framework;
            namespace App
            {
                public class C
                {
                    private TriedEx<MemoryStream> _cached;
                    public void M()
                    {
                        var result = new TriedEx<MemoryStream> { Success = true, Value = new MemoryStream() };
                        _cached = result;
                    }
                }
            }
            """ + TestSources.TriedDisposableStubs;

        await AnalyzerVerifier<TriedDisposableUsageAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task DisposableTried_NotConsumed_Reports()
    {
        var source =
            """
            using System.IO;
            using NexusLabs.Framework;
            namespace App
            {
                public class C
                {
                    public void M()
                    {
                        var {|#0:t|} = new Tried<MemoryStream> { Success = true, Value = new MemoryStream() };
                    }
                }
            }
            """ + TestSources.TriedDisposableStubs;

        var expected = AnalyzerVerifier<TriedDisposableUsageAnalyzer>
            .Diagnostic(DiagnosticDescriptors.TriedDisposableValueNotDisposed)
            .WithLocation(0)
            .WithArguments("t", "Tried", "System.IO.MemoryStream", "IDisposable and IAsyncDisposable");

        await AnalyzerVerifier<TriedDisposableUsageAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task DisposableTriedNullEx_NotConsumed_Reports()
    {
        var source =
            """
            using System.IO;
            using NexusLabs.Framework;
            namespace App
            {
                public class C
                {
                    public void M()
                    {
                        var {|#0:n|} = new TriedNullEx<MemoryStream> { Success = true, Value = new MemoryStream() };
                    }
                }
            }
            """ + TestSources.TriedDisposableStubs;

        var expected = AnalyzerVerifier<TriedDisposableUsageAnalyzer>
            .Diagnostic(DiagnosticDescriptors.TriedDisposableValueNotDisposed)
            .WithLocation(0)
            .WithArguments("n", "TriedNullEx", "System.IO.MemoryStream", "IDisposable and IAsyncDisposable");

        await AnalyzerVerifier<TriedDisposableUsageAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task AsyncOnlyDisposable_TypeArg_Reports_WithAsyncMessage()
    {
        var source =
            """
            using System;
            using System.Threading.Tasks;
            using NexusLabs.Framework;
            namespace App
            {
                public sealed class AsyncOnly : IAsyncDisposable
                {
                    public ValueTask DisposeAsync() => default;
                }

                public class C
                {
                    public void M()
                    {
                        var {|#0:result|} = new TriedEx<AsyncOnly> { Success = true, Value = new AsyncOnly() };
                    }
                }
            }
            """ + TestSources.TriedDisposableStubs;

        var expected = AnalyzerVerifier<TriedDisposableUsageAnalyzer>
            .Diagnostic(DiagnosticDescriptors.TriedDisposableValueNotDisposed)
            .WithLocation(0)
            .WithArguments("result", "TriedEx", "App.AsyncOnly", "IAsyncDisposable");

        await AnalyzerVerifier<TriedDisposableUsageAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task SimilarlyNamedTypeInOtherNamespace_DoesNotReport()
    {
        var source =
            """
            using System;
            using NexusLabs.Framework;
            namespace App
            {
                public readonly struct TriedEx<T> : IDisposable
                {
                    public T Value { get; init; }
                    public void Dispose() { }
                }

                public sealed class C
                {
                    public void M()
                    {
                        var result = new TriedEx<System.IO.MemoryStream> { Value = new System.IO.MemoryStream() };
                    }
                }
            }
            """ + TestSources.TriedDisposableStubs;

        await AnalyzerVerifier<TriedDisposableUsageAnalyzer>.VerifyAnalyzerAsync(source);
    }
}
