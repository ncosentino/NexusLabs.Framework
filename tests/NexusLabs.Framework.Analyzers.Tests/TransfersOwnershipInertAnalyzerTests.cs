using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

using Xunit;

namespace NexusLabs.Framework.Analyzers.Tests;

public sealed class TransfersOwnershipInertAnalyzerTests
{
    [Fact]
    public async Task Reports_When_Parameterless_On_Bool_Field()
    {
        var source =
            """
            using System;
            using System.IO;
            using NexusLabs.Framework;

            namespace Test
            {
                public sealed class StreamWrapper : IDisposable
                {
                    private readonly Stream _inner = null!;

                    [{|#0:TransfersOwnership|}]
                    private readonly bool _takeOwnership;

                    public void Dispose()
                    {
                        if (_takeOwnership)
                        {
                            _inner.Dispose();
                        }
                    }
                }
            }
            """;

        var expected = new DiagnosticResult("NLF0012", DiagnosticSeverity.Warning)
            .WithLocation(0)
            .WithArguments("_takeOwnership");

        await VerifyAsync(source, expected, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Reports_When_Parameterless_On_Int_Field()
    {
        var source =
            """
            using NexusLabs.Framework;

            namespace Test
            {
                public sealed class Holder
                {
                    [{|#0:TransfersOwnership|}]
                    private readonly int _count;
                }
            }
            """;

        var expected = new DiagnosticResult("NLF0012", DiagnosticSeverity.Warning)
            .WithLocation(0)
            .WithArguments("_count");

        await VerifyAsync(source, expected, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Reports_When_Parameterless_On_Bool_Property()
    {
        var source =
            """
            using NexusLabs.Framework;

            namespace Test
            {
                public sealed class Holder
                {
                    [{|#0:TransfersOwnership|}]
                    public bool Take { get; init; }
                }
            }
            """;

        var expected = new DiagnosticResult("NLF0012", DiagnosticSeverity.Warning)
            .WithLocation(0)
            .WithArguments("Take");

        await VerifyAsync(source, expected, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Reports_When_Parameterless_On_Method_Parameter()
    {
        var source =
            """
            using NexusLabs.Framework;

            namespace Test
            {
                public sealed class Holder
                {
                    public void Configure([{|#0:TransfersOwnership|}] bool takeOwnership) { }
                }
            }
            """;

        var expected = new DiagnosticResult("NLF0012", DiagnosticSeverity.Warning)
            .WithLocation(0)
            .WithArguments("takeOwnership");

        await VerifyAsync(source, expected, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Reports_When_Parameterless_On_Primary_Constructor_Parameter()
    {
        var source =
            """
            using System;
            using System.IO;
            using NexusLabs.Framework;

            namespace Test
            {
                public sealed class StreamWrapper(
                    Stream inner,
                    [{|#0:TransfersOwnership|}] bool takeOwnership) : IDisposable
                {
                    public void Dispose()
                    {
                        if (takeOwnership)
                        {
                            inner.Dispose();
                        }
                    }
                }
            }
            """;

        var expected = new DiagnosticResult("NLF0012", DiagnosticSeverity.Warning)
            .WithLocation(0)
            .WithArguments("takeOwnership");

        await VerifyAsync(source, expected, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Reports_When_Empty_Parens_Used()
    {
        var source =
            """
            using NexusLabs.Framework;

            namespace Test
            {
                public sealed class Holder
                {
                    [{|#0:TransfersOwnership()|}]
                    private readonly bool _flag;
                }
            }
            """;

        var expected = new DiagnosticResult("NLF0012", DiagnosticSeverity.Warning)
            .WithLocation(0)
            .WithArguments("_flag");

        await VerifyAsync(source, expected, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Does_Not_Report_When_Parameterless_On_Stream_Field()
    {
        var source =
            """
            using System;
            using System.IO;
            using NexusLabs.Framework;

            namespace Test
            {
                public sealed class Wrapper : IDisposable
                {
                    [TransfersOwnership]
                    private readonly Stream _inner = null!;

                    public void Dispose() => _inner.Dispose();
                }
            }
            """;

        await VerifyAsync(source, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Does_Not_Report_When_Parameterless_On_IDisposable_Interface_Field()
    {
        var source =
            """
            using System;
            using NexusLabs.Framework;

            namespace Test
            {
                public sealed class Wrapper : IDisposable
                {
                    [TransfersOwnership]
                    private readonly IDisposable _inner = null!;

                    public void Dispose() => _inner.Dispose();
                }
            }
            """;

        await VerifyAsync(source, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Does_Not_Report_When_Parameterless_On_IAsyncDisposable_Interface_Field()
    {
        var source =
            """
            using System;
            using System.Threading.Tasks;
            using NexusLabs.Framework;

            namespace Test
            {
                public sealed class Wrapper : IAsyncDisposable
                {
                    [TransfersOwnership]
                    private readonly IAsyncDisposable _inner = null!;

                    public ValueTask DisposeAsync() => _inner.DisposeAsync();
                }
            }
            """;

        await VerifyAsync(source, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Does_Not_Report_When_Targets_Supplied_On_Non_Disposable_Flag()
    {
        var source =
            """
            using System;
            using System.IO;
            using NexusLabs.Framework;

            namespace Test
            {
                public sealed class Wrapper : IDisposable
                {
                    private readonly Stream _inner = null!;

                    [TransfersOwnership(nameof(_inner))]
                    private readonly bool _takeOwnership;

                    public void Dispose()
                    {
                        if (_takeOwnership) _inner.Dispose();
                    }
                }
            }
            """;

        await VerifyAsync(source, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Does_Not_Report_When_Targets_Supplied_On_Disposable_Field()
    {
        var source =
            """
            using System;
            using System.IO;
            using NexusLabs.Framework;

            namespace Test
            {
                public sealed class Wrapper : IDisposable
                {
                    private readonly Stream _other = null!;

                    [TransfersOwnership(nameof(_other))]
                    private readonly Stream _inner = null!;

                    public void Dispose() => _inner.Dispose();
                }
            }
            """;

        await VerifyAsync(source, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Does_Not_Report_When_Generic_Field_Constrained_To_IDisposable()
    {
        var source =
            """
            using System;
            using NexusLabs.Framework;

            namespace Test
            {
                public sealed class Wrapper<T> : IDisposable
                    where T : IDisposable
                {
                    [TransfersOwnership]
                    private readonly T _value = default!;

                    public void Dispose() => _value.Dispose();
                }
            }
            """;

        await VerifyAsync(source, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Reports_For_Each_Variable_In_Multi_Variable_Field_Declaration()
    {
        var source =
            """
            using NexusLabs.Framework;

            namespace Test
            {
                public sealed class Holder
                {
                    [{|#0:TransfersOwnership|}]
                    private readonly bool _a, _b;
                }
            }
            """;

        var expected1 = new DiagnosticResult("NLF0012", DiagnosticSeverity.Warning)
            .WithLocation(0)
            .WithArguments("_a");
        var expected2 = new DiagnosticResult("NLF0012", DiagnosticSeverity.Warning)
            .WithLocation(0)
            .WithArguments("_b");

        await VerifyAsync(source, [expected1, expected2], TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Reports_When_Attribute_Is_Aliased()
    {
        var source =
            """
            using NexusLabs.Framework;
            using TO = NexusLabs.Framework.TransfersOwnershipAttribute;

            namespace Test
            {
                public sealed class Holder
                {
                    [{|#0:TO|}]
                    private readonly bool _flag;
                }
            }
            """;

        var expected = new DiagnosticResult("NLF0012", DiagnosticSeverity.Warning)
            .WithLocation(0)
            .WithArguments("_flag");

        await VerifyAsync(source, expected, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Does_Not_Report_When_No_TransfersOwnership_Attribute_Present()
    {
        var source =
            """
            namespace Test
            {
                public sealed class Holder
                {
                    private readonly bool _flag;
                }
            }
            """;

        await VerifyAsync(source, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Does_Not_Report_When_Attribute_Type_Is_Missing_From_Compilation()
    {
        // Compilation has no NexusLabs.Framework.TransfersOwnershipAttribute reference at all;
        // the analyzer must bail out cleanly without throwing or reporting spurious diagnostics.
        var source =
            """
            namespace Test
            {
                public sealed class Holder
                {
                    private readonly bool _flag;
                }
            }
            """;

        var test = new CSharpAnalyzerTest<TransfersOwnershipInertAnalyzer, DefaultVerifier>
        {
            TestCode = source,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
        };

        await test.RunAsync(TestContext.Current.CancellationToken);
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
        var test = new CSharpAnalyzerTest<TransfersOwnershipInertAnalyzer, DefaultVerifier>
        {
            TestCode = source,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
        };

        test.TestState.Sources.Add(
            ("TransfersOwnershipAttributeStub.cs", TestSources.TransfersOwnershipAttributeStub));
        test.ExpectedDiagnostics.AddRange(expected);

        await test.RunAsync(cancellationToken);
    }
}
