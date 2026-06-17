using System.Threading.Tasks;

using NexusLabs.Framework.Analyzers;

using Xunit;

namespace NexusLabs.Framework.Analyzers.Tests;

public sealed class AsyncMethodCancellationTokenAnalyzerTests
{
    [Fact]
    public async Task AsyncKeywordNoToken_Reports()
    {
        var source =
            """
            using System.Threading.Tasks;

            namespace App
            {
                public sealed class C
                {
                    public async Task {|#0:Do|}()
                    {
                        await Task.CompletedTask;
                    }
                }
            }
            """;

        var expected = AnalyzerVerifier<AsyncMethodCancellationTokenAnalyzer>
            .Diagnostic(DiagnosticDescriptors.AsyncMethodMustDeclareCancellationToken)
            .WithLocation(0)
            .WithArguments("Do");

        await AnalyzerVerifier<AsyncMethodCancellationTokenAnalyzer>.VerifyAnalyzerAsync(source, expected, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task AsyncSuffixNotAsyncKeyword_Reports()
    {
        var source =
            """
            using System.Threading.Tasks;

            namespace App
            {
                public sealed class C
                {
                    public Task {|#0:DoAsync|}() => Task.CompletedTask;
                }
            }
            """;

        var expected = AnalyzerVerifier<AsyncMethodCancellationTokenAnalyzer>
            .Diagnostic(DiagnosticDescriptors.AsyncMethodMustDeclareCancellationToken)
            .WithLocation(0)
            .WithArguments("DoAsync");

        await AnalyzerVerifier<AsyncMethodCancellationTokenAnalyzer>.VerifyAnalyzerAsync(source, expected, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task AsyncVoidNotEventHandler_Reports()
    {
        var source =
            """
            using System.Threading.Tasks;

            namespace App
            {
                public sealed class C
                {
                    public async void {|#0:DoAsync|}()
                    {
                        await Task.CompletedTask;
                    }
                }
            }
            """;

        var expected = AnalyzerVerifier<AsyncMethodCancellationTokenAnalyzer>
            .Diagnostic(DiagnosticDescriptors.AsyncMethodMustDeclareCancellationToken)
            .WithLocation(0)
            .WithArguments("DoAsync");

        await AnalyzerVerifier<AsyncMethodCancellationTokenAnalyzer>.VerifyAnalyzerAsync(source, expected, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task HasCancellationToken_NoDiagnostic()
    {
        var source =
            """
            using System.Threading;
            using System.Threading.Tasks;

            namespace App
            {
                public sealed class C
                {
                    public async Task DoAsync(CancellationToken cancellationToken)
                    {
                        await Task.CompletedTask;
                    }
                }
            }
            """;

        await AnalyzerVerifier<AsyncMethodCancellationTokenAnalyzer>.VerifyAnalyzerAsync(source, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task CancellationTokenPresentButNotLast_NoDiagnostic()
    {
        // NLF0020 enforces PRESENCE only; CA1068 owns position. A token anywhere
        // in the signature satisfies this rule.
        var source =
            """
            using System.Threading;
            using System.Threading.Tasks;

            namespace App
            {
                public sealed class C
                {
                    public Task DoAsync(CancellationToken cancellationToken, int value)
                        => Task.CompletedTask;
                }
            }
            """;

        await AnalyzerVerifier<AsyncMethodCancellationTokenAnalyzer>.VerifyAnalyzerAsync(source, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Override_OnlyBaseReports()
    {
        var source =
            """
            using System.Threading.Tasks;

            namespace App
            {
                public class Base
                {
                    public virtual Task {|#0:RunAsync|}() => Task.CompletedTask;
                }

                public sealed class Derived : Base
                {
                    public override Task RunAsync() => Task.CompletedTask;
                }
            }
            """;

        var expected = AnalyzerVerifier<AsyncMethodCancellationTokenAnalyzer>
            .Diagnostic(DiagnosticDescriptors.AsyncMethodMustDeclareCancellationToken)
            .WithLocation(0)
            .WithArguments("RunAsync");

        await AnalyzerVerifier<AsyncMethodCancellationTokenAnalyzer>.VerifyAnalyzerAsync(source, expected, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task InterfaceImplementation_OnlyInterfaceReports()
    {
        var source =
            """
            using System.Threading.Tasks;

            namespace App
            {
                public interface IRunner
                {
                    Task {|#0:RunAsync|}();
                }

                public sealed class Runner : IRunner
                {
                    public Task RunAsync() => Task.CompletedTask;
                }
            }
            """;

        var expected = AnalyzerVerifier<AsyncMethodCancellationTokenAnalyzer>
            .Diagnostic(DiagnosticDescriptors.AsyncMethodMustDeclareCancellationToken)
            .WithLocation(0)
            .WithArguments("RunAsync");

        await AnalyzerVerifier<AsyncMethodCancellationTokenAnalyzer>.VerifyAnalyzerAsync(source, expected, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task TestMethod_NoDiagnostic()
    {
        var source =
            """
            using System.Threading.Tasks;

            namespace Xunit
            {
                public sealed class FactAttribute : System.Attribute { }
            }

            namespace App
            {
                public sealed class C
                {
                    [Xunit.Fact]
                    public async Task DoAsync()
                    {
                        await Task.CompletedTask;
                    }
                }
            }
            """;

        await AnalyzerVerifier<AsyncMethodCancellationTokenAnalyzer>.VerifyAnalyzerAsync(source, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task EventHandler_NoDiagnostic()
    {
        var source =
            """
            using System;
            using System.Threading.Tasks;

            namespace App
            {
                public sealed class C
                {
                    public async void OnClickAsync(object sender, EventArgs e)
                    {
                        await Task.CompletedTask;
                    }
                }
            }
            """;

        await AnalyzerVerifier<AsyncMethodCancellationTokenAnalyzer>.VerifyAnalyzerAsync(source, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Main_NoDiagnostic()
    {
        var source =
            """
            using System.Threading.Tasks;

            namespace App
            {
                public static class Program
                {
                    public static async Task Main(string[] args)
                    {
                        await Task.CompletedTask;
                    }
                }
            }
            """;

        await AnalyzerVerifier<AsyncMethodCancellationTokenAnalyzer>.VerifyAnalyzerAsync(source, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task NonAsyncNonSuffix_NoDiagnostic()
    {
        var source =
            """
            using System.Threading.Tasks;

            namespace App
            {
                public sealed class C
                {
                    public Task Do() => Task.CompletedTask;
                }
            }
            """;

        await AnalyzerVerifier<AsyncMethodCancellationTokenAnalyzer>.VerifyAnalyzerAsync(source, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task SiblingOverloadWithCancellationToken_NoDiagnostic()
    {
        var source =
            """
            using System.Threading;
            using System.Threading.Tasks;

            namespace App
            {
                public sealed class C
                {
                    public Task DoAsync() => DoAsync(CancellationToken.None);
                    public Task DoAsync(CancellationToken cancellationToken) => Task.CompletedTask;
                }
            }
            """;

        await AnalyzerVerifier<AsyncMethodCancellationTokenAnalyzer>.VerifyAnalyzerAsync(source, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task DelegateParameter_NoDiagnostic()
    {
        var source =
            """
            using System;
            using System.Threading.Tasks;

            namespace App
            {
                public sealed class C
                {
                    public async Task WrapAsync(Func<Task> action)
                    {
                        await action();
                    }

                    public async Task ApplyAsync(Action callback)
                    {
                        await Task.Yield();
                        callback();
                    }
                }
            }
            """;

        await AnalyzerVerifier<AsyncMethodCancellationTokenAnalyzer>.VerifyAnalyzerAsync(source, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task MulticastDelegateParameter_NoDiagnostic()
    {
        var source =
            """
            using System;
            using System.Threading.Tasks;

            namespace App
            {
                public sealed class C
                {
                    public async Task InvokeAsync(MulticastDelegate callback)
                    {
                        await Task.Yield();
                        callback.DynamicInvoke();
                    }
                }
            }
            """;

        await AnalyzerVerifier<AsyncMethodCancellationTokenAnalyzer>.VerifyAnalyzerAsync(source, TestContext.Current.CancellationToken);
    }
}
