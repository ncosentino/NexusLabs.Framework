using System.Linq;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;

using Xunit;

namespace NexusLabs.Framework.Analyzers.Tests;

// File-local helpers — we throw InvalidOperationException instead of using
// xunit.v3 Assert.* because Microsoft.CodeAnalysis.CSharp.Analyzer.Testing.XUnit
// 1.1.2 transitively pulls in xunit 2.x, which collides with xunit.v3 on the
// Xunit.Assert type (CS0433). Helpers compile in any xunit version and any
// unhandled exception still fails the test.
file static class TestAssert
{
    public static Diagnostic SingleDiagnostic(
        System.Collections.Immutable.ImmutableArray<Diagnostic> diagnostics,
        string id)
    {
        var matches = diagnostics.Where(d => d.Id == id).ToArray();
        if (matches.Length != 1)
        {
            throw new System.InvalidOperationException(
                $"Expected exactly one diagnostic with id '{id}', found {matches.Length}. " +
                $"All diagnostics: {FormatAll(diagnostics)}");
        }
        return matches[0];
    }

    public static void AssertTrue(bool condition, string message)
    {
        if (!condition)
        {
            throw new System.InvalidOperationException("Expected true: " + message);
        }
    }

    public static void AssertFalse(bool condition, string message)
    {
        if (condition)
        {
            throw new System.InvalidOperationException("Expected false: " + message);
        }
    }

    private static string FormatAll(
        System.Collections.Immutable.ImmutableArray<Diagnostic> diagnostics) =>
        diagnostics.Length == 0
            ? "<none>"
            : string.Join("; ", diagnostics.Select(d => $"{d.Id}@{d.Location.SourceSpan} IsSuppressed={d.IsSuppressed}"));
}

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

        var diagnostic = TestAssert.SingleDiagnostic(diagnostics, "IDISP007");
        TestAssert.AssertTrue(diagnostic.IsSuppressed,
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

        var diagnostic = TestAssert.SingleDiagnostic(diagnostics, "IDISP007");
        TestAssert.AssertTrue(diagnostic.IsSuppressed,
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

        var diagnostic = TestAssert.SingleDiagnostic(diagnostics, "IDISP007");
        TestAssert.AssertTrue(diagnostic.IsSuppressed,
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

        var diagnostic = TestAssert.SingleDiagnostic(diagnostics, "IDISP007");
        TestAssert.AssertTrue(diagnostic.IsSuppressed,
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

        var diagnostic = TestAssert.SingleDiagnostic(diagnostics, "IDISP007");
        TestAssert.AssertFalse(diagnostic.IsSuppressed,
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

        var diagnostic = TestAssert.SingleDiagnostic(diagnostics, "IDISP007");
        TestAssert.AssertTrue(diagnostic.IsSuppressed,
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

                    [TransfersOwnership]
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

        var diagnostic = TestAssert.SingleDiagnostic(diagnostics, "IDISP007");
        TestAssert.AssertTrue(diagnostic.IsSuppressed,
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

                    [TransfersOwnership]
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

        var diagnostic = TestAssert.SingleDiagnostic(diagnostics, "IDISP007");
        TestAssert.AssertTrue(diagnostic.IsSuppressed,
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

                    [TransfersOwnership]
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

        var diagnostic = TestAssert.SingleDiagnostic(diagnostics, "IDISP007");
        TestAssert.AssertTrue(diagnostic.IsSuppressed,
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

        var diagnostic = TestAssert.SingleDiagnostic(diagnostics, "IDISP007");
        TestAssert.AssertFalse(diagnostic.IsSuppressed,
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

                    [TransfersOwnership]
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

        var diagnostic = TestAssert.SingleDiagnostic(diagnostics, "IDISP007");
        TestAssert.AssertFalse(diagnostic.IsSuppressed,
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

                    [TransfersOwnership]
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

        var diagnostic = TestAssert.SingleDiagnostic(diagnostics, "IDISP007");
        TestAssert.AssertFalse(diagnostic.IsSuppressed,
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

                    [TransfersOwnership]
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

        var diagnostic = TestAssert.SingleDiagnostic(diagnostics, "IDISP007");
        TestAssert.AssertFalse(diagnostic.IsSuppressed,
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

        var diagnostic = TestAssert.SingleDiagnostic(diagnostics, "XYZ9999");
        TestAssert.AssertFalse(diagnostic.IsSuppressed,
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

        var diagnostic = TestAssert.SingleDiagnostic(diagnostics, "IDISP007");
        TestAssert.AssertFalse(diagnostic.IsSuppressed,
            "v1 scope: parameter-only annotation is documentation; suppressor " +
            "requires the attribute on the field/property that the dispose call " +
            "resolves to.");
    }
}
