using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;

using NexusLabs.TUnit.Assertions.Analyzers;

namespace NexusLabs.TUnit.Assertions.Tests;

public sealed class TriedResultAssertionAnalyzerTests
{
    [Test]
    [Arguments("TriedEx", "Success")]
    [Arguments("TriedEx", "Value")]
    [Arguments("TriedEx", "Error")]
    [Arguments("TriedNullEx", "Success")]
    [Arguments("TriedNullEx", "Value")]
    [Arguments("TriedNullEx", "Error")]
    public async Task AssertThat_TriedResultMember_ReportsDiagnostic(
        string resultType,
        string propertyName,
        CancellationToken cancellationToken)
    {
        var source =
            $$"""
            using NexusLabs.Framework;
            using TUnit.Assertions;

            namespace Test
            {
                public sealed class TestClass
                {
                    public void TestMethod()
                    {
                        {{resultType}}<string> result = default;
                        Assert.That({|#0:result.{{propertyName}}|});
                    }
                }
            }

            namespace NexusLabs.Framework
            {
                public readonly struct TriedEx<T>
                {
                    public bool Success => true;
                    public T Value => default!;
                    public System.Exception Error => null!;
                }

                public readonly struct TriedNullEx<T>
                {
                    public bool Success => true;
                    public T? Value => default;
                    public System.Exception Error => null!;
                }
            }

            namespace TUnit.Assertions
            {
                public static class Assert
                {
                    public static void That<T>(T value)
                    {
                    }
                }
            }
            """;

        var expected = new DiagnosticResult("NLT0001", DiagnosticSeverity.Warning)
            .WithLocation(0)
            .WithArguments(propertyName);

        await AnalyzerVerifier<TriedResultAssertionAnalyzer>.VerifyAsync(
            source,
            expected,
            cancellationToken);
    }

    [Test]
    public async Task AssertThat_CompleteTriedResult_NoDiagnostic(
        CancellationToken cancellationToken)
    {
        var source =
            """
            using NexusLabs.Framework;
            using TUnit.Assertions;

            namespace Test
            {
                public sealed class TestClass
                {
                    public void TestMethod()
                    {
                        TriedEx<string> result = default;
                        Assert.That(result);
                    }
                }
            }

            namespace NexusLabs.Framework
            {
                public readonly struct TriedEx<T>
                {
                    public bool Success => true;
                }
            }

            namespace TUnit.Assertions
            {
                public static class Assert
                {
                    public static void That<T>(T value)
                    {
                    }
                }
            }
            """;

        await AnalyzerVerifier<TriedResultAssertionAnalyzer>.VerifyAsync(
            source,
            cancellationToken);
    }

    [Test]
    public async Task NonTUnitAssert_TriedResultMember_NoDiagnostic(
        CancellationToken cancellationToken)
    {
        var source =
            """
            using NexusLabs.Framework;

            namespace Test
            {
                public sealed class TestClass
                {
                    public void TestMethod()
                    {
                        TriedEx<string> result = default;
                        Assert.That(result.Success);
                    }
                }

                public static class Assert
                {
                    public static void That<T>(T value)
                    {
                    }
                }
            }

            namespace NexusLabs.Framework
            {
                public readonly struct TriedEx<T>
                {
                    public bool Success => true;
                }
            }
            """;

        await AnalyzerVerifier<TriedResultAssertionAnalyzer>.VerifyAsync(
            source,
            cancellationToken);
    }

    [Test]
    public async Task TUnitAssert_LookalikeResult_NoDiagnostic(
        CancellationToken cancellationToken)
    {
        var source =
            """
            using TUnit.Assertions;

            namespace Test
            {
                public sealed class TestClass
                {
                    public void TestMethod()
                    {
                        TriedEx<string> result = default;
                        Assert.That(result.Success);
                    }
                }

                public readonly struct TriedEx<T>
                {
                    public bool Success => true;
                }
            }

            namespace TUnit.Assertions
            {
                public static class Assert
                {
                    public static void That<T>(T value)
                    {
                    }
                }
            }
            """;

        await AnalyzerVerifier<TriedResultAssertionAnalyzer>.VerifyAsync(
            source,
            cancellationToken);
    }

    [Test]
    public async Task GuardedMemberAccess_OutsideAssertThat_NoDiagnostic(
        CancellationToken cancellationToken)
    {
        var source =
            """
            using NexusLabs.Framework;

            namespace Test
            {
                public sealed class TestClass
                {
                    public string? TestMethod(TriedEx<string> result)
                    {
                        return result.Success
                            ? result.Value
                            : null;
                    }
                }
            }

            namespace NexusLabs.Framework
            {
                public readonly struct TriedEx<T>
                {
                    public bool Success => true;
                    public T Value => default!;
                }
            }
            """;

        await AnalyzerVerifier<TriedResultAssertionAnalyzer>.VerifyAsync(
            source,
            cancellationToken);
    }
}
