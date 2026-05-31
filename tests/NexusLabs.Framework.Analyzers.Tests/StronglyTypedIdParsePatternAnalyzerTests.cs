using System.Threading.Tasks;

using NexusLabs.Framework.Analyzers;

using Xunit;

namespace NexusLabs.Framework.Analyzers.Tests;

public sealed class StronglyTypedIdParsePatternAnalyzerTests
{
    private const string GuidFooId =
        """

        namespace App
        {
            [StronglyTypedIds.StronglyTypedId]
            public readonly partial struct FooId
            {
                public FooId(System.Guid value) { Value = value; }
                public System.Guid Value { get; }
                public static FooId Parse(string s) => default;
                public static bool TryParse(string s, out FooId result) { result = default; return false; }
            }
        }
        """;

    private const string IntFooId =
        """

        namespace App
        {
            [StronglyTypedIds.StronglyTypedId]
            public readonly partial struct IntId
            {
                public IntId(int value) { Value = value; }
                public int Value { get; }
                public static IntId Parse(string s) => default;
                public static bool TryParse(string s, out IntId result) { result = default; return false; }
            }
        }
        """;

    private const string LongFooId =
        """

        namespace App
        {
            [StronglyTypedIds.StronglyTypedId]
            public readonly partial struct LongId
            {
                public LongId(long value) { Value = value; }
                public long Value { get; }
                public static LongId Parse(string s) => default;
                public static bool TryParse(string s, out LongId result) { result = default; return false; }
            }
        }
        """;

    [Fact]
    public async Task ParsePattern_GuidBacked_Reports()
    {
        var source =
            """
            namespace App
            {
                public class C
                {
                    public FooId M(string s) => {|#0:new FooId(System.Guid.Parse(s))|};
                }
            }
            """ + GuidFooId + TestSources.StronglyTypedIdAttributeStub;

        var expected = AnalyzerVerifier<StronglyTypedIdParsePatternAnalyzer>
            .Diagnostic(DiagnosticDescriptors.StronglyTypedIdParsePatternMisuse)
            .WithLocation(0)
            .WithArguments("FooId", "Parse", "System.Guid");

        await AnalyzerVerifier<StronglyTypedIdParsePatternAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task ParsePattern_IntBacked_Reports()
    {
        var source =
            """
            namespace App
            {
                public class C
                {
                    public IntId M(string s) => {|#0:new IntId(int.Parse(s))|};
                }
            }
            """ + IntFooId + TestSources.StronglyTypedIdAttributeStub;

        var expected = AnalyzerVerifier<StronglyTypedIdParsePatternAnalyzer>
            .Diagnostic(DiagnosticDescriptors.StronglyTypedIdParsePatternMisuse)
            .WithLocation(0)
            .WithArguments("IntId", "Parse", "int");

        await AnalyzerVerifier<StronglyTypedIdParsePatternAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task ParsePattern_LongBacked_Reports()
    {
        var source =
            """
            namespace App
            {
                public class C
                {
                    public LongId M(string s) => {|#0:new LongId(long.Parse(s))|};
                }
            }
            """ + LongFooId + TestSources.StronglyTypedIdAttributeStub;

        var expected = AnalyzerVerifier<StronglyTypedIdParsePatternAnalyzer>
            .Diagnostic(DiagnosticDescriptors.StronglyTypedIdParsePatternMisuse)
            .WithLocation(0)
            .WithArguments("LongId", "Parse", "long");

        await AnalyzerVerifier<StronglyTypedIdParsePatternAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task ParsePattern_ImplicitObjectCreation_Reports()
    {
        var source =
            """
            namespace App
            {
                public class C
                {
                    public FooId M(string s)
                    {
                        FooId id = {|#0:new(System.Guid.Parse(s))|};
                        return id;
                    }
                }
            }
            """ + GuidFooId + TestSources.StronglyTypedIdAttributeStub;

        var expected = AnalyzerVerifier<StronglyTypedIdParsePatternAnalyzer>
            .Diagnostic(DiagnosticDescriptors.StronglyTypedIdParsePatternMisuse)
            .WithLocation(0)
            .WithArguments("FooId", "Parse", "System.Guid");

        await AnalyzerVerifier<StronglyTypedIdParsePatternAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task ParsePattern_WithParenthesesAroundInnerCall_Reports()
    {
        var source =
            """
            namespace App
            {
                public class C
                {
                    public FooId M(string s) => {|#0:new FooId((System.Guid.Parse(s)))|};
                }
            }
            """ + GuidFooId + TestSources.StronglyTypedIdAttributeStub;

        var expected = AnalyzerVerifier<StronglyTypedIdParsePatternAnalyzer>
            .Diagnostic(DiagnosticDescriptors.StronglyTypedIdParsePatternMisuse)
            .WithLocation(0)
            .WithArguments("FooId", "Parse", "System.Guid");

        await AnalyzerVerifier<StronglyTypedIdParsePatternAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task TryParsePattern_GuidBacked_InsideIfThenBranch_Reports()
    {
        var source =
            """
            namespace App
            {
                public class C
                {
                    public FooId? M(string s)
                    {
                        if (System.Guid.TryParse(s, out var g))
                        {
                            return {|#0:new FooId(g)|};
                        }
                        return null;
                    }
                }
            }
            """ + GuidFooId + TestSources.StronglyTypedIdAttributeStub;

        var expected = AnalyzerVerifier<StronglyTypedIdParsePatternAnalyzer>
            .Diagnostic(DiagnosticDescriptors.StronglyTypedIdParsePatternMisuse)
            .WithLocation(0)
            .WithArguments("FooId", "TryParse", "System.Guid");

        await AnalyzerVerifier<StronglyTypedIdParsePatternAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task TryParsePattern_TypedOutDesignation_Reports()
    {
        var source =
            """
            namespace App
            {
                public class C
                {
                    public FooId? M(string s)
                    {
                        if (System.Guid.TryParse(s, out System.Guid g))
                        {
                            return {|#0:new FooId(g)|};
                        }
                        return null;
                    }
                }
            }
            """ + GuidFooId + TestSources.StronglyTypedIdAttributeStub;

        var expected = AnalyzerVerifier<StronglyTypedIdParsePatternAnalyzer>
            .Diagnostic(DiagnosticDescriptors.StronglyTypedIdParsePatternMisuse)
            .WithLocation(0)
            .WithArguments("FooId", "TryParse", "System.Guid");

        await AnalyzerVerifier<StronglyTypedIdParsePatternAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task TryParsePattern_ConditionWrappedInParentheses_Reports()
    {
        var source =
            """
            namespace App
            {
                public class C
                {
                    public FooId? M(string s)
                    {
                        if ((System.Guid.TryParse(s, out var g)))
                        {
                            return {|#0:new FooId(g)|};
                        }
                        return null;
                    }
                }
            }
            """ + GuidFooId + TestSources.StronglyTypedIdAttributeStub;

        var expected = AnalyzerVerifier<StronglyTypedIdParsePatternAnalyzer>
            .Diagnostic(DiagnosticDescriptors.StronglyTypedIdParsePatternMisuse)
            .WithLocation(0)
            .WithArguments("FooId", "TryParse", "System.Guid");

        await AnalyzerVerifier<StronglyTypedIdParsePatternAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task TryParsePattern_OtherUsesOfOutVarInBranch_StillReports()
    {
        var source =
            """
            using System;
            namespace App
            {
                public class C
                {
                    public FooId? M(string s)
                    {
                        if (System.Guid.TryParse(s, out var g))
                        {
                            Console.WriteLine(g);
                            return {|#0:new FooId(g)|};
                        }
                        return null;
                    }
                }
            }
            """ + GuidFooId + TestSources.StronglyTypedIdAttributeStub;

        var expected = AnalyzerVerifier<StronglyTypedIdParsePatternAnalyzer>
            .Diagnostic(DiagnosticDescriptors.StronglyTypedIdParsePatternMisuse)
            .WithLocation(0)
            .WithArguments("FooId", "TryParse", "System.Guid");

        await AnalyzerVerifier<StronglyTypedIdParsePatternAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task TryParsePattern_OutVarReassignedInBranch_DoesNotReport()
    {
        var source =
            """
            namespace App
            {
                public class C
                {
                    public FooId? M(string s)
                    {
                        if (System.Guid.TryParse(s, out var g))
                        {
                            g = System.Guid.Empty;
                            return new FooId(g);
                        }
                        return null;
                    }
                }
            }
            """ + GuidFooId + TestSources.StronglyTypedIdAttributeStub;

        await AnalyzerVerifier<StronglyTypedIdParsePatternAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task TryParsePattern_NegatedCondition_DoesNotReport()
    {
        var source =
            """
            namespace App
            {
                public class C
                {
                    public FooId? M(string s)
                    {
                        if (!System.Guid.TryParse(s, out var g))
                        {
                            return null;
                        }
                        return new FooId(g);
                    }
                }
            }
            """ + GuidFooId + TestSources.StronglyTypedIdAttributeStub;

        await AnalyzerVerifier<StronglyTypedIdParsePatternAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task TryParsePattern_CompoundCondition_DoesNotReport()
    {
        var source =
            """
            namespace App
            {
                public class C
                {
                    public FooId? M(string s, bool extra)
                    {
                        if (System.Guid.TryParse(s, out var g) && extra)
                        {
                            return new FooId(g);
                        }
                        return null;
                    }
                }
            }
            """ + GuidFooId + TestSources.StronglyTypedIdAttributeStub;

        await AnalyzerVerifier<StronglyTypedIdParsePatternAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task TryParsePattern_TryParseStatementOutsideIf_DoesNotReport()
    {
        var source =
            """
            namespace App
            {
                public class C
                {
                    public FooId M(string s)
                    {
                        var ok = System.Guid.TryParse(s, out var g);
                        return new FooId(g);
                    }
                }
            }
            """ + GuidFooId + TestSources.StronglyTypedIdAttributeStub;

        await AnalyzerVerifier<StronglyTypedIdParsePatternAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task TryParsePattern_ConstructionInsideNestedLambda_DoesNotReport()
    {
        var source =
            """
            using System;
            namespace App
            {
                public class C
                {
                    public void M(string s)
                    {
                        if (System.Guid.TryParse(s, out var g))
                        {
                            Action a = () =>
                            {
                                var id = new FooId(g);
                            };
                            a();
                        }
                    }
                }
            }
            """ + GuidFooId + TestSources.StronglyTypedIdAttributeStub;

        await AnalyzerVerifier<StronglyTypedIdParsePatternAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task TryParsePattern_ConstructionInsideLocalFunction_DoesNotReport()
    {
        var source =
            """
            namespace App
            {
                public class C
                {
                    public FooId? M(string s)
                    {
                        if (System.Guid.TryParse(s, out var g))
                        {
                            FooId Make() => new FooId(g);
                            return Make();
                        }
                        return null;
                    }
                }
            }
            """ + GuidFooId + TestSources.StronglyTypedIdAttributeStub;

        await AnalyzerVerifier<StronglyTypedIdParsePatternAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task TryParsePattern_ConstructionInsideElseBranch_DoesNotReport()
    {
        var source =
            """
            namespace App
            {
                public class C
                {
                    public FooId? M(string s)
                    {
                        if (System.Guid.TryParse(s, out var g))
                        {
                            return null;
                        }
                        else
                        {
                            return new FooId(g);
                        }
                    }
                }
            }
            """ + GuidFooId + TestSources.StronglyTypedIdAttributeStub;

        await AnalyzerVerifier<StronglyTypedIdParsePatternAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task TypeWithoutStronglyTypedIdAttribute_DoesNotReport()
    {
        var source =
            """
            namespace App
            {
                public readonly struct PlainId
                {
                    public PlainId(System.Guid value) { Value = value; }
                    public System.Guid Value { get; }
                    public static PlainId Parse(string s) => default;
                    public static bool TryParse(string s, out PlainId result) { result = default; return false; }
                }

                public class C
                {
                    public PlainId M(string s) => new PlainId(System.Guid.Parse(s));
                }
            }
            """ + TestSources.StronglyTypedIdAttributeStub;

        await AnalyzerVerifier<StronglyTypedIdParsePatternAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task ParseExactInsteadOfParse_DoesNotReport()
    {
        var source =
            """
            namespace App
            {
                public class C
                {
                    public FooId M(string s) => new FooId(System.Guid.ParseExact(s, "N"));
                }
            }
            """ + GuidFooId + TestSources.StronglyTypedIdAttributeStub;

        await AnalyzerVerifier<StronglyTypedIdParsePatternAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task ParseWithExtraOverloadArgs_DoesNotReport()
    {
        var source =
            """
            namespace App
            {
                public class C
                {
                    public IntId M(string s) => new IntId(int.Parse(s, System.Globalization.NumberStyles.Integer));
                }
            }
            """ + IntFooId + TestSources.StronglyTypedIdAttributeStub;

        await AnalyzerVerifier<StronglyTypedIdParsePatternAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task NewGuidConstructor_DoesNotReport()
    {
        var source =
            """
            namespace App
            {
                public class C
                {
                    public FooId M() => new FooId(System.Guid.NewGuid());
                }
            }
            """ + GuidFooId + TestSources.StronglyTypedIdAttributeStub;

        await AnalyzerVerifier<StronglyTypedIdParsePatternAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task IdTypeWithoutOwnParseMethod_DoesNotReport()
    {
        var source =
            """
            namespace App
            {
                [StronglyTypedIds.StronglyTypedId]
                public readonly partial struct NoParserId
                {
                    public NoParserId(System.Guid value) { Value = value; }
                    public System.Guid Value { get; }
                }

                public class C
                {
                    public NoParserId M(string s) => new NoParserId(System.Guid.Parse(s));
                }
            }
            """ + TestSources.StronglyTypedIdAttributeStub;

        await AnalyzerVerifier<StronglyTypedIdParsePatternAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task IdTypeWithoutOwnTryParseMethod_DoesNotReport()
    {
        var source =
            """
            namespace App
            {
                [StronglyTypedIds.StronglyTypedId]
                public readonly partial struct NoTryParserId
                {
                    public NoTryParserId(System.Guid value) { Value = value; }
                    public System.Guid Value { get; }
                    public static NoTryParserId Parse(string s) => default;
                }

                public class C
                {
                    public NoTryParserId? M(string s)
                    {
                        if (System.Guid.TryParse(s, out var g))
                        {
                            return new NoTryParserId(g);
                        }
                        return null;
                    }
                }
            }
            """ + TestSources.StronglyTypedIdAttributeStub;

        await AnalyzerVerifier<StronglyTypedIdParsePatternAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task StringBackedId_PassingStringLiteral_DoesNotReport()
    {
        var source =
            """
            namespace App
            {
                [StronglyTypedIds.StronglyTypedId]
                public readonly partial struct StringId
                {
                    public StringId(string value) { Value = value; }
                    public string Value { get; }
                }

                public class C
                {
                    public StringId M(string s) => new StringId(s);
                }
            }
            """ + TestSources.StronglyTypedIdAttributeStub;

        await AnalyzerVerifier<StronglyTypedIdParsePatternAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task UserDefinedTryParseOnUnrelatedHelper_DoesNotReport()
    {
        var source =
            """
            namespace App
            {
                public static class Helper
                {
                    public static bool TryParse(string s, out System.Guid g)
                    {
                        g = default;
                        return false;
                    }
                }

                public class C
                {
                    public FooId? M(string s)
                    {
                        if (Helper.TryParse(s, out var g))
                        {
                            return new FooId(g);
                        }
                        return null;
                    }
                }
            }
            """ + GuidFooId + TestSources.StronglyTypedIdAttributeStub;

        await AnalyzerVerifier<StronglyTypedIdParsePatternAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task AttributeNotReferenced_AnalyzerSilent()
    {
        var source =
            """
            namespace App
            {
                public readonly partial struct FooId
                {
                    public FooId(System.Guid value) { Value = value; }
                    public System.Guid Value { get; }
                    public static FooId Parse(string s) => default;
                    public static bool TryParse(string s, out FooId result) { result = default; return false; }
                }

                public class C
                {
                    public FooId M(string s) => new FooId(System.Guid.Parse(s));
                }
            }
            """;

        await AnalyzerVerifier<StronglyTypedIdParsePatternAnalyzer>.VerifyAnalyzerAsync(source);
    }
}
