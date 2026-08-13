using System.Threading.Tasks;

using Microsoft.CodeAnalysis.Testing;

using NexusLabs.Framework.Analyzers;

using Xunit;

namespace NexusLabs.Framework.Analyzers.Tests;

public sealed class AsyncMethodCancellationTokenAnalyzerTests
{
    private static readonly PackageIdentity[] _xunitV3 =
    [
        new("xunit.v3.core", "3.2.2"),
    ];

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

    [Fact]
    public async Task XunitMemberDataProvider_NoDiagnostic()
    {
        var source =
            $$"""
            using System.Collections.Generic;
            using System.Threading.Tasks;

            {{XunitStubs}}

            namespace App
            {
                public sealed class C
                {
                    public static async Task<IEnumerable<object[]>> GetCases()
                    {
                        await Task.Yield();
                        return new[] { new object[] { 42 } };
                    }

                    [Xunit.Theory]
                    [Xunit.MemberData(nameof(GetCases))]
                    public void ValueIsPositive(int value)
                    {
                    }
                }
            }
            """;

        await AnalyzerVerifier<AsyncMethodCancellationTokenAnalyzer>.VerifyAnalyzerAsync(source, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task XunitMemberDataProviderNamedByStringLiteral_NoDiagnostic()
    {
        var source =
            $$"""
            using System.Collections.Generic;
            using System.Threading.Tasks;

            {{XunitStubs}}

            namespace App
            {
                public sealed class C
                {
                    public static async Task<IEnumerable<object[]>> GetCasesAsync()
                    {
                        await Task.Yield();
                        return new[] { new object[] { 42 } };
                    }

                    [Xunit.Theory]
                    [Xunit.MemberData("GetCasesAsync")]
                    public void ValueIsPositive(int value)
                    {
                    }
                }
            }
            """;

        await AnalyzerVerifier<AsyncMethodCancellationTokenAnalyzer>.VerifyAnalyzerAsync(source, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task XunitMemberDataProviderOnAnotherType_NoDiagnostic()
    {
        var source =
            $$"""
            using System.Collections.Generic;
            using System.Threading.Tasks;

            {{XunitStubs}}

            namespace App
            {
                public static class SharedCases
                {
                    public static async Task<IEnumerable<object[]>> GetCasesAsync()
                    {
                        await Task.Yield();
                        return new[] { new object[] { 42 } };
                    }
                }

                public sealed class C
                {
                    [Xunit.Theory]
                    [Xunit.MemberData(nameof(SharedCases.GetCasesAsync), MemberType = typeof(SharedCases))]
                    public void ValueIsPositive(int value)
                    {
                    }
                }
            }
            """;

        await AnalyzerVerifier<AsyncMethodCancellationTokenAnalyzer>.VerifyAnalyzerAsync(source, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task XunitMemberDataProviderOnBaseType_NoDiagnostic()
    {
        var source =
            $$"""
            using System.Collections.Generic;
            using System.Threading.Tasks;

            {{XunitStubs}}

            namespace App
            {
                public abstract class CaseSource
                {
                    public static async Task<IEnumerable<object[]>> GetCasesAsync()
                    {
                        await Task.Yield();
                        return new[] { new object[] { 42 } };
                    }
                }

                public sealed class C : CaseSource
                {
                    [Xunit.Theory]
                    [Xunit.MemberData(nameof(GetCasesAsync))]
                    public void ValueIsPositive(int value)
                    {
                    }
                }
            }
            """;

        await AnalyzerVerifier<AsyncMethodCancellationTokenAnalyzer>.VerifyAnalyzerAsync(source, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task AsyncMethodNotReferencedByMemberData_Reports()
    {
        var source =
            $$"""
            using System.Collections.Generic;
            using System.Threading.Tasks;

            {{XunitStubs}}

            namespace App
            {
                public sealed class C
                {
                    public static async Task<IEnumerable<object[]>> {|#0:GetCasesAsync|}()
                    {
                        await Task.Yield();
                        return new[] { new object[] { 42 } };
                    }
                }
            }
            """;

        var expected = AnalyzerVerifier<AsyncMethodCancellationTokenAnalyzer>
            .Diagnostic(DiagnosticDescriptors.AsyncMethodMustDeclareCancellationToken)
            .WithLocation(0)
            .WithArguments("GetCasesAsync");

        await AnalyzerVerifier<AsyncMethodCancellationTokenAnalyzer>.VerifyAnalyzerAsync(source, expected, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task SameNamedMethodOnUnreferencedType_Reports()
    {
        var source =
            $$"""
            using System.Collections.Generic;
            using System.Threading.Tasks;

            {{XunitStubs}}

            namespace App
            {
                public static class Unrelated
                {
                    public static async Task<IEnumerable<object[]>> {|#0:GetCasesAsync|}()
                    {
                        await Task.Yield();
                        return new[] { new object[] { 7 } };
                    }
                }

                public sealed class C
                {
                    public static async Task<IEnumerable<object[]>> GetCasesAsync()
                    {
                        await Task.Yield();
                        return new[] { new object[] { 42 } };
                    }

                    [Xunit.Theory]
                    [Xunit.MemberData(nameof(GetCasesAsync))]
                    public void ValueIsPositive(int value)
                    {
                    }
                }
            }
            """;

        var expected = AnalyzerVerifier<AsyncMethodCancellationTokenAnalyzer>
            .Diagnostic(DiagnosticDescriptors.AsyncMethodMustDeclareCancellationToken)
            .WithLocation(0)
            .WithArguments("GetCasesAsync");

        await AnalyzerVerifier<AsyncMethodCancellationTokenAnalyzer>.VerifyAnalyzerAsync(source, expected, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task MemberDataThatForwardsStringArgument_ReportsOnlyTheForwardedName()
    {
        var source =
            $$"""
            using System.Collections.Generic;
            using System.Threading.Tasks;

            {{XunitStubs}}

            namespace App
            {
                public sealed class C
                {
                    public static async Task<IEnumerable<object[]>> GetCasesAsync(string tag)
                    {
                        await Task.Yield();
                        return new[] { new object[] { tag } };
                    }

                    public static async Task<IEnumerable<object[]>> {|#0:LoadAsync|}()
                    {
                        await Task.Yield();
                        return new[] { new object[] { 42 } };
                    }

                    [Xunit.Theory]
                    [Xunit.MemberData(nameof(GetCasesAsync), "LoadAsync")]
                    public void ValueIsPositive(string value)
                    {
                    }
                }
            }
            """;

        var expected = AnalyzerVerifier<AsyncMethodCancellationTokenAnalyzer>
            .Diagnostic(DiagnosticDescriptors.AsyncMethodMustDeclareCancellationToken)
            .WithLocation(0)
            .WithArguments("LoadAsync");

        await AnalyzerVerifier<AsyncMethodCancellationTokenAnalyzer>.VerifyAnalyzerAsync(source, expected, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task NUnitTestCaseSourceProvider_NoDiagnostic()
    {
        var source =
            """
            using System.Collections.Generic;
            using System.Threading.Tasks;

            namespace NUnit.Framework
            {
                public sealed class TestCaseSourceAttribute : System.Attribute
                {
                    public TestCaseSourceAttribute(string sourceName) { }

                    public TestCaseSourceAttribute(System.Type sourceType, string sourceName) { }
                }
            }

            namespace App
            {
                public static class SharedCases
                {
                    public static async Task<IEnumerable<int>> GetCasesAsync()
                    {
                        await Task.Yield();
                        return new[] { 42 };
                    }
                }

                public sealed class C
                {
                    [NUnit.Framework.TestCaseSource(typeof(SharedCases), nameof(SharedCases.GetCasesAsync))]
                    public void ValueIsPositive(int value)
                    {
                    }
                }
            }
            """;

        await AnalyzerVerifier<AsyncMethodCancellationTokenAnalyzer>.VerifyAnalyzerAsync(source, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task MsTestDynamicDataProvider_NoDiagnostic()
    {
        var source =
            """
            using System.Collections.Generic;
            using System.Threading.Tasks;

            namespace Microsoft.VisualStudio.TestTools.UnitTesting
            {
                public sealed class DynamicDataAttribute : System.Attribute
                {
                    public DynamicDataAttribute(string dynamicDataSourceName) { }

                    public DynamicDataAttribute(string dynamicDataSourceName, System.Type dynamicDataDeclaringType) { }
                }
            }

            namespace App
            {
                public static class SharedCases
                {
                    public static async Task<IEnumerable<object[]>> GetCasesAsync()
                    {
                        await Task.Yield();
                        return new[] { new object[] { 42 } };
                    }
                }

                public sealed class C
                {
                    [Microsoft.VisualStudio.TestTools.UnitTesting.DynamicData(nameof(SharedCases.GetCasesAsync), typeof(SharedCases))]
                    public void ValueIsPositive(int value)
                    {
                    }
                }
            }
            """;

        await AnalyzerVerifier<AsyncMethodCancellationTokenAnalyzer>.VerifyAnalyzerAsync(source, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task TUnitMethodDataSourceProvider_NoDiagnostic()
    {
        var source =
            """
            using System.Threading.Tasks;

            namespace TUnit.Core
            {
                public sealed class MethodDataSourceAttribute : System.Attribute
                {
                    public MethodDataSourceAttribute(string methodName) { }

                    public MethodDataSourceAttribute(System.Type classType, string methodName) { }
                }
            }

            namespace App
            {
                public sealed class C
                {
                    public static async Task<int> GetCaseAsync()
                    {
                        await Task.Yield();
                        return 42;
                    }

                    [TUnit.Core.MethodDataSource(nameof(GetCaseAsync))]
                    public void ValueIsPositive(int value)
                    {
                    }
                }
            }
            """;

        await AnalyzerVerifier<AsyncMethodCancellationTokenAnalyzer>.VerifyAnalyzerAsync(source, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GenericMethodDataSourceProvider_NoDiagnostic()
    {
        var source =
            """
            using System.Threading.Tasks;

            namespace TUnit.Core
            {
                public class MethodDataSourceAttribute : System.Attribute
                {
                    public MethodDataSourceAttribute(string methodName) { }

                    public MethodDataSourceAttribute(System.Type classType, string methodName) { }
                }

                public sealed class MethodDataSourceAttribute<T> : MethodDataSourceAttribute
                {
                    public MethodDataSourceAttribute(string methodName)
                        : base(typeof(T), methodName)
                    {
                    }
                }
            }

            namespace App
            {
                public sealed class SharedCases
                {
                    public static async Task<int> GetCaseAsync()
                    {
                        await Task.Yield();
                        return 42;
                    }
                }

                public sealed class C
                {
                    [TUnit.Core.MethodDataSource<SharedCases>(nameof(SharedCases.GetCaseAsync))]
                    public void ValueIsPositive(int value)
                    {
                    }
                }
            }
            """;

        await AnalyzerVerifier<AsyncMethodCancellationTokenAnalyzer>.VerifyAnalyzerAsync(source, TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// <c>[ValueSource]</c> sits on a parameter rather than a member, which
    /// reaches the index through a different syntax path than the other
    /// attributes.
    /// </summary>
    [Fact]
    public async Task NUnitValueSourceProviderOnParameter_NoDiagnostic()
    {
        var source =
            """
            using System.Collections.Generic;
            using System.Threading.Tasks;

            namespace NUnit.Framework
            {
                [System.AttributeUsage(System.AttributeTargets.Parameter)]
                public sealed class ValueSourceAttribute : System.Attribute
                {
                    public ValueSourceAttribute(string sourceName) { }

                    public ValueSourceAttribute(System.Type sourceType, string sourceName) { }
                }
            }

            namespace App
            {
                public sealed class C
                {
                    public static async Task<IEnumerable<int>> GetValuesAsync()
                    {
                        await Task.Yield();
                        return new[] { 42 };
                    }

                    public void ValueIsPositive(
                        [NUnit.Framework.ValueSource(nameof(GetValuesAsync))] int value)
                    {
                    }
                }
            }
            """;

        await AnalyzerVerifier<AsyncMethodCancellationTokenAnalyzer>.VerifyAnalyzerAsync(source, TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// The attribute sits on the consuming test, which routinely lives in a
    /// different file from the provider. Pins that the index spans the whole
    /// compilation rather than the tree holding the reported node.
    /// </summary>
    [Fact]
    public async Task MemberDataProviderInAnotherFile_ReportsOnlyTheUnreferencedMethod()
    {
        var providerSource =
            """
            using System.Collections.Generic;
            using System.Threading.Tasks;

            namespace App
            {
                public static class SharedCases
                {
                    public static async Task<IEnumerable<object[]>> GetCasesAsync()
                    {
                        await Task.Yield();
                        return new[] { new object[] { 42 } };
                    }

                    public static async Task<IEnumerable<object[]>> {|#0:LoadCasesAsync|}()
                    {
                        await Task.Yield();
                        return new[] { new object[] { 7 } };
                    }
                }
            }
            """;

        var testSource =
            $$"""
            {{XunitStubs}}

            namespace App
            {
                public sealed class C
                {
                    [Xunit.Theory]
                    [Xunit.MemberData(nameof(SharedCases.GetCasesAsync), MemberType = typeof(SharedCases))]
                    public void ValueIsPositive(int value)
                    {
                    }
                }
            }
            """;

        var expected = AnalyzerVerifier<AsyncMethodCancellationTokenAnalyzer>
            .Diagnostic(DiagnosticDescriptors.AsyncMethodMustDeclareCancellationToken)
            .WithLocation(0)
            .WithArguments("LoadCasesAsync");

        await AnalyzerVerifier<AsyncMethodCancellationTokenAnalyzer>.VerifyAnalyzerWithSourcesAsync(
            [providerSource, testSource],
            [expected],
            TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// End-to-end check against the real xUnit v3 assemblies rather than the
    /// local attribute stubs, so a rename or namespace move in xUnit surfaces
    /// here. The exempt and reported methods share one compilation to keep the
    /// package restore to a single test.
    /// </summary>
    [Fact]
    public async Task RealXunitMemberDataProvider_ReportsOnlyTheUnreferencedMethod()
    {
        var source =
            """
            using System.Threading.Tasks;

            using Xunit;

            namespace App
            {
                public sealed class ExampleTests
                {
                    public static async Task<TheoryData<int>> GetCases()
                    {
                        await Task.Yield();
                        return new TheoryData<int> { 42 };
                    }

                    public static async Task<TheoryData<int>> {|#0:LoadCasesAsync|}()
                    {
                        await Task.Yield();
                        return new TheoryData<int> { 42 };
                    }

                    [Theory]
                    [MemberData(nameof(GetCases))]
                    public void ValueIsPositive(int value)
                    {
                    }
                }
            }
            """;

        var expected = AnalyzerVerifier<AsyncMethodCancellationTokenAnalyzer>
            .Diagnostic(DiagnosticDescriptors.AsyncMethodMustDeclareCancellationToken)
            .WithLocation(0)
            .WithArguments("LoadCasesAsync");

        await AnalyzerVerifier<AsyncMethodCancellationTokenAnalyzer>.VerifyAnalyzerWithPackagesAsync(
            source,
            _xunitV3,
            [expected],
            TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Minimal stand-ins for the xUnit attributes. The analyzer matches
    /// attributes by simple name, so the test compilation does not need a real
    /// xUnit reference.
    /// </summary>
    private const string XunitStubs =
        """
        namespace Xunit
        {
            public sealed class TheoryAttribute : System.Attribute { }

            public sealed class MemberDataAttribute : System.Attribute
            {
                public MemberDataAttribute(string memberName, params object[] parameters) { }

                public System.Type MemberType { get; set; }
            }
        }
        """;
}
