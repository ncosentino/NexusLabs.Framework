using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

using Xunit;

namespace NexusLabs.Framework.Analyzers.Tests;

public sealed class TryResultMethodNameAnalyzerTests
{
    [Theory]
    [InlineData("TriedEx<int>")]
    [InlineData("TriedNullEx<string?>")]
    [InlineData("Exception?")]
    [InlineData("Task<TriedEx<int>>")]
    [InlineData("Task<TriedNullEx<string?>>")]
    [InlineData("Task<Exception?>")]
    [InlineData("ValueTask<TriedEx<int>>")]
    [InlineData("ValueTask<TriedNullEx<string?>>")]
    [InlineData("ValueTask<Exception?>")]
    public async Task ResultReturnType_RequiresTryPrefix(string returnType)
    {
        var source =
            $$"""
            #nullable enable
            using System;
            using System.Threading.Tasks;
            using NexusLabs.Framework;

            public abstract class C
            {
                public abstract {{returnType}} {|#0:ReadAsync|}();
                public abstract {{returnType}} TryReadAsync();
            }
            """ + TestSources.TriedExStubs;

        var expected = AnalyzerVerifier<TryResultMethodNameAnalyzer>
            .Diagnostic(DiagnosticDescriptors.TryResultMethodMustHaveTryPrefix)
            .WithLocation(0)
            .WithArguments("ReadAsync");

        await AnalyzerVerifier<TryResultMethodNameAnalyzer>.VerifyAnalyzerAsync(
            source, expected, TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData("Read")]
    [InlineData("Try")]
    [InlineData("Tryread")]
    [InlineData("tryRead")]
    [InlineData("Read_Result")]
    public async Task NonConformingName_Reports(string name)
    {
        var source =
            $$"""
            using NexusLabs.Framework;
            public abstract class C
            {
                public abstract TriedEx<int> {|#0:{{name}}|}();
            }
            """ + TestSources.TriedExStubs;

        var expected = AnalyzerVerifier<TryResultMethodNameAnalyzer>
            .Diagnostic(DiagnosticDescriptors.TryResultMethodMustHaveTryPrefix)
            .WithLocation(0)
            .WithArguments(name);

        await AnalyzerVerifier<TryResultMethodNameAnalyzer>.VerifyAnalyzerAsync(
            source, expected, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task AliasedResultAndEscapedPrefix_UseSemanticNames()
    {
        const string source =
            """
            using Result = NexusLabs.Framework.TriedEx<int>;
            public abstract class C
            {
                public abstract Result {|#0:Read|}();
                public abstract Result @TryRead();
                public abstract Result \u0054ryReadEscaped();
                public abstract Result TryRead_Result();
            }
            """ + TestSources.TriedExStubs;

        var expected = AnalyzerVerifier<TryResultMethodNameAnalyzer>
            .Diagnostic(DiagnosticDescriptors.TryResultMethodMustHaveTryPrefix)
            .WithLocation(0)
            .WithArguments("Read");

        await AnalyzerVerifier<TryResultMethodNameAnalyzer>.VerifyAnalyzerAsync(
            source, expected, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Overrides_BaseDeclarationReportedOnly()
    {
        const string source =
            """
            #nullable enable
            using System;
            public abstract class Base
            {
                public abstract Exception? {|#0:Read|}();
            }
            public sealed class Derived : Base
            {
                public override Exception? Read() => null;
            }
            """;

        var expected = AnalyzerVerifier<TryResultMethodNameAnalyzer>
            .Diagnostic(DiagnosticDescriptors.TryResultMethodMustHaveTryPrefix)
            .WithLocation(0)
            .WithArguments("Read");

        await AnalyzerVerifier<TryResultMethodNameAnalyzer>.VerifyAnalyzerAsync(
            source, expected, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task OwnedInterface_DeclarationAndImplementationsReported()
    {
        const string source =
            """
            #nullable enable
            using System;
            public interface IReader
            {
                Exception? {|#0:Read|}();
            }
            public sealed class Implicit : IReader
            {
                public Exception? {|#1:Read|}() => null;
            }
            public sealed class Explicit : IReader
            {
                Exception? IReader.{|#2:Read|}() => null;
            }
            """;

        var diagnostic = AnalyzerVerifier<TryResultMethodNameAnalyzer>
            .Diagnostic(DiagnosticDescriptors.TryResultMethodMustHaveTryPrefix)
            .WithArguments("Read");

        await AnalyzerVerifier<TryResultMethodNameAnalyzer>.VerifyAnalyzerAsync(
            source,
            [diagnostic.WithLocation(0), diagnostic.WithLocation(1), diagnostic.WithLocation(2)],
            TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData("TriedEx<int>")]
    [InlineData("TriedNullEx<string?>")]
    [InlineData("System.Exception?")]
    [InlineData("System.Threading.Tasks.Task<TriedEx<int>>")]
    [InlineData("System.Threading.Tasks.Task<TriedNullEx<string?>>")]
    [InlineData("System.Threading.Tasks.Task<System.Exception?>")]
    [InlineData("System.Threading.Tasks.ValueTask<TriedEx<int>>")]
    [InlineData("System.Threading.Tasks.ValueTask<TriedNullEx<string?>>")]
    [InlineData("System.Threading.Tasks.ValueTask<System.Exception?>")]
    public async Task ExternalInterface_ImplementationsExemptButUnrelatedOverloadReported(string returnType)
    {
        var externalSource =
            $$"""
            #nullable enable
            using NexusLabs.Framework;
            namespace External
            {
                public interface IReader<T>
                {
                    {{returnType}} {|#1:Read|}(T key);
                }
            }
            """ + TestSources.TriedExStubs;
        var source =
            $$"""
            #nullable enable
            using NexusLabs.Framework;
            using External;
            public interface IOwnedReader : IReader<int> { }
            public abstract class Implicit : IOwnedReader
            {
                public abstract {{returnType}} Read(int key);
                public abstract {{returnType}} {|#0:Read|}(string key);
            }
            public sealed class Explicit : IReader<int>
            {
                {{returnType}} IReader<int>.Read(int key) => default!;
            }
            """;

        var expected = AnalyzerVerifier<TryResultMethodNameAnalyzer>
            .Diagnostic(DiagnosticDescriptors.TryResultMethodMustHaveTryPrefix)
            .WithArguments("Read");

        await AnalyzerVerifier<TryResultMethodNameAnalyzer>.VerifyAnalyzerWithAdditionalProjectAsync(
            source, "ExternalContracts", [externalSource],
            [expected.WithLocation(0), expected.WithLocation(1)], TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task LocalFunctionsLambdasAndAccessors_NoDiagnostic()
    {
        const string source =
            """
            #nullable enable
            using System;
            using System.Threading.Tasks;
            using NexusLabs.Framework;
            public sealed class C
            {
                public Exception? Error => null;
                public TriedEx<int> this[int index] => default;
                public void Run()
                {
                    TriedEx<int> Read() => default;
                    Task<TriedNullEx<string?>> ReadAsync() => Task.FromResult(default(TriedNullEx<string?>));
                    Exception? Error() => null;
                    Func<Exception?> lambda = () => null;
                    Func<TriedEx<int>> anonymous = delegate { return default; };
                    _ = Read();
                    _ = ReadAsync();
                    _ = Error();
                    _ = lambda();
                    _ = anonymous();
                }
            }
            """ + TestSources.TriedExStubs;

        await AnalyzerVerifier<TryResultMethodNameAnalyzer>.VerifyAnalyzerAsync(
            source, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task UnrelatedTypesAndNonNullableExceptions_NoDiagnostic()
    {
        const string source =
            """
            #nullable enable
            using System;
            using System.Threading.Tasks;
            public abstract class C
            {
                public abstract Exception Read();
                public abstract Task<Exception> ReadAsync();
                public abstract ValueTask<Exception> ReadValueAsync();
                public abstract InvalidOperationException? ReadDerived();
                public abstract Task<InvalidOperationException?> ReadDerivedAsync();
                public abstract Other.TriedEx<int> ReadOther();
                public abstract Task<Other.TriedNullEx<string>> ReadOtherAsync();
                public abstract ValueTask<Other.Exception?> ReadOtherValueAsync();
                public abstract Other.Task<NexusLabs.Framework.TriedEx<int>> ReadFakeTask();
                public abstract Other.ValueTask<NexusLabs.Framework.TriedNullEx<string>> ReadFakeValueTask();
                public abstract Task<Task<NexusLabs.Framework.TriedEx<int>>> ReadNestedTask();
                public abstract NexusLabs.Framework.TriedEx<int>? ReadNullableStruct();
                public abstract bool ReadBool();
                public abstract string? ReadString();
            #nullable disable
                public abstract Exception ReadOblivious();
                public abstract Task<Exception> ReadObliviousAsync();
                public abstract ValueTask<Exception> ReadObliviousValueAsync();
            }
            namespace Other
            {
                public struct TriedEx<T> { }
                public struct TriedNullEx<T> { }
                public class Exception { }
                public class Task<T> { }
                public struct ValueTask<T> { }
            }
            """ + TestSources.TriedExStubs;

        await AnalyzerVerifier<TryResultMethodNameAnalyzer>.VerifyAnalyzerAsync(
            source, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task MatchingNamespaceButWrongTypeArity_NoDiagnostic()
    {
        const string source =
            """
            public abstract class C
            {
                public abstract NexusLabs.Framework.TriedEx Read();
                public abstract NexusLabs.Framework.TriedNullEx<int, string> ReadPair();
                public abstract NexusLabs.Framework.Container.TriedEx<int> ReadNested();
            }
            namespace NexusLabs.Framework
            {
                public struct TriedEx { }
                public struct TriedNullEx<T, U> { }
                public class Container
                {
                    public struct TriedEx<T> { }
                }
            }
            """;

        await AnalyzerVerifier<TryResultMethodNameAnalyzer>.VerifyAnalyzerAsync(
            source, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Severity_CanBeConfiguredIndependently()
    {
        var test = new CSharpAnalyzerTest<TryResultMethodNameAnalyzer, DefaultVerifier>
        {
            TestCode =
                """
                #nullable enable
                public abstract class C
                {
                    public abstract System.Exception? Read();
                }
                """,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
        };
        test.SolutionTransforms.Add((solution, projectId) =>
        {
            var project = solution.GetProject(projectId)!;
            return solution.WithProjectCompilationOptions(
                projectId,
                project.CompilationOptions!.WithSpecificDiagnosticOptions(
                    project.CompilationOptions.SpecificDiagnosticOptions
                        .SetItem("NLF0029", ReportDiagnostic.Suppress)
                        .SetItem("NLF0015", ReportDiagnostic.Error)));
        });

        await test.RunAsync(TestContext.Current.CancellationToken);
    }
}
