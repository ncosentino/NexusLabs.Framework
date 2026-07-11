using System.Threading.Tasks;

using NexusLabs.Framework.Analyzers;

using Xunit;

namespace NexusLabs.Framework.Analyzers.Tests;

public sealed class RentedHandleCopyAnalyzerTests
{
    private static Task Reports(string body) =>
        Run(body, Expected());

    private static Task DoesNotReport(string body) =>
        Run(body, System.Array.Empty<Microsoft.CodeAnalysis.Testing.DiagnosticResult>());

    private static Task Run(string body, params Microsoft.CodeAnalysis.Testing.DiagnosticResult[] expected)
    {
        var source =
            "using System;\n" +
            "using NexusLabs.Framework.Buffers;\n" +
            "namespace App { public class C {\n" +
            body +
            "\n} }" + TestSources.RentedHandleStubs;
        return AnalyzerVerifier<RentedHandleCopyAnalyzer>.VerifyAnalyzerAsync(
            source, expected, TestContext.Current.CancellationToken);
    }

    private static Microsoft.CodeAnalysis.Testing.DiagnosticResult Expected(string name = "a") =>
        AnalyzerVerifier<RentedHandleCopyAnalyzer>
            .Diagnostic(DiagnosticDescriptors.DoNotCopyRentedHandle)
            .WithLocation(0)
            .WithArguments(name, "NexusLabs.Framework.Buffers.RentedSpan<byte>");

    [Fact]
    public Task VariableInitializerCopy_Reports() => Reports(
        "public void M() { RentedSpan<byte> a = default; var b = {|#0:a|}; b.Dispose(); }");

    [Fact]
    public Task UsingDeclarationCopy_Reports() => Reports(
        "public void M() { RentedSpan<byte> a = default; using var b = {|#0:a|}; }");

    [Fact]
    public Task PlainAssignmentCopy_Reports() => Reports(
        "public void M() { RentedSpan<byte> a = default; RentedSpan<byte> b = default; b = {|#0:a|}; b.Dispose(); }");

    [Fact]
    public Task ByValueArgumentCopy_Reports() => Reports(
        "public void Consume(RentedSpan<byte> x) { } public void M() { RentedSpan<byte> a = default; Consume({|#0:a|}); }");

    [Fact]
    public Task TernaryBranchCopy_Reports() => Reports(
        "public void M(bool cond) { RentedSpan<byte> a = default; var b = cond ? {|#0:a|} : default; b.Dispose(); }");

    [Fact]
    public Task SwitchExpressionArmCopy_Reports() => Reports(
        "public void M(int k) { RentedSpan<byte> a = default; var b = k switch { 0 => {|#0:a|}, _ => default }; b.Dispose(); }");

    [Fact]
    public Task ReturnOfUsingBoundHandle_Reports() => Reports(
        "public RentedSpan<byte> Rent() => default; public RentedSpan<byte> M() { using var a = Rent(); return {|#0:a|}; }");

    [Fact]
    public Task DefensiveCopy_NonReadonlyMemberOnInParam_Reports() => Run(
        "public void M(in RentedSpan<byte> p) { {|#0:p|}.Dispose(); }",
        Expected("p"));

    [Fact]
    public Task ParameterCopy_Reports() => Reports(
        "public void M(RentedSpan<byte> a) { var b = {|#0:a|}; b.Dispose(); }");

    [Fact]
    public Task MemberInvocationReceiver_DoesNotReport() => DoesNotReport(
        "public void M() { RentedSpan<byte> a = default; a.Dispose(); }");

    [Fact]
    public Task PropertyReceiver_DoesNotReport() => DoesNotReport(
        "public void M() { RentedSpan<byte> a = default; var n = a.Length; var s = a.Span; a.Dispose(); }");

    [Fact]
    public Task IndexerReceiver_DoesNotReport() => DoesNotReport(
        "public void M() { RentedSpan<byte> a = default; a[0] = 1; a.Dispose(); }");

    [Fact]
    public Task RefArgument_DoesNotReport() => DoesNotReport(
        "public void Consume(ref RentedSpan<byte> x) { } public void M() { RentedSpan<byte> a = default; Consume(ref a); }");

    [Fact]
    public Task InArgumentAtCallSite_DoesNotReport() => DoesNotReport(
        "public void Consume(in RentedSpan<byte> x) { } public void M() { RentedSpan<byte> a = default; Consume(in a); }");

    [Fact]
    public Task InArgument_ReadonlyMemberOnly_DoesNotReport() => DoesNotReport(
        "public void M(in RentedSpan<byte> p) { var n = p.Length; var s = p.Span; }");

    [Fact]
    public Task OutReassignment_DoesNotReport() => DoesNotReport(
        "public void Make(out RentedSpan<byte> x) { x = default; } public void M() { RentedSpan<byte> a = default; Make(out a); a.Dispose(); }");

    [Fact]
    public Task NameOf_DoesNotReport() => DoesNotReport(
        "public void M() { RentedSpan<byte> a = default; var s = nameof(a); a.Dispose(); }");

    [Fact]
    public Task DiscardAssignment_DoesNotReport() => DoesNotReport(
        "public void M() { RentedSpan<byte> a = default; _ = a; a.Dispose(); }");

    [Fact]
    public Task IsPattern_DoesNotReport() => DoesNotReport(
        "public void M() { RentedSpan<byte> a = default; if (a is { }) { } a.Dispose(); }");

    [Fact]
    public Task FreshAcquisitionBoundToUsing_DoesNotReport() => DoesNotReport(
        "public RentedSpan<byte> Rent() => default; public void M() { using var b = Rent(); }");

    [Fact]
    public Task ReturnMoveOfNonUsingLocal_DoesNotReport() => DoesNotReport(
        "public RentedSpan<byte> M() { RentedSpan<byte> a = default; return a; }");

    [Fact]
    public Task RentedMemoryCopy_DoesNotReport() => DoesNotReport(
        "public void M() { RentedMemory<byte> a = default!; var b = a; b.Dispose(); }");

    [Fact]
    public async Task SameNameDifferentNamespace_DoesNotReport()
    {
        var source =
            """
            namespace App
            {
                public ref struct RentedSpan<T> { public void Dispose() { } }
                public class C
                {
                    public void M()
                    {
                        RentedSpan<byte> a = default;
                        var b = a;
                        b.Dispose();
                    }
                }
            }
            """;

        await AnalyzerVerifier<RentedHandleCopyAnalyzer>.VerifyAnalyzerAsync(
            source, TestContext.Current.CancellationToken);
    }
}
