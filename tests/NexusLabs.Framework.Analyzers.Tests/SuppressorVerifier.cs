using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;


using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

// These fake DiagnosticAnalyzer subclasses exist only as fixtures for the
// suppressor tests in this assembly; they are never published or loaded by
// the compiler outside the in-process test host. The standard "real-world"
// analyzer rules (assembly shape, TFM, release tracking, EnforceExtendedAnalyzerRules)
// therefore don't apply.
#pragma warning disable RS1036 // EnforceExtendedAnalyzerRules
#pragma warning disable RS1038 // Don't reference Microsoft.CodeAnalysis.Workspaces
#pragma warning disable RS1041 // Target framework should be netstandard2.0
#pragma warning disable RS2008 // Enable analyzer release tracking

namespace NexusLabs.Framework.Analyzers.Tests;

/// <summary>
/// Test harness for <see cref="DiagnosticSuppressor"/> implementations. We
/// bypass <c>Microsoft.CodeAnalysis.Testing.CSharpAnalyzerTest</c> here
/// because its <c>ProgrammaticSuppressionInfoWrapper</c> lightup helper
/// throws on Roslyn 4.10+ (the property type changed from
/// <c>ImmutableHashSet&lt;(string,LocalizableString)&gt;</c> to
/// <c>ImmutableArray&lt;Suppression&gt;</c> upstream of the testing package).
/// See https://github.com/dotnet/roslyn-sdk/issues/1175. Instead we compose a
/// <see cref="CSharpCompilation"/> directly, attach the suppressor alongside
/// a fake producing analyzer (see <see cref="FakeIDisp007Analyzer"/>), and
/// inspect <see cref="Diagnostic.IsSuppressed"/> on the results.
/// </summary>
internal static class SuppressorHarness
{
    private const string TransfersOwnershipAttributeStub =
        """

        namespace NexusLabs.Framework
        {
            [global::System.AttributeUsage(
                global::System.AttributeTargets.Field |
                global::System.AttributeTargets.Property |
                global::System.AttributeTargets.Parameter,
                AllowMultiple = false,
                Inherited = false)]
            public sealed class TransfersOwnershipAttribute : global::System.Attribute
            {
            }
        }

        """;

    private static readonly ImmutableArray<MetadataReference> _references = LoadHostReferences();

    public static async Task<ImmutableArray<Diagnostic>> AnalyzeAsync(
        string source,
        params DiagnosticAnalyzer[] analyzers)
    {
        var sourceTree = CSharpSyntaxTree.ParseText(source);
        var stubTree = CSharpSyntaxTree.ParseText(TransfersOwnershipAttributeStub);

        var compilation = CSharpCompilation.Create(
            assemblyName: "SuppressorHarnessTest",
            syntaxTrees: new[] { sourceTree, stubTree },
            references: _references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var compileDiagnostics = compilation.GetDiagnostics();
        if (compileDiagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
        {
            var formatted = string.Join(
                Environment.NewLine,
                compileDiagnostics
                    .Where(d => d.Severity == DiagnosticSeverity.Error)
                    .Select(d => d.ToString()));
            throw new InvalidOperationException(
                $"Test source failed to compile:{Environment.NewLine}{formatted}");
        }

        // Without `reportSuppressedDiagnostics: true`, programmatically-suppressed
        // diagnostics are stripped from the result list (we lose the ability to
        // assert IsSuppressed == true). That default makes sense for production
        // build pipelines but defeats every suppressor test.
        var analyzerOptions = new CompilationWithAnalyzersOptions(
            options: null!,
            onAnalyzerException: null!,
            concurrentAnalysis: false,
            logAnalyzerExecutionTime: false,
            reportSuppressedDiagnostics: true);

        var withAnalyzers = compilation.WithAnalyzers(
            ImmutableArray.Create(analyzers),
            analyzerOptions);

        return await withAnalyzers.GetAllDiagnosticsAsync();
    }

    private static ImmutableArray<MetadataReference> LoadHostReferences()
    {
        var trustedAssemblies = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")
            ?? throw new InvalidOperationException(
                "TRUSTED_PLATFORM_ASSEMBLIES is not available in the host AppContext.");

        return trustedAssemblies
            .Split(Path.PathSeparator)
            .Where(static path => !string.IsNullOrEmpty(path))
            .Select(static path => (MetadataReference)MetadataReference.CreateFromFile(path))
            .ToImmutableArray();
    }
}

/// <summary>
/// Synthetic IDISP007 producer used in suppressor tests. Emits an IDISP007
/// diagnostic at every <c>Dispose()</c> / <c>DisposeAsync()</c> invocation in
/// the source under test, regardless of whether the receiver is truly an
/// "injected" disposable. The suppressor under test is what decides whether
/// to suppress the diagnostic — this fake exists only to give it something to
/// suppress.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class FakeIDisp007Analyzer : DiagnosticAnalyzer
{
    public static readonly DiagnosticDescriptor Rule = new(
        id: "IDISP007",
        title: "Don't dispose injected (fake)",
        messageFormat: "Don't dispose injected (fake)",
        category: "Correctness",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.InvocationExpression);
    }

    private static void Analyze(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        if (invocation.Expression is MemberAccessExpressionSyntax member &&
            (member.Name.Identifier.ValueText is "Dispose" or "DisposeAsync"))
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation()));
        }
    }
}

/// <summary>
/// Synthetic IDISP007 producer that mirrors the diagnostic-location quirk
/// of the real <c>IDisposableAnalyzers</c> IDISP007 when the dispose call
/// is wrapped in an <c>await</c> (with optional <c>.ConfigureAwait(...)</c>
/// chain): the diagnostic is anchored at the <c>await</c> keyword's
/// expression, NOT at the inner <c>DisposeAsync()</c> invocation. The
/// suppressor under test must therefore be able to find the dispose
/// target as a DESCENDANT of the reported location, not just an ancestor.
/// </summary>
#pragma warning disable RS1019 // Diagnostic Id 'IDISP007' is reused intentionally to mirror the production analyzer; both fakes coexist only as test fixtures.
[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class FakeIDisp007AwaitAnalyzer : DiagnosticAnalyzer
{
    public static readonly DiagnosticDescriptor Rule = new(
        id: "IDISP007",
        title: "Don't dispose injected (fake await)",
        messageFormat: "Don't dispose injected (fake await)",
        category: "Correctness",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.AwaitExpression);
    }

    private static void Analyze(SyntaxNodeAnalysisContext context)
    {
        var awaitExpression = (AwaitExpressionSyntax)context.Node;
        var disposeInvocation = awaitExpression
            .DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .FirstOrDefault(invocation =>
                invocation.Expression is MemberAccessExpressionSyntax member &&
                (member.Name.Identifier.ValueText is "Dispose" or "DisposeAsync"));

        if (disposeInvocation is not null)
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, awaitExpression.GetLocation()));
        }
    }
}
#pragma warning restore RS1019

/// <summary>
/// Emits a configurable-id diagnostic on every <c>Dispose()</c> /
/// <c>DisposeAsync()</c> invocation. Used by the scope-isolation test to
/// confirm the suppressor only acts on its declared <c>SuppressedDiagnosticId</c>
/// and leaves other diagnostic ids untouched.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class FakeAnalyzerEmittingId : DiagnosticAnalyzer
{
    private readonly DiagnosticDescriptor _rule;

    public FakeAnalyzerEmittingId() : this("XYZ9999")
    {
    }

    public FakeAnalyzerEmittingId(string id)
    {
        _rule = new DiagnosticDescriptor(
            id: id,
            title: $"{id} (fake)",
            messageFormat: $"{id} (fake)",
            category: "Correctness",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);
    }

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(_rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.InvocationExpression);
    }

    private void Analyze(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        if (invocation.Expression is MemberAccessExpressionSyntax member &&
            (member.Name.Identifier.ValueText is "Dispose" or "DisposeAsync"))
        {
            context.ReportDiagnostic(Diagnostic.Create(_rule, invocation.GetLocation()));
        }
    }
}
