using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;

using NexusLabs.Framework.Analyzers;

using Xunit;

namespace NexusLabs.Framework.Analyzers.Tests;

public sealed class CancellationTokenDefaultAnalyzerTests
{
    [Fact]
    public async Task OptionalDefault_Reports()
    {
        var source =
            """
            using System.Threading;
            using System.Threading.Tasks;

            namespace App
            {
                public sealed class C
                {
                    public Task DoAsync(CancellationToken cancellationToken {|#0:= default|})
                        => Task.CompletedTask;
                }
            }
            """;

        var expected = AnalyzerVerifier<CancellationTokenDefaultAnalyzer>
            .Diagnostic(DiagnosticDescriptors.CancellationTokenMustNotHaveDefaultValue)
            .WithLocation(0)
            .WithArguments("cancellationToken", "C", "DoAsync", "default");

        await AnalyzerVerifier<CancellationTokenDefaultAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task OptionalDefaultOfCancellationToken_Reports()
    {
        var source =
            """
            using System.Threading;
            using System.Threading.Tasks;

            namespace App
            {
                public sealed class C
                {
                    public Task DoAsync(CancellationToken cancellationToken {|#0:= default(CancellationToken)|})
                        => Task.CompletedTask;
                }
            }
            """;

        var expected = AnalyzerVerifier<CancellationTokenDefaultAnalyzer>
            .Diagnostic(DiagnosticDescriptors.CancellationTokenMustNotHaveDefaultValue)
            .WithLocation(0)
            .WithArguments("cancellationToken", "C", "DoAsync", "default(CancellationToken)");

        await AnalyzerVerifier<CancellationTokenDefaultAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task CancellationTokenNoneDefault_ReportsBothNlf0018AndCs1736()
    {
        // `= CancellationToken.None` is not a valid parameter default (CS1736:
        // default parameter value must be a compile-time constant), so the test
        // expects both the analyzer warning AND the compiler error. The analyzer
        // still correctly identifies the syntactic form even though such code
        // would never compile in production.
        var source =
            """
            using System.Threading;
            using System.Threading.Tasks;

            namespace App
            {
                public sealed class C
                {
                    public Task DoAsync(CancellationToken cancellationToken {|#0:= {|#1:CancellationToken.None|}|})
                        => Task.CompletedTask;
                }
            }
            """;

        var nlf = AnalyzerVerifier<CancellationTokenDefaultAnalyzer>
            .Diagnostic(DiagnosticDescriptors.CancellationTokenMustNotHaveDefaultValue)
            .WithLocation(0)
            .WithArguments("cancellationToken", "C", "DoAsync", "CancellationToken.None");

        var cs1736 = DiagnosticResult.CompilerError("CS1736")
            .WithLocation(1)
            .WithArguments("cancellationToken");

        await AnalyzerVerifier<CancellationTokenDefaultAnalyzer>.VerifyAnalyzerAsync(source, nlf, cs1736);
    }

    [Fact]
    public async Task NewCancellationTokenDefault_Reports()
    {
        var source =
            """
            using System.Threading;
            using System.Threading.Tasks;

            namespace App
            {
                public sealed class C
                {
                    public Task DoAsync(CancellationToken cancellationToken {|#0:= new CancellationToken()|})
                        => Task.CompletedTask;
                }
            }
            """;

        var expected = AnalyzerVerifier<CancellationTokenDefaultAnalyzer>
            .Diagnostic(DiagnosticDescriptors.CancellationTokenMustNotHaveDefaultValue)
            .WithLocation(0)
            .WithArguments("cancellationToken", "C", "DoAsync", "new CancellationToken()");

        await AnalyzerVerifier<CancellationTokenDefaultAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task RequiredCancellationToken_NoDiagnostic()
    {
        var source =
            """
            using System.Threading;
            using System.Threading.Tasks;

            namespace App
            {
                public sealed class C
                {
                    public Task DoAsync(CancellationToken cancellationToken)
                        => Task.CompletedTask;
                }
            }
            """;

        await AnalyzerVerifier<CancellationTokenDefaultAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task InterfaceDeclaration_OptionalDefault_Reports()
    {
        var source =
            """
            using System.Threading;
            using System.Threading.Tasks;

            namespace App
            {
                public interface IFoo
                {
                    Task DoAsync(CancellationToken cancellationToken {|#0:= default|});
                }
            }
            """;

        var expected = AnalyzerVerifier<CancellationTokenDefaultAnalyzer>
            .Diagnostic(DiagnosticDescriptors.CancellationTokenMustNotHaveDefaultValue)
            .WithLocation(0)
            .WithArguments("cancellationToken", "IFoo", "DoAsync", "default");

        await AnalyzerVerifier<CancellationTokenDefaultAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task AbstractMethod_OptionalDefault_Reports()
    {
        var source =
            """
            using System.Threading;
            using System.Threading.Tasks;

            namespace App
            {
                public abstract class C
                {
                    public abstract Task DoAsync(CancellationToken cancellationToken {|#0:= default|});
                }
            }
            """;

        var expected = AnalyzerVerifier<CancellationTokenDefaultAnalyzer>
            .Diagnostic(DiagnosticDescriptors.CancellationTokenMustNotHaveDefaultValue)
            .WithLocation(0)
            .WithArguments("cancellationToken", "C", "DoAsync", "default");

        await AnalyzerVerifier<CancellationTokenDefaultAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task Constructor_OptionalDefault_Reports()
    {
        var source =
            """
            using System.Threading;

            namespace App
            {
                public sealed class C
                {
                    public C(CancellationToken cancellationToken {|#0:= default|})
                    {
                    }
                }
            }
            """;

        var expected = AnalyzerVerifier<CancellationTokenDefaultAnalyzer>
            .Diagnostic(DiagnosticDescriptors.CancellationTokenMustNotHaveDefaultValue)
            .WithLocation(0)
            .WithArguments("cancellationToken", "C", ".ctor", "default");

        await AnalyzerVerifier<CancellationTokenDefaultAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task StaticMethod_OptionalDefault_Reports()
    {
        var source =
            """
            using System.Threading;
            using System.Threading.Tasks;

            namespace App
            {
                public static class C
                {
                    public static Task DoAsync(CancellationToken cancellationToken {|#0:= default|})
                        => Task.CompletedTask;
                }
            }
            """;

        var expected = AnalyzerVerifier<CancellationTokenDefaultAnalyzer>
            .Diagnostic(DiagnosticDescriptors.CancellationTokenMustNotHaveDefaultValue)
            .WithLocation(0)
            .WithArguments("cancellationToken", "C", "DoAsync", "default");

        await AnalyzerVerifier<CancellationTokenDefaultAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task ExtensionMethod_OptionalDefault_Reports()
    {
        var source =
            """
            using System.Threading;
            using System.Threading.Tasks;

            namespace App
            {
                public static class CExtensions
                {
                    public static Task DoAsync(this string s, CancellationToken cancellationToken {|#0:= default|})
                        => Task.CompletedTask;
                }
            }
            """;

        var expected = AnalyzerVerifier<CancellationTokenDefaultAnalyzer>
            .Diagnostic(DiagnosticDescriptors.CancellationTokenMustNotHaveDefaultValue)
            .WithLocation(0)
            .WithArguments("cancellationToken", "CExtensions", "DoAsync", "default");

        await AnalyzerVerifier<CancellationTokenDefaultAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task EnumeratorCancellationIterator_OptionalDefault_Reports()
    {
        // The rule fires even on [EnumeratorCancellation] — suppression is the
        // documented escape path because the BCL convention requires the default.
        var source =
            """
            using System.Collections.Generic;
            using System.Runtime.CompilerServices;
            using System.Threading;
            using System.Threading.Tasks;

            namespace App
            {
                public static class C
                {
                    public static async IAsyncEnumerable<int> IterAsync(
                        [EnumeratorCancellation] CancellationToken cancellationToken {|#0:= default|})
                    {
                        yield return 1;
                        await Task.CompletedTask;
                    }
                }
            }
            """;

        var expected = AnalyzerVerifier<CancellationTokenDefaultAnalyzer>
            .Diagnostic(DiagnosticDescriptors.CancellationTokenMustNotHaveDefaultValue)
            .WithLocation(0)
            .WithArguments("cancellationToken", "C", "IterAsync", "default");

        await AnalyzerVerifier<CancellationTokenDefaultAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task DelegateSignature_OptionalDefault_Reports()
    {
        // For a delegate type, the parameter's containing symbol is the
        // synthesized Invoke method, not the delegate type itself — so the
        // member-name slot in the diagnostic reads 'Invoke'.
        var source =
            """
            using System.Threading;
            using System.Threading.Tasks;

            namespace App
            {
                public delegate Task FooDelegate(CancellationToken cancellationToken {|#0:= default|});
            }
            """;

        var expected = AnalyzerVerifier<CancellationTokenDefaultAnalyzer>
            .Diagnostic(DiagnosticDescriptors.CancellationTokenMustNotHaveDefaultValue)
            .WithLocation(0)
            .WithArguments("cancellationToken", "FooDelegate", "Invoke", "default");

        await AnalyzerVerifier<CancellationTokenDefaultAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task NonCancellationTokenParameterWithDefault_NoDiagnostic()
    {
        var source =
            """
            namespace App
            {
                public sealed class C
                {
                    public void Do(int x = 0, string s = "", bool flag = true)
                    {
                    }
                }
            }
            """;

        await AnalyzerVerifier<CancellationTokenDefaultAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task SameNamedTypeInDifferentNamespace_NoDiagnostic()
    {
        // A user-defined type also called CancellationToken in a non-
        // System.Threading namespace must NOT trigger the rule.
        var source =
            """
            namespace MyOwn
            {
                public readonly struct CancellationToken { }
            }

            namespace App
            {
                using MyOwn;

                public sealed class C
                {
                    public void Do(CancellationToken token = default) { }
                }
            }
            """;

        await AnalyzerVerifier<CancellationTokenDefaultAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task PragmaWarningDisable_Suppresses()
    {
        var source =
            """
            using System.Threading;
            using System.Threading.Tasks;

            namespace App
            {
                public sealed class C
                {
                    // Ergonomic-default companion overload; intentional.
            #pragma warning disable NLF0018
                    public Task DoAsync(CancellationToken cancellationToken = default)
                        => Task.CompletedTask;
            #pragma warning restore NLF0018
                }
            }
            """;

        await AnalyzerVerifier<CancellationTokenDefaultAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task MultipleParametersWithCancellationTokenDefault_AllReported()
    {
        // Both parameters use `= default` since `= CancellationToken.None` is
        // not a valid parameter default (not a compile-time constant).
        var source =
            """
            using System.Threading;
            using System.Threading.Tasks;

            namespace App
            {
                public sealed class C
                {
                    public Task DoAsync(
                        CancellationToken outer {|#0:= default|},
                        CancellationToken inner {|#1:= default|})
                        => Task.CompletedTask;
                }
            }
            """;

        var expectedOuter = AnalyzerVerifier<CancellationTokenDefaultAnalyzer>
            .Diagnostic(DiagnosticDescriptors.CancellationTokenMustNotHaveDefaultValue)
            .WithLocation(0)
            .WithArguments("outer", "C", "DoAsync", "default");

        var expectedInner = AnalyzerVerifier<CancellationTokenDefaultAnalyzer>
            .Diagnostic(DiagnosticDescriptors.CancellationTokenMustNotHaveDefaultValue)
            .WithLocation(1)
            .WithArguments("inner", "C", "DoAsync", "default");

        await AnalyzerVerifier<CancellationTokenDefaultAnalyzer>.VerifyAnalyzerAsync(source, expectedOuter, expectedInner);
    }
}
