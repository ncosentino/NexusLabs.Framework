using System.Threading.Tasks;

using Microsoft.CodeAnalysis;

using NexusLabs.Framework.Analyzers;

using Xunit;

namespace NexusLabs.Framework.Analyzers.Tests;

public sealed class TryPrefixedMethodReturnTypeAnalyzerTests
{
    [Fact]
    public async Task TryAsync_ReturnsTaskOfTriedEx_NoDiagnostic()
    {
        var source =
            """
            using System.Threading.Tasks;
            using NexusLabs.Framework;

            namespace App
            {
                public sealed class C
                {
                    public Task<TriedEx<int>> TryGetAsync() => Task.FromResult(new TriedEx<int>());
                }
            }
            """ + TestSources.TriedExStubs;

        await AnalyzerVerifier<TryPrefixedMethodReturnTypeAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task TryAsync_ReturnsTaskOfTriedNullEx_NoDiagnostic()
    {
        var source =
            """
            using System.Threading.Tasks;
            using NexusLabs.Framework;

            namespace App
            {
                public sealed class C
                {
                    public Task<TriedNullEx<string>> TryFindAsync() => Task.FromResult(new TriedNullEx<string>());
                }
            }
            """ + TestSources.TriedExStubs;

        await AnalyzerVerifier<TryPrefixedMethodReturnTypeAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task TryAsync_ReturnsTaskOfExceptionNullable_NoDiagnostic()
    {
        var source =
            """
            using System;
            using System.Threading.Tasks;

            namespace App
            {
                public sealed class C
                {
                    public Task<Exception?> TryDoAsync() => Task.FromResult<Exception?>(null);
                }
            }
            """;

        await AnalyzerVerifier<TryPrefixedMethodReturnTypeAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Try_ReturnsExceptionSync_NoDiagnostic()
    {
        var source =
            """
            using System;

            namespace App
            {
                public sealed class C
                {
                    public Exception? TryDo() => null;
                }
            }
            """;

        await AnalyzerVerifier<TryPrefixedMethodReturnTypeAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Try_ReturnsTriedExSync_NoDiagnostic()
    {
        var source =
            """
            using NexusLabs.Framework;

            namespace App
            {
                public sealed class C
                {
                    public TriedEx<int> TryGet() => new TriedEx<int>();
                }
            }
            """ + TestSources.TriedExStubs;

        await AnalyzerVerifier<TryPrefixedMethodReturnTypeAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Try_ReturnsValueTaskOfTriedEx_NoDiagnostic()
    {
        var source =
            """
            using System.Threading.Tasks;
            using NexusLabs.Framework;

            namespace App
            {
                public sealed class C
                {
                    public ValueTask<TriedEx<int>> TryGetAsync() => new ValueTask<TriedEx<int>>(new TriedEx<int>());
                }
            }
            """ + TestSources.TriedExStubs;

        await AnalyzerVerifier<TryPrefixedMethodReturnTypeAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task TryGetAsync_ReturnsTaskOfNullableT_Reports()
    {
        var source =
            """
            using System.Threading.Tasks;

            namespace App
            {
                public sealed class C
                {
                    public Task<string?> {|#0:TryGetAsync|}() => Task.FromResult<string?>(null);
                }
            }
            """;

        var expected = AnalyzerVerifier<TryPrefixedMethodReturnTypeAnalyzer>
            .Diagnostic(DiagnosticDescriptors.TryPrefixedMethodMustReturnTryResultType)
            .WithLocation(0)
            .WithArguments("TryGetAsync", "System.Threading.Tasks.Task<string?>");

        await AnalyzerVerifier<TryPrefixedMethodReturnTypeAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task Try_ReturnsBool_Reports()
    {
        var source =
            """
            namespace App
            {
                public sealed class C
                {
                    public bool {|#0:TryDo|}() => true;
                }
            }
            """;

        var expected = AnalyzerVerifier<TryPrefixedMethodReturnTypeAnalyzer>
            .Diagnostic(DiagnosticDescriptors.TryPrefixedMethodMustReturnTryResultType)
            .WithLocation(0)
            .WithArguments("TryDo", "bool");

        await AnalyzerVerifier<TryPrefixedMethodReturnTypeAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task TryParse_BclStyleBoolWithOut_Reports()
    {
        var source =
            """
            namespace App
            {
                public sealed class C
                {
                    public bool {|#0:TryParse|}(string s, out int value)
                    {
                        value = 0;
                        return false;
                    }
                }
            }
            """;

        var expected = AnalyzerVerifier<TryPrefixedMethodReturnTypeAnalyzer>
            .Diagnostic(DiagnosticDescriptors.TryPrefixedMethodMustReturnTryResultType)
            .WithLocation(0)
            .WithArguments("TryParse", "bool");

        await AnalyzerVerifier<TryPrefixedMethodReturnTypeAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task Try_ReturnsVoid_Reports()
    {
        var source =
            """
            namespace App
            {
                public sealed class C
                {
                    public void {|#0:TryDo|}() { }
                }
            }
            """;

        var expected = AnalyzerVerifier<TryPrefixedMethodReturnTypeAnalyzer>
            .Diagnostic(DiagnosticDescriptors.TryPrefixedMethodMustReturnTryResultType)
            .WithLocation(0)
            .WithArguments("TryDo", "void");

        await AnalyzerVerifier<TryPrefixedMethodReturnTypeAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task Try_ReturnsTaskNonGeneric_Reports()
    {
        var source =
            """
            using System.Threading.Tasks;

            namespace App
            {
                public sealed class C
                {
                    public Task {|#0:TryDoAsync|}() => Task.CompletedTask;
                }
            }
            """;

        var expected = AnalyzerVerifier<TryPrefixedMethodReturnTypeAnalyzer>
            .Diagnostic(DiagnosticDescriptors.TryPrefixedMethodMustReturnTryResultType)
            .WithLocation(0)
            .WithArguments("TryDoAsync", "System.Threading.Tasks.Task");

        await AnalyzerVerifier<TryPrefixedMethodReturnTypeAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task Try_ReturnsLongNullable_Reports()
    {
        var source =
            """
            namespace App
            {
                public sealed class C
                {
                    public long? {|#0:TryCount|}() => null;
                }
            }
            """;

        var expected = AnalyzerVerifier<TryPrefixedMethodReturnTypeAnalyzer>
            .Diagnostic(DiagnosticDescriptors.TryPrefixedMethodMustReturnTryResultType)
            .WithLocation(0)
            .WithArguments("TryCount", "long?");

        await AnalyzerVerifier<TryPrefixedMethodReturnTypeAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task NonTryName_DoesNotReport()
    {
        var source =
            """
            using System.Threading.Tasks;

            namespace App
            {
                public sealed class C
                {
                    public Task<string?> GetAsync() => Task.FromResult<string?>(null);
                    public bool ContainsItem() => false;
                    public void Configure() { }
                }
            }
            """;

        await AnalyzerVerifier<TryPrefixedMethodReturnTypeAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task MethodNamedExactlyTry_DoesNotReport()
    {
        var source =
            """
            namespace App
            {
                public sealed class C
                {
                    public bool Try() => true;
                }
            }
            """;

        await AnalyzerVerifier<TryPrefixedMethodReturnTypeAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task TrylowercaseSuffix_DoesNotReport()
    {
        var source =
            """
            namespace App
            {
                public sealed class C
                {
                    public bool Trythis() => true;
                }
            }
            """;

        await AnalyzerVerifier<TryPrefixedMethodReturnTypeAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task UnderscoredName_TestStyle_DoesNotReport()
    {
        // The codebase uses MethodUnderTest_Scenario_Expectation for tests; the first
        // underscore-separated segment is the SUT name, not an API contract claim.
        // Names containing an underscore are excluded regardless of return type.
        var source =
            """
            using System.Threading.Tasks;

            namespace App.Tests
            {
                public sealed class Tests
                {
                    public Task TryGetAsync_NotFound_ReturnsNull() => Task.CompletedTask;
                    public Task TryParse_EmptyInput_Reports() => Task.CompletedTask;
                    public Task TryAcquireAsync_TimesOut_ReturnsNull() => Task.CompletedTask;
                    public void TryDo_Always_Throws() { }
                    public bool TryParse_AllowsUnderscore() => true;
                }
            }
            """;

        await AnalyzerVerifier<TryPrefixedMethodReturnTypeAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task MethodOnNexusLabsFrameworkTryHelper_DoesNotReport()
    {
        // Methods on NexusLabs.Framework.Try itself produce the Try-result types
        // and are explicitly exempt from this rule.
        var source =
            """
            namespace NexusLabs.Framework
            {
                public static class Try
                {
                    public static bool TrySomething() => true;
                    public static void TryRun() { }
                }
            }
            """;

        await AnalyzerVerifier<TryPrefixedMethodReturnTypeAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task MethodOnUnrelatedTryClass_StillReports()
    {
        // A class named 'Try' in some OTHER namespace is NOT the framework helper;
        // the rule fires.
        var source =
            """
            namespace MyApp.Helpers
            {
                public static class Try
                {
                    public static bool {|#0:TrySomething|}() => true;
                }
            }
            """;

        var expected = AnalyzerVerifier<TryPrefixedMethodReturnTypeAnalyzer>
            .Diagnostic(DiagnosticDescriptors.TryPrefixedMethodMustReturnTryResultType)
            .WithLocation(0)
            .WithArguments("TrySomething", "bool");

        await AnalyzerVerifier<TryPrefixedMethodReturnTypeAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task Override_BaseDeclarationFlaggedOnce_DerivedSkipped()
    {
        var source =
            """
            namespace App
            {
                public abstract class Base
                {
                    public abstract bool {|#0:TryDo|}();
                }

                public sealed class Derived : Base
                {
                    public override bool TryDo() => true;
                }
            }
            """;

        var expected = AnalyzerVerifier<TryPrefixedMethodReturnTypeAnalyzer>
            .Diagnostic(DiagnosticDescriptors.TryPrefixedMethodMustReturnTryResultType)
            .WithLocation(0)
            .WithArguments("TryDo", "bool");

        await AnalyzerVerifier<TryPrefixedMethodReturnTypeAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task ImplicitInterfaceImplementation_InterfaceFlaggedOnce_ImplSkipped()
    {
        var source =
            """
            namespace App
            {
                public interface IFoo
                {
                    bool {|#0:TryDo|}();
                }

                public sealed class FooImpl : IFoo
                {
                    public bool TryDo() => true;
                }
            }
            """;

        var expected = AnalyzerVerifier<TryPrefixedMethodReturnTypeAnalyzer>
            .Diagnostic(DiagnosticDescriptors.TryPrefixedMethodMustReturnTryResultType)
            .WithLocation(0)
            .WithArguments("TryDo", "bool");

        await AnalyzerVerifier<TryPrefixedMethodReturnTypeAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task ExplicitInterfaceImplementation_InterfaceFlaggedOnce_ImplSkipped()
    {
        var source =
            """
            namespace App
            {
                public interface IFoo
                {
                    bool {|#0:TryDo|}();
                }

                public sealed class FooImpl : IFoo
                {
                    bool IFoo.TryDo() => true;
                }
            }
            """;

        var expected = AnalyzerVerifier<TryPrefixedMethodReturnTypeAnalyzer>
            .Diagnostic(DiagnosticDescriptors.TryPrefixedMethodMustReturnTryResultType)
            .WithLocation(0)
            .WithArguments("TryDo", "bool");

        await AnalyzerVerifier<TryPrefixedMethodReturnTypeAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task TriedExFromDifferentNamespace_DoesNotSatisfy()
    {
        // A type also named 'TriedEx' but living in a non-NexusLabs.Framework
        // namespace does NOT satisfy the rule; the method gets flagged.
        var source =
            """
            namespace MyOtherFramework
            {
                public readonly struct TriedEx<T>
                {
                    public T Value { get; init; }
                }
            }

            namespace App
            {
                using MyOtherFramework;

                public sealed class C
                {
                    public TriedEx<int> {|#0:TryGet|}() => default;
                }
            }
            """;

        var expected = AnalyzerVerifier<TryPrefixedMethodReturnTypeAnalyzer>
            .Diagnostic(DiagnosticDescriptors.TryPrefixedMethodMustReturnTryResultType)
            .WithLocation(0)
            .WithArguments("TryGet", "MyOtherFramework.TriedEx<int>");

        await AnalyzerVerifier<TryPrefixedMethodReturnTypeAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }
}
