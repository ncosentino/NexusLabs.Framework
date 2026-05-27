using System.Linq;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;

using Xunit;

namespace NexusLabs.Framework.Analyzers.Tests;

public sealed class TransfersOwnershipDisposeSuppressorTests
{
    [Fact]
    public async Task Shape_B_Suppresses_When_DisposeTarget_Field_Is_Annotated()
    {
        var source =
            """
            using System.IO;
            using NexusLabs.Framework;

            namespace Tests
            {
                public sealed class Adapter : System.IDisposable
                {
                    [TransfersOwnership]
                    private readonly Stream _inner = Stream.Null;

                    public void Dispose() => _inner.Dispose();
                }
            }
            """;

        var diagnostics = await SuppressorHarness.AnalyzeAsync(
            source,
            new FakeIDisp007Analyzer(),
            new TransfersOwnershipDisposeSuppressor());

        var diagnostic = Assert.Single(diagnostics.Where(d => d.Id == "IDISP007"));
        Assert.True(diagnostic.IsSuppressed,
            "Disposing a field annotated with [TransfersOwnership] must be suppressed.");
    }

    [Fact]
    public async Task Shape_B_Suppresses_When_DisposeTarget_Property_Is_Annotated()
    {
        var source =
            """
            using System.IO;
            using NexusLabs.Framework;

            namespace Tests
            {
                public sealed class Adapter : System.IDisposable
                {
                    [TransfersOwnership]
                    private Stream Inner { get; } = Stream.Null;

                    public void Dispose() => Inner.Dispose();
                }
            }
            """;

        var diagnostics = await SuppressorHarness.AnalyzeAsync(
            source,
            new FakeIDisp007Analyzer(),
            new TransfersOwnershipDisposeSuppressor());

        var diagnostic = Assert.Single(diagnostics.Where(d => d.Id == "IDISP007"));
        Assert.True(diagnostic.IsSuppressed,
            "Disposing a property annotated with [TransfersOwnership] must be suppressed.");
    }

    [Fact]
    public async Task Shape_B_Suppresses_When_DisposeTarget_Is_This_Qualified_Field()
    {
        var source =
            """
            using System.IO;
            using NexusLabs.Framework;

            namespace Tests
            {
                public sealed class Adapter : System.IDisposable
                {
                    [TransfersOwnership]
                    private readonly Stream _inner = Stream.Null;

                    public void Dispose() => this._inner.Dispose();
                }
            }
            """;

        var diagnostics = await SuppressorHarness.AnalyzeAsync(
            source,
            new FakeIDisp007Analyzer(),
            new TransfersOwnershipDisposeSuppressor());

        var diagnostic = Assert.Single(diagnostics.Where(d => d.Id == "IDISP007"));
        Assert.True(diagnostic.IsSuppressed,
            "Disposing this.<annotated-field> must be suppressed.");
    }

    [Fact]
    public async Task Shape_B_Suppresses_DisposeAsync_When_Field_Is_Annotated()
    {
        var source =
            """
            using System.IO;
            using System.Threading.Tasks;
            using NexusLabs.Framework;

            namespace Tests
            {
                public sealed class Adapter : System.IAsyncDisposable
                {
                    [TransfersOwnership]
                    private readonly Stream _inner = Stream.Null;

                    public ValueTask DisposeAsync() => _inner.DisposeAsync();
                }
            }
            """;

        var diagnostics = await SuppressorHarness.AnalyzeAsync(
            source,
            new FakeIDisp007Analyzer(),
            new TransfersOwnershipDisposeSuppressor());

        var diagnostic = Assert.Single(diagnostics.Where(d => d.Id == "IDISP007"));
        Assert.True(diagnostic.IsSuppressed,
            "DisposeAsync on annotated field must be suppressed.");
    }

    [Fact]
    public async Task Shape_B_Does_Not_Suppress_When_Field_Lacks_Attribute()
    {
        var source =
            """
            using System.IO;

            namespace Tests
            {
                public sealed class Adapter : System.IDisposable
                {
                    private readonly Stream _inner = Stream.Null;

                    public void Dispose() => _inner.Dispose();
                }
            }
            """;

        var diagnostics = await SuppressorHarness.AnalyzeAsync(
            source,
            new FakeIDisp007Analyzer(),
            new TransfersOwnershipDisposeSuppressor());

        var diagnostic = Assert.Single(diagnostics.Where(d => d.Id == "IDISP007"));
        Assert.False(diagnostic.IsSuppressed,
            "Disposing a non-annotated field must NOT be suppressed.");
    }

    [Fact]
    public async Task Shape_A_Suppresses_When_If_Condition_Is_Annotated_Bool_Field()
    {
        var source =
            """
            using System.IO;
            using NexusLabs.Framework;

            namespace Tests
            {
                public sealed class Adapter : System.IDisposable
                {
                    private readonly Stream _inner = Stream.Null;

                    [TransfersOwnership(nameof(_inner))]
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

        var diagnostics = await SuppressorHarness.AnalyzeAsync(
            source,
            new FakeIDisp007Analyzer(),
            new TransfersOwnershipDisposeSuppressor());

        var diagnostic = Assert.Single(diagnostics.Where(d => d.Id == "IDISP007"));
        Assert.True(diagnostic.IsSuppressed,
            "Disposing inside if (<annotated-bool>) must be suppressed.");
    }

    [Fact]
    public async Task Shape_A_Suppresses_When_If_Condition_Is_This_Qualified()
    {
        var source =
            """
            using System.IO;
            using NexusLabs.Framework;

            namespace Tests
            {
                public sealed class Adapter : System.IDisposable
                {
                    private readonly Stream _inner = Stream.Null;

                    [TransfersOwnership(nameof(_inner))]
                    private readonly bool _takeOwnership;

                    public void Dispose()
                    {
                        if (this._takeOwnership)
                        {
                            _inner.Dispose();
                        }
                    }
                }
            }
            """;

        var diagnostics = await SuppressorHarness.AnalyzeAsync(
            source,
            new FakeIDisp007Analyzer(),
            new TransfersOwnershipDisposeSuppressor());

        var diagnostic = Assert.Single(diagnostics.Where(d => d.Id == "IDISP007"));
        Assert.True(diagnostic.IsSuppressed,
            "Disposing inside if (this.<annotated-bool>) must be suppressed.");
    }

    [Fact]
    public async Task Shape_A_Suppresses_When_Annotated_Field_Is_AndAlso_Operand()
    {
        var source =
            """
            using System.IO;
            using NexusLabs.Framework;

            namespace Tests
            {
                public sealed class Adapter : System.IDisposable
                {
                    private readonly Stream _inner = Stream.Null;

                    [TransfersOwnership(nameof(_inner))]
                    private readonly bool _takeOwnership;

                    private bool _disposed;

                    public void Dispose()
                    {
                        if (!_disposed && _takeOwnership)
                        {
                            _inner.Dispose();
                        }
                        _disposed = true;
                    }
                }
            }
            """;

        var diagnostics = await SuppressorHarness.AnalyzeAsync(
            source,
            new FakeIDisp007Analyzer(),
            new TransfersOwnershipDisposeSuppressor());

        var diagnostic = Assert.Single(diagnostics.Where(d => d.Id == "IDISP007"));
        Assert.True(diagnostic.IsSuppressed,
            "Disposing inside if (otherFlag && <annotated-bool>) must be suppressed.");
    }

    [Fact]
    public async Task Shape_A_Suppresses_Single_Statement_If_Without_Braces()
    {
        var source =
            """
            using System.IO;
            using NexusLabs.Framework;

            namespace Tests
            {
                public sealed class Adapter : System.IDisposable
                {
                    private readonly Stream _inner = Stream.Null;

                    [TransfersOwnership(nameof(_inner))]
                    private readonly bool _takeOwnership;

                    public void Dispose()
                    {
                        if (_takeOwnership)
                            _inner.Dispose();
                    }
                }
            }
            """;

        var diagnostics = await SuppressorHarness.AnalyzeAsync(
            source,
            new FakeIDisp007Analyzer(),
            new TransfersOwnershipDisposeSuppressor());

        var diagnostic = Assert.Single(diagnostics.Where(d => d.Id == "IDISP007"));
        Assert.True(diagnostic.IsSuppressed,
            "Braceless single-statement if-body must still be detected.");
    }

    [Fact]
    public async Task Shape_A_Does_Not_Suppress_When_Bool_Field_Lacks_Attribute()
    {
        var source =
            """
            using System.IO;

            namespace Tests
            {
                public sealed class Adapter : System.IDisposable
                {
                    private readonly Stream _inner = Stream.Null;
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

        var diagnostics = await SuppressorHarness.AnalyzeAsync(
            source,
            new FakeIDisp007Analyzer(),
            new TransfersOwnershipDisposeSuppressor());

        var diagnostic = Assert.Single(diagnostics.Where(d => d.Id == "IDISP007"));
        Assert.False(diagnostic.IsSuppressed,
            "Without [TransfersOwnership] on the bool, NLFSUP001 must not fire — " +
            "the suppressor must NOT match on identifier names.");
    }

    [Fact]
    public async Task Does_Not_Suppress_When_Dispose_Is_Outside_Any_If_Block()
    {
        var source =
            """
            using System.IO;
            using NexusLabs.Framework;

            namespace Tests
            {
                public sealed class Adapter : System.IDisposable
                {
                    private readonly Stream _inner = Stream.Null;

                    [TransfersOwnership(nameof(_inner))]
                    private readonly bool _takeOwnership;

                    public void Dispose()
                    {
                        var unrelated = _takeOwnership;
                        _inner.Dispose();
                    }
                }
            }
            """;

        var diagnostics = await SuppressorHarness.AnalyzeAsync(
            source,
            new FakeIDisp007Analyzer(),
            new TransfersOwnershipDisposeSuppressor());

        var diagnostic = Assert.Single(diagnostics.Where(d => d.Id == "IDISP007"));
        Assert.False(diagnostic.IsSuppressed,
            "Annotated bool merely READ in the method body does not authorise " +
            "an unguarded dispose call elsewhere in that method.");
    }

    [Fact]
    public async Task Does_Not_Suppress_Disjunction_Even_With_Annotated_Field()
    {
        var source =
            """
            using System.IO;
            using NexusLabs.Framework;

            namespace Tests
            {
                public sealed class Adapter : System.IDisposable
                {
                    private readonly Stream _inner = Stream.Null;

                    [TransfersOwnership(nameof(_inner))]
                    private readonly bool _takeOwnership;

                    private readonly bool _someOtherFlag = false;

                    public void Dispose()
                    {
                        if (_someOtherFlag || _takeOwnership)
                        {
                            _inner.Dispose();
                        }
                    }
                }
            }
            """;

        var diagnostics = await SuppressorHarness.AnalyzeAsync(
            source,
            new FakeIDisp007Analyzer(),
            new TransfersOwnershipDisposeSuppressor());

        var diagnostic = Assert.Single(diagnostics.Where(d => d.Id == "IDISP007"));
        Assert.False(diagnostic.IsSuppressed,
            "OR disjunction does not guarantee the annotated flag was true at " +
            "the dispose site, so suppression must NOT fire.");
    }

    [Fact]
    public async Task Does_Not_Suppress_When_Dispose_Is_In_Else_Branch()
    {
        var source =
            """
            using System.IO;
            using NexusLabs.Framework;

            namespace Tests
            {
                public sealed class Adapter : System.IDisposable
                {
                    private readonly Stream _inner = Stream.Null;

                    [TransfersOwnership(nameof(_inner))]
                    private readonly bool _takeOwnership;

                    public void Dispose()
                    {
                        if (_takeOwnership)
                        {
                            // intentionally empty
                        }
                        else
                        {
                            _inner.Dispose();
                        }
                    }
                }
            }
            """;

        var diagnostics = await SuppressorHarness.AnalyzeAsync(
            source,
            new FakeIDisp007Analyzer(),
            new TransfersOwnershipDisposeSuppressor());

        var diagnostic = Assert.Single(diagnostics.Where(d => d.Id == "IDISP007"));
        Assert.False(diagnostic.IsSuppressed,
            "Dispose in else-branch runs when the annotated flag is FALSE, so " +
            "the ownership-transfer rationale does not apply.");
    }

    [Fact]
    public async Task Suppression_Is_Scoped_To_IDISP007_Only()
    {
        var source =
            """
            using System.IO;
            using NexusLabs.Framework;

            namespace Tests
            {
                public sealed class Adapter : System.IDisposable
                {
                    [TransfersOwnership]
                    private readonly Stream _inner = Stream.Null;

                    public void Dispose() => _inner.Dispose();
                }
            }
            """;

        var diagnostics = await SuppressorHarness.AnalyzeAsync(
            source,
            new FakeAnalyzerEmittingId("XYZ9999"),
            new TransfersOwnershipDisposeSuppressor());

        var diagnostic = Assert.Single(diagnostics.Where(d => d.Id == "XYZ9999"));
        Assert.False(diagnostic.IsSuppressed,
            "Suppressor declared SuppressedDiagnosticId='IDISP007'; it must NOT " +
            "leak across diagnostic ids.");
    }

    [Fact]
    public async Task Parameter_Only_Annotation_Does_Not_Trigger_Shape_B()
    {
        // Documents v1 scope: [TransfersOwnership] on a parameter alone does NOT
        // currently propagate to dispose-target suppression via dataflow. The
        // user must also annotate the backing field. If we ever add dataflow
        // analysis, this test should be updated (or deleted) accordingly.
        var source =
            """
            using System.IO;
            using NexusLabs.Framework;

            namespace Tests
            {
                public sealed class Adapter : System.IDisposable
                {
                    private readonly Stream _inner;

                    public Adapter([TransfersOwnership] Stream inner)
                    {
                        _inner = inner;
                    }

                    public void Dispose() => _inner.Dispose();
                }
            }
            """;

        var diagnostics = await SuppressorHarness.AnalyzeAsync(
            source,
            new FakeIDisp007Analyzer(),
            new TransfersOwnershipDisposeSuppressor());

        var diagnostic = Assert.Single(diagnostics.Where(d => d.Id == "IDISP007"));
        Assert.False(diagnostic.IsSuppressed,
            "v1 scope: parameter-only annotation is documentation; suppressor " +
            "requires the attribute on the field/property that the dispose call " +
            "resolves to.");
    }

    [Fact]
    public async Task Shape_A_Suppresses_When_If_Condition_Is_Annotated_Primary_Ctor_Parameter()
    {
        var source =
            """
            using System.IO;
            using NexusLabs.Framework;

            namespace Tests
            {
                public sealed class Adapter(
                    Stream _inner,
                    [TransfersOwnership(nameof(_inner))] bool _takeOwnership) : System.IDisposable
                {
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

        var diagnostics = await SuppressorHarness.AnalyzeAsync(
            source,
            new FakeIDisp007Analyzer(),
            new TransfersOwnershipDisposeSuppressor());

        var diagnostic = Assert.Single(diagnostics.Where(d => d.Id == "IDISP007"));
        Assert.True(diagnostic.IsSuppressed,
            "Disposing inside if (<annotated-primary-ctor-bool-param>) must be suppressed.");
    }

    [Fact]
    public async Task Shape_A_Suppresses_AndAlso_When_Operand_Is_Annotated_Primary_Ctor_Parameter()
    {
        // Production shape from StreamWithLength: `if (disposing && _takeOwnership)`
        // where _takeOwnership is a primary-constructor bool parameter annotated
        // with [TransfersOwnership]. The suppressor must recurse into the AndAlso
        // operands and resolve _takeOwnership to the IParameterSymbol.
        var source =
            """
            using System.IO;
            using NexusLabs.Framework;

            namespace Tests
            {
                public sealed class Adapter(
                    Stream _inner,
                    [TransfersOwnership(nameof(_inner))] bool _takeOwnership) : System.IDisposable
                {
                    private bool _otherFlag = true;

                    public void Dispose()
                    {
                        if (_otherFlag && _takeOwnership)
                        {
                            _inner.Dispose();
                        }
                    }
                }
            }
            """;

        var diagnostics = await SuppressorHarness.AnalyzeAsync(
            source,
            new FakeIDisp007Analyzer(),
            new TransfersOwnershipDisposeSuppressor());

        var diagnostic = Assert.Single(diagnostics.Where(d => d.Id == "IDISP007"));
        Assert.True(diagnostic.IsSuppressed,
            "Disposing inside if (<other> && <annotated-primary-ctor-bool-param>) must be suppressed.");
    }

    [Fact]
    public async Task Shape_B_Suppresses_When_Dispose_Is_Inside_Await_ConfigureAwait()
    {
        // Production shape from decorators like LeasedAsyncDbConnection and
        // OpenTrackingDecorator: `await _inner.DisposeAsync().ConfigureAwait(false);`
        // inside a try/finally with an Interlocked guard. The real
        // IDisposableAnalyzers IDISP007 anchors its diagnostic on the
        // surrounding AwaitExpression, not on the inner DisposeAsync()
        // invocation, so the suppressor must descend into the reported node
        // to find the dispose call - walking up alone is insufficient.
        var source =
            """
            using System;
            using System.Threading;
            using System.Threading.Tasks;
            using NexusLabs.Framework;

            namespace Tests
            {
                public sealed class Decorator : IAsyncDisposable
                {
                    [TransfersOwnership]
                    private readonly IAsyncDisposable _inner;
                    private int _disposed;

                    public Decorator(IAsyncDisposable inner)
                    {
                        _inner = inner;
                    }

                    public async ValueTask DisposeAsync()
                    {
                        if (Interlocked.Exchange(ref _disposed, 1) != 0)
                        {
                            return;
                        }

                        try
                        {
                            await _inner.DisposeAsync().ConfigureAwait(false);
                        }
                        finally
                        {
                            GC.SuppressFinalize(this);
                        }
                    }
                }
            }
            """;

        var diagnostics = await SuppressorHarness.AnalyzeAsync(
            source,
            new FakeIDisp007AwaitAnalyzer(),
            new TransfersOwnershipDisposeSuppressor());

        var diagnostic = Assert.Single(diagnostics.Where(d => d.Id == "IDISP007"));
        Assert.True(diagnostic.IsSuppressed,
            "Disposing an annotated [TransfersOwnership] field via " +
            "`await _field.DisposeAsync().ConfigureAwait(false)` must be suppressed, " +
            "even when the underlying analyzer anchors the diagnostic at the await " +
            "expression rather than at the inner invocation.");
    }

    [Fact]
    public async Task Shape_B_Suppresses_Await_Dispose_Without_ConfigureAwait()
    {
        // Variant of the await-shape test without the ConfigureAwait wrapper.
        // Confirms the descend-into-descendants logic is not accidentally
        // coupled to the presence of a chained .ConfigureAwait call.
        var source =
            """
            using System;
            using System.Threading.Tasks;
            using NexusLabs.Framework;

            namespace Tests
            {
                public sealed class Decorator : IAsyncDisposable
                {
                    [TransfersOwnership]
                    private readonly IAsyncDisposable _inner;

                    public Decorator(IAsyncDisposable inner)
                    {
                        _inner = inner;
                    }

                    public async ValueTask DisposeAsync()
                    {
                        await _inner.DisposeAsync();
                    }
                }
            }
            """;

        var diagnostics = await SuppressorHarness.AnalyzeAsync(
            source,
            new FakeIDisp007AwaitAnalyzer(),
            new TransfersOwnershipDisposeSuppressor());

        var diagnostic = Assert.Single(diagnostics.Where(d => d.Id == "IDISP007"));
        Assert.True(diagnostic.IsSuppressed,
            "Disposing an annotated [TransfersOwnership] field via " +
            "`await _field.DisposeAsync()` must be suppressed regardless of " +
            "the presence of a ConfigureAwait wrapper.");
    }

    [Fact]
    public async Task Shape_A_Strict_Suppresses_When_Dispose_Receiver_Matches_Target()
    {
        var source =
            """
            using System.IO;
            using NexusLabs.Framework;

            namespace Tests
            {
                public sealed class Adapter : System.IDisposable
                {
                    private readonly Stream _inner = Stream.Null;

                    [TransfersOwnership(nameof(_inner))]
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

        var diagnostics = await SuppressorHarness.AnalyzeAsync(
            source,
            new FakeIDisp007Analyzer(),
            new TransfersOwnershipDisposeSuppressor());

        var diagnostic = Assert.Single(diagnostics.Where(d => d.Id == "IDISP007"));
        Assert.True(diagnostic.IsSuppressed,
            "Dispose receiver named in [TransfersOwnership(nameof(...))] must be suppressed.");
    }

    [Fact]
    public async Task Shape_A_Strict_Does_Not_Suppress_When_Dispose_Receiver_Not_In_Targets()
    {
        // Canonical bug class strict targeting fixes: two disposables guarded
        // by the same ownership flag, but only ONE was actually transferred.
        // Loose mode (no targets) would suppress both; strict mode correctly
        // leaves the non-target dispose exposed.
        var source =
            """
            using System.IO;
            using NexusLabs.Framework;

            namespace Tests
            {
                public sealed class Adapter : System.IDisposable
                {
                    private readonly Stream _ownedStream = Stream.Null;
                    private readonly Stream _otherStream = Stream.Null;

                    [TransfersOwnership(nameof(_ownedStream))]
                    private readonly bool _takeOwnership;

                    public void Dispose()
                    {
                        if (_takeOwnership)
                        {
                            _ownedStream.Dispose();
                            _otherStream.Dispose();
                        }
                    }
                }
            }
            """;

        var diagnostics = await SuppressorHarness.AnalyzeAsync(
            source,
            new FakeIDisp007Analyzer(),
            new TransfersOwnershipDisposeSuppressor());

        var idisp = diagnostics.Where(d => d.Id == "IDISP007").ToArray();
        Assert.Equal(2, idisp.Length);

        var suppressed = idisp.Single(d => d.IsSuppressed);
        var notSuppressed = idisp.Single(d => !d.IsSuppressed);

        var cancellationToken = TestContext.Current.CancellationToken;
        var suppressedText = suppressed.Location.SourceTree!
            .GetText(cancellationToken).ToString(suppressed.Location.SourceSpan);
        var notSuppressedText = notSuppressed.Location.SourceTree!
            .GetText(cancellationToken).ToString(notSuppressed.Location.SourceSpan);

        Assert.Contains("_ownedStream", suppressedText);
        Assert.Contains("_otherStream", notSuppressedText);
    }

    [Fact]
    public async Task Shape_A_Strict_Suppresses_When_Receiver_Matches_One_Of_Multiple_Targets()
    {
        var source =
            """
            using System.IO;
            using NexusLabs.Framework;

            namespace Tests
            {
                public sealed class Adapter : System.IDisposable
                {
                    private readonly Stream _streamA = Stream.Null;
                    private readonly Stream _streamB = Stream.Null;

                    [TransfersOwnership(nameof(_streamA), nameof(_streamB))]
                    private readonly bool _takeOwnership;

                    public void Dispose()
                    {
                        if (_takeOwnership)
                        {
                            _streamA.Dispose();
                            _streamB.Dispose();
                        }
                    }
                }
            }
            """;

        var diagnostics = await SuppressorHarness.AnalyzeAsync(
            source,
            new FakeIDisp007Analyzer(),
            new TransfersOwnershipDisposeSuppressor());

        var idisp = diagnostics.Where(d => d.Id == "IDISP007").ToArray();
        Assert.Equal(2, idisp.Length);
        Assert.All(idisp, d => Assert.True(d.IsSuppressed,
            "Multiple targets: every listed disposable inside the guard must be suppressed."));
    }

    [Fact]
    public async Task Shape_A_Does_Not_Suppress_When_Flag_Has_Empty_Targets()
    {
        // Strict-mode regression lock: [TransfersOwnership] with no targets
        // applied to a flag MUST NOT suppress anything. The loose "any
        // dispose inside the guard" behaviour is intentionally removed —
        // see Shape_A_Strict_Does_Not_Suppress_When_Dispose_Receiver_Not_In_Targets
        // for the bug class this fixes.
        var source =
            """
            using System.IO;
            using NexusLabs.Framework;

            namespace Tests
            {
                public sealed class Adapter : System.IDisposable
                {
                    private readonly Stream _inner = Stream.Null;

                    [TransfersOwnership]
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

        var diagnostics = await SuppressorHarness.AnalyzeAsync(
            source,
            new FakeIDisp007Analyzer(),
            new TransfersOwnershipDisposeSuppressor());

        var diagnostic = Assert.Single(diagnostics.Where(d => d.Id == "IDISP007"));
        Assert.False(diagnostic.IsSuppressed,
            "Empty targets list must not suppress. Loose Shape A is removed.");
    }

    [Fact]
    public async Task Shape_A_Strict_Suppresses_With_Primary_Ctor_Parameter_Target()
    {
        // Production shape from StreamWithLength: primary-ctor parameter
        // annotated with [TransfersOwnership(nameof(_streamToWrap))] gates
        // disposal of another primary-ctor parameter that captures a stream.
        var source =
            """
            using System.IO;
            using NexusLabs.Framework;

            namespace Tests
            {
                public sealed class Wrapper(
                    Stream _streamToWrap,
                    [TransfersOwnership(nameof(_streamToWrap))] bool _takeOwnership) : System.IDisposable
                {
                    public void Dispose()
                    {
                        if (_takeOwnership)
                        {
                            _streamToWrap.Dispose();
                        }
                    }
                }
            }
            """;

        var diagnostics = await SuppressorHarness.AnalyzeAsync(
            source,
            new FakeIDisp007Analyzer(),
            new TransfersOwnershipDisposeSuppressor());

        var diagnostic = Assert.Single(diagnostics.Where(d => d.Id == "IDISP007"));
        Assert.True(diagnostic.IsSuppressed,
            "Strict-targeting via nameof on a primary-ctor parameter must suppress.");
    }

    [Fact]
    public async Task Shape_A_Strict_Suppresses_With_This_Qualified_Receiver()
    {
        // nameof(this._inner) lowers to "_inner", so this.<field>.Dispose()
        // must still match a target list declared as nameof(_inner).
        var source =
            """
            using System.IO;
            using NexusLabs.Framework;

            namespace Tests
            {
                public sealed class Adapter : System.IDisposable
                {
                    private readonly Stream _inner = Stream.Null;

                    [TransfersOwnership(nameof(_inner))]
                    private readonly bool _takeOwnership;

                    public void Dispose()
                    {
                        if (_takeOwnership)
                        {
                            this._inner.Dispose();
                        }
                    }
                }
            }
            """;

        var diagnostics = await SuppressorHarness.AnalyzeAsync(
            source,
            new FakeIDisp007Analyzer(),
            new TransfersOwnershipDisposeSuppressor());

        var diagnostic = Assert.Single(diagnostics.Where(d => d.Id == "IDISP007"));
        Assert.True(diagnostic.IsSuppressed,
            "Strict-targeting must match `this._field.Dispose()` against nameof(_field).");
    }
}
