using System.Threading.Tasks;

using NexusLabs.Framework.Analyzers;

using Xunit;

namespace NexusLabs.Framework.Analyzers.Tests;

public sealed class ParseFormatProviderAnalyzerTests
{
    [Fact]
    public async Task IntParse_NoProvider_Reports()
    {
        var source =
            """
            namespace App
            {
                public class C
                {
                    public int M(string s) => int.{|#0:Parse|}(s);
                }
            }
            """;

        var expected = AnalyzerVerifier<ParseFormatProviderAnalyzer>
            .Diagnostic(DiagnosticDescriptors.ParseTryParseMissingFormatProvider)
            .WithLocation(0)
            .WithArguments("int", "Parse");

        await AnalyzerVerifier<ParseFormatProviderAnalyzer>.VerifyAnalyzerAsync(source, expected, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task IntParse_WithProvider_DoesNotReport()
    {
        var source =
            """
            using System.Globalization;
            namespace App
            {
                public class C
                {
                    public int M(string s) => int.Parse(s, CultureInfo.InvariantCulture);
                }
            }
            """;

        await AnalyzerVerifier<ParseFormatProviderAnalyzer>.VerifyAnalyzerAsync(source, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task IntParse_WithNullProvider_DoesNotReport()
    {
        var source =
            """
            namespace App
            {
                public class C
                {
                    public int M(string s) => int.Parse(s, (System.IFormatProvider)null);
                }
            }
            """;

        await AnalyzerVerifier<ParseFormatProviderAnalyzer>.VerifyAnalyzerAsync(source, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task IntTryParse_NoProvider_Reports()
    {
        var source =
            """
            namespace App
            {
                public class C
                {
                    public bool M(string s, out int value) => int.{|#0:TryParse|}(s, out value);
                }
            }
            """;

        var expected = AnalyzerVerifier<ParseFormatProviderAnalyzer>
            .Diagnostic(DiagnosticDescriptors.ParseTryParseMissingFormatProvider)
            .WithLocation(0)
            .WithArguments("int", "TryParse");

        await AnalyzerVerifier<ParseFormatProviderAnalyzer>.VerifyAnalyzerAsync(source, expected, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task IntTryParse_WithProvider_DoesNotReport()
    {
        var source =
            """
            using System.Globalization;
            namespace App
            {
                public class C
                {
                    public bool M(string s, out int value) =>
                        int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
                }
            }
            """;

        await AnalyzerVerifier<ParseFormatProviderAnalyzer>.VerifyAnalyzerAsync(source, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task IntParse_WithNumberStylesButNoProvider_Reports()
    {
        var source =
            """
            using System.Globalization;
            namespace App
            {
                public class C
                {
                    public int M(string s) => int.{|#0:Parse|}(s, NumberStyles.Integer);
                }
            }
            """;

        var expected = AnalyzerVerifier<ParseFormatProviderAnalyzer>
            .Diagnostic(DiagnosticDescriptors.ParseTryParseMissingFormatProvider)
            .WithLocation(0)
            .WithArguments("int", "Parse");

        await AnalyzerVerifier<ParseFormatProviderAnalyzer>.VerifyAnalyzerAsync(source, expected, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GuidParse_NoProvider_Reports()
    {
        var source =
            """
            namespace App
            {
                public class C
                {
                    public System.Guid M(string s) => System.Guid.{|#0:Parse|}(s);
                }
            }
            """;

        var expected = AnalyzerVerifier<ParseFormatProviderAnalyzer>
            .Diagnostic(DiagnosticDescriptors.ParseTryParseMissingFormatProvider)
            .WithLocation(0)
            .WithArguments("System.Guid", "Parse");

        await AnalyzerVerifier<ParseFormatProviderAnalyzer>.VerifyAnalyzerAsync(source, expected, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task DateTimeParse_NoProvider_Reports()
    {
        var source =
            """
            namespace App
            {
                public class C
                {
                    public System.DateTime M(string s) => System.DateTime.{|#0:Parse|}(s);
                }
            }
            """;

        var expected = AnalyzerVerifier<ParseFormatProviderAnalyzer>
            .Diagnostic(DiagnosticDescriptors.ParseTryParseMissingFormatProvider)
            .WithLocation(0)
            .WithArguments("System.DateTime", "Parse");

        await AnalyzerVerifier<ParseFormatProviderAnalyzer>.VerifyAnalyzerAsync(source, expected, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task UserDefinedTypeWithoutFormatProviderOverload_DoesNotReport()
    {
        var source =
            """
            namespace App
            {
                public static class MyParser
                {
                    public static int Parse(string s) => 0;
                    public static bool TryParse(string s, out int value) { value = 0; return false; }
                }

                public class C
                {
                    public int M(string s) => MyParser.Parse(s);
                    public bool N(string s, out int v) => MyParser.TryParse(s, out v);
                }
            }
            """;

        await AnalyzerVerifier<ParseFormatProviderAnalyzer>.VerifyAnalyzerAsync(source, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task UserDefinedTypeWithIncompatibleFormatProviderOverload_DoesNotReport()
    {
        var source =
            """
            namespace App
            {
                public static class MyParser
                {
                    public static int Parse(string s) => 0;
                    public static int Parse(System.ReadOnlySpan<char> s, System.IFormatProvider provider) => 0;
                }

                public class C
                {
                    public int M(string s) => MyParser.Parse(s);
                }
            }
            """;

        await AnalyzerVerifier<ParseFormatProviderAnalyzer>.VerifyAnalyzerAsync(source, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task UserDefinedTypeWithCompatibleOverload_Reports()
    {
        var source =
            """
            namespace App
            {
                public static class MyParser
                {
                    public static int Parse(string s) => 0;
                    public static int Parse(string s, System.IFormatProvider provider) => 0;
                }

                public class C
                {
                    public int M(string s) => MyParser.{|#0:Parse|}(s);
                }
            }
            """;

        var expected = AnalyzerVerifier<ParseFormatProviderAnalyzer>
            .Diagnostic(DiagnosticDescriptors.ParseTryParseMissingFormatProvider)
            .WithLocation(0)
            .WithArguments("App.MyParser", "Parse");

        await AnalyzerVerifier<ParseFormatProviderAnalyzer>.VerifyAnalyzerAsync(source, expected, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task InstanceParseMethod_DoesNotReport()
    {
        var source =
            """
            namespace App
            {
                public class Parser
                {
                    public int Parse(string s) => 0;
                    public int Parse(string s, System.IFormatProvider provider) => 0;
                }

                public class C
                {
                    public int M(Parser p, string s) => p.Parse(s);
                }
            }
            """;

        await AnalyzerVerifier<ParseFormatProviderAnalyzer>.VerifyAnalyzerAsync(source, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ExtensionParseMethod_DoesNotReport()
    {
        var source =
            """
            namespace App
            {
                public static class ParserExt
                {
                    public static int Parse(this string s) => 0;
                    public static int Parse(this string s, System.IFormatProvider provider) => 0;
                }

                public class C
                {
                    public int M(string s) => s.Parse();
                }
            }
            """;

        await AnalyzerVerifier<ParseFormatProviderAnalyzer>.VerifyAnalyzerAsync(source, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task UnrelatedStaticMethodNamedParse_DoesNotReport()
    {
        var source =
            """
            namespace App
            {
                public static class C
                {
                    public static int OtherName(string s) => 0;
                    public static int OtherName(string s, System.IFormatProvider provider) => 0;

                    public static int M(string s) => OtherName(s);
                }
            }
            """;

        await AnalyzerVerifier<ParseFormatProviderAnalyzer>.VerifyAnalyzerAsync(source, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task DecimalParse_WithCultureInfo_DoesNotReport()
    {
        var source =
            """
            using System.Globalization;
            namespace App
            {
                public class C
                {
                    public decimal M(string s) => decimal.Parse(s, CultureInfo.InvariantCulture);
                }
            }
            """;

        await AnalyzerVerifier<ParseFormatProviderAnalyzer>.VerifyAnalyzerAsync(source, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task DecimalParse_NoProvider_Reports()
    {
        var source =
            """
            namespace App
            {
                public class C
                {
                    public decimal M(string s) => decimal.{|#0:Parse|}(s);
                }
            }
            """;

        var expected = AnalyzerVerifier<ParseFormatProviderAnalyzer>
            .Diagnostic(DiagnosticDescriptors.ParseTryParseMissingFormatProvider)
            .WithLocation(0)
            .WithArguments("decimal", "Parse");

        await AnalyzerVerifier<ParseFormatProviderAnalyzer>.VerifyAnalyzerAsync(source, expected, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task IntTryParse_OutAsLastParam_FormatProviderInMiddle_Reports()
    {
        var source =
            """
            namespace App
            {
                public class C
                {
                    public bool M(string s, out int v) => int.{|#0:TryParse|}(s, out v);
                }
            }
            """;

        var expected = AnalyzerVerifier<ParseFormatProviderAnalyzer>
            .Diagnostic(DiagnosticDescriptors.ParseTryParseMissingFormatProvider)
            .WithLocation(0)
            .WithArguments("int", "TryParse");

        await AnalyzerVerifier<ParseFormatProviderAnalyzer>.VerifyAnalyzerAsync(source, expected, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task DateTimeOffsetParse_WithCultureInfoNonNull_DoesNotReport()
    {
        var source =
            """
            using System.Globalization;
            namespace App
            {
                public class C
                {
                    public System.DateTimeOffset M(string s) =>
                        System.DateTimeOffset.Parse(s, CultureInfo.InvariantCulture);
                }
            }
            """;

        await AnalyzerVerifier<ParseFormatProviderAnalyzer>.VerifyAnalyzerAsync(source, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ConstructorInvocation_NotFlagged()
    {
        var source =
            """
            namespace App
            {
                public class C
                {
                    public string M() => new string('a', 5);
                }
            }
            """;

        await AnalyzerVerifier<ParseFormatProviderAnalyzer>.VerifyAnalyzerAsync(source, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task BoolParse_NoProvider_DoesNotReport()
    {
        // bool's IParsable<bool> implementation is explicit, so there is no
        // public-callable bool.Parse(string, IFormatProvider) overload to
        // upgrade to. The analyzer correctly stays silent for this exact
        // reason — it only flags when a compatible *public* upgrade exists.
        var source =
            """
            namespace App
            {
                public class C
                {
                    public bool M(string s) => bool.Parse(s);
                }
            }
            """;

        await AnalyzerVerifier<ParseFormatProviderAnalyzer>.VerifyAnalyzerAsync(source, TestContext.Current.CancellationToken);
    }
}
