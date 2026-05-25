using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

using Xunit;

namespace NexusLabs.Framework.Analyzers.Tests;

public sealed class TryResultAnalyzerTests
{
    [Fact]
    public async Task ValueAccess_WithSuccessCheck_NoDiagnostic()
    {
        var source = """
            using NexusLabs.Framework;
            namespace Test
            {
                public class TestClass
                {
                    public void TestMethod()
                    {
                        var result = GetResult();
                        if (result.Success)
                        {
                            var value = result.Value;
                        }
                    }
                    private TriedEx<string> GetResult() => default;
                }
            }
            """;

        await VerifyAsync(source);
    }

    [Fact]
    public async Task ValueAccess_WithoutSuccessCheck_ReportsDiagnostic()
    {
        var source = """
            using NexusLabs.Framework;
            namespace Test
            {
                public class TestClass
                {
                    public void TestMethod()
                    {
                        var result = GetResult();
                        var value = {|#0:result.Value|};
                    }
                    private TriedEx<string> GetResult() => default;
                }
            }
            """;

        var expected = new DiagnosticResult("NLF0002", DiagnosticSeverity.Warning)
            .WithLocation(0);

        await VerifyAsync(source, expected);
    }

    [Fact]
    public async Task ErrorAccess_WithSuccessCheckFalse_NoDiagnostic()
    {
        var source = """
            using NexusLabs.Framework;
            namespace Test
            {
                public class TestClass
                {
                    public void TestMethod()
                    {
                        var result = GetResult();
                        if (!result.Success)
                        {
                            var error = result.Error;
                        }
                    }
                    private TriedEx<string> GetResult() => default;
                }
            }
            """;

        await VerifyAsync(source);
    }

    [Fact]
    public async Task ErrorAccess_WithoutSuccessCheck_ReportsDiagnostic()
    {
        var source = """
            using NexusLabs.Framework;
            namespace Test
            {
                public class TestClass
                {
                    public void TestMethod()
                    {
                        var result = GetResult();
                        var error = {|#0:result.Error|};
                    }
                    private TriedEx<string> GetResult() => default;
                }
            }
            """;

        var expected = new DiagnosticResult("NLF0003", DiagnosticSeverity.Warning)
            .WithLocation(0);

        await VerifyAsync(source, expected);
    }

    [Fact]
    public async Task ValueAccess_InTernaryOperator_WithSuccessCheck_NoDiagnostic()
    {
        var source = """
            using NexusLabs.Framework;
            namespace Test
            {
                public class TestClass
                {
                    public void TestMethod()
                    {
                        var result = GetResult();
                        var value = result.Success ? result.Value : "default";
                    }
                    private TriedEx<string> GetResult() => default;
                }
            }
            """;

        await VerifyAsync(source);
    }

    [Fact]
    public async Task ErrorAccess_InTernaryOperator_WithSuccessCheck_NoDiagnostic()
    {
        var source = """
            using NexusLabs.Framework;
            namespace Test
            {
                public class TestClass
                {
                    public void TestMethod()
                    {
                        var result = GetResult();
                        var error = result.Success ? null : result.Error;
                    }
                    private TriedEx<string> GetResult() => default;
                }
            }
            """;

        await VerifyAsync(source);
    }

    [Fact]
    public async Task TriedNullEx_ValueAccess_WithSuccessCheck_NoDiagnostic()
    {
        var source = """
            using NexusLabs.Framework;
            namespace Test
            {
                public class TestClass
                {
                    public void TestMethod()
                    {
                        var result = GetResult();
                        if (result.Success)
                        {
                            var value = result.Value;
                        }
                    }
                    private TriedNullEx<string?> GetResult() => default;
                }
            }
            """;

        await VerifyAsync(source);
    }

    [Fact]
    public async Task TriedNullEx_ValueAccess_WithoutSuccessCheck_ReportsDiagnostic()
    {
        var source = """
            using NexusLabs.Framework;
            namespace Test
            {
                public class TestClass
                {
                    public void TestMethod()
                    {
                        var result = GetResult();
                        var value = {|#0:result.Value|};
                    }
                    private TriedNullEx<string?> GetResult() => default;
                }
            }
            """;

        var expected = new DiagnosticResult("NLF0002", DiagnosticSeverity.Warning)
            .WithLocation(0);

        await VerifyAsync(source, expected);
    }

    [Fact]
    public async Task SuccessPropertyAccess_NoDiagnostic()
    {
        var source = """
            using NexusLabs.Framework;
            namespace Test
            {
                public class TestClass
                {
                    public void TestMethod()
                    {
                        var result = GetResult();
                        var success = result.Success;
                    }
                    private TriedEx<string> GetResult() => default;
                }
            }
            """;

        await VerifyAsync(source);
    }

    [Fact]
    public async Task ValueAccess_WithAndCondition_NoDiagnostic()
    {
        var source = """
            using NexusLabs.Framework;
            namespace Test
            {
                public class TestClass
                {
                    public void TestMethod(bool otherCondition)
                    {
                        var result = GetResult();
                        if (otherCondition && result.Success)
                        {
                            var value = result.Value;
                        }
                    }
                    private TriedEx<string> GetResult() => default;
                }
            }
            """;

        await VerifyAsync(source);
    }

    [Fact]
    public async Task NonTryResultType_NoDiagnostic()
    {
        var source = """
            namespace Test
            {
                public class MyType
                {
                    public bool Success { get; set; }
                    public string Value { get; set; } = "";
                }
                public class TestClass
                {
                    public void TestMethod()
                    {
                        var result = new MyType();
                        var value = result.Value;
                    }
                }
            }
            """;

        await VerifyAsync(source);
    }

    [Fact]
    public async Task ErrorAccess_AfterEarlyReturnOnSuccess_NoDiagnostic()
    {
        var source = """
            using System;
            using NexusLabs.Framework;
            namespace Test
            {
                public class TestClass
                {
                    public Exception ProcessResult()
                    {
                        var result = GetResult();
                        if (result.Success)
                        {
                            return new Exception("Success");
                        }
                        var error = result.Error;
                        return error;
                    }
                    private TriedEx<string> GetResult() => default;
                }
            }
            """;

        await VerifyAsync(source);
    }

    [Fact]
    public async Task ErrorAccess_AfterEarlyReturnOnSuccessInComplexScenario_NoDiagnostic()
    {
        var source = """
            using System;
            using NexusLabs.Framework;
            namespace Test
            {
                public class TestClass
                {
                    public void ProcessResult()
                    {
                        var result = GetResult();
                        if (result.Success)
                        {
                            return;
                        }
                        var error1 = result.Error;
                        if (result.Error is OperationCanceledException)
                        {
                            var error2 = result.Error;
                        }
                        var error3 = result.Error;
                    }
                    private TriedEx<string> GetResult() => default;
                }
            }
            """;

        await VerifyAsync(source);
    }

    [Fact]
    public async Task ValueAccess_AfterEarlyReturnOnNotSuccess_NoDiagnostic()
    {
        var source = """
            using NexusLabs.Framework;
            namespace Test
            {
                public class TestClass
                {
                    public string ProcessResult()
                    {
                        var result = GetResult();
                        if (!result.Success)
                        {
                            return "default";
                        }
                        var value = result.Value;
                        return value;
                    }
                    private TriedEx<string> GetResult() => default;
                }
            }
            """;

        await VerifyAsync(source);
    }

    [Fact]
    public async Task ErrorAccess_AfterContinueOnSuccess_NoDiagnostic()
    {
        var source = """
            using System;
            using NexusLabs.Framework;
            namespace Test
            {
                public class TestClass
                {
                    public void ProcessResults()
                    {
                        var results = GetResults();
                        foreach (var result in results)
                        {
                            if (result.Success)
                            {
                                continue;
                            }
                            var error = result.Error;
                            Console.WriteLine(error.Message);
                        }
                    }
                    private TriedEx<string>[] GetResults() => default!;
                }
            }
            """;

        await VerifyAsync(source);
    }

    [Fact]
    public async Task ValueAccess_AfterContinueOnNotSuccess_NoDiagnostic()
    {
        var source = """
            using System;
            using NexusLabs.Framework;
            namespace Test
            {
                public class TestClass
                {
                    public void ProcessResults()
                    {
                        var results = GetResults();
                        foreach (var result in results)
                        {
                            if (!result.Success)
                            {
                                continue;
                            }
                            var value = result.Value;
                            Console.WriteLine(value);
                        }
                    }
                    private TriedEx<string>[] GetResults() => default!;
                }
            }
            """;

        await VerifyAsync(source);
    }

    [Fact]
    public async Task ErrorAccess_AfterBreakOnSuccess_NoDiagnostic()
    {
        var source = """
            using System;
            using NexusLabs.Framework;
            namespace Test
            {
                public class TestClass
                {
                    public void ProcessResults()
                    {
                        var results = GetResults();
                        TriedEx<string> result = default;
                        foreach (var r in results)
                        {
                            if (r.Success)
                            {
                                break;
                            }
                            result = r;
                        }
                        if (result.Success)
                        {
                            return;
                        }
                        var error = result.Error;
                        Console.WriteLine(error.Message);
                    }
                    private TriedEx<string>[] GetResults() => default!;
                }
            }
            """;

        await VerifyAsync(source);
    }

    [Fact]
    public async Task ErrorAccess_InShortCircuitAnd_WithNotSuccessCheck_NoDiagnostic()
    {
        var source = """
            using System;
            using NexusLabs.Framework;
            namespace Test
            {
                public class TestClass
                {
                    public void ProcessResult()
                    {
                        var result = GetResult();
                        if (!result.Success && IsError(result.Error))
                        {
                            Console.WriteLine("Has error");
                        }
                    }
                    private TriedEx<string> GetResult() => default;
                    private bool IsError(Exception ex) => true;
                }
            }
            """;

        await VerifyAsync(source);
    }

    [Fact]
    public async Task ValueAccess_InShortCircuitAnd_WithSuccessCheck_NoDiagnostic()
    {
        var source = """
            using System;
            using NexusLabs.Framework;
            namespace Test
            {
                public class TestClass
                {
                    public void ProcessResult()
                    {
                        var result = GetResult();
                        if (result.Success && !string.IsNullOrEmpty(result.Value))
                        {
                            Console.WriteLine(result.Value);
                        }
                    }
                    private TriedEx<string> GetResult() => default;
                }
            }
            """;

        await VerifyAsync(source);
    }

    [Fact]
    public async Task ErrorAccess_InShortCircuitOr_WithSuccessCheck_NoDiagnostic()
    {
        var source = """
            using System;
            using NexusLabs.Framework;
            namespace Test
            {
                public class TestClass
                {
                    public void ProcessResult()
                    {
                        var result = GetResult();
                        if (result.Success || IsError(result.Error))
                        {
                            Console.WriteLine("Success or has error");
                        }
                    }
                    private TriedEx<string> GetResult() => default;
                    private bool IsError(Exception ex) => true;
                }
            }
            """;

        await VerifyAsync(source);
    }

    [Fact]
    public async Task ValueAccess_InShortCircuitOr_WithNotSuccessCheck_NoDiagnostic()
    {
        var source = """
            using System;
            using NexusLabs.Framework;
            namespace Test
            {
                public class TestClass
                {
                    public void ProcessResult()
                    {
                        var result = GetResult();
                        if (!result.Success || string.IsNullOrEmpty(result.Value))
                        {
                            Console.WriteLine("Not success or empty value");
                        }
                    }
                    private TriedEx<string> GetResult() => default;
                }
            }
            """;

        await VerifyAsync(source);
    }

    [Fact]
    public async Task ValueAccess_InReturnStatementWithShortCircuitAnd_NoDiagnostic()
    {
        var source = """
            using NexusLabs.Framework;
            namespace Test
            {
                public class TestClass
                {
                    public bool ProcessResult()
                    {
                        var result = GetResult();
                        return result.Success && result.Value;
                    }
                    private TriedEx<bool> GetResult() => default;
                }
            }
            """;

        await VerifyAsync(source);
    }

    [Fact]
    public async Task ErrorAccess_InReturnStatementWithShortCircuitOr_NoDiagnostic()
    {
        var source = """
            using System;
            using NexusLabs.Framework;
            namespace Test
            {
                public class TestClass
                {
                    public bool ProcessResult()
                    {
                        var result = GetResult();
                        return result.Success || IsError(result.Error);
                    }
                    private TriedEx<string> GetResult() => default;
                    private bool IsError(Exception ex) => true;
                }
            }
            """;

        await VerifyAsync(source);
    }

    private static async Task VerifyAsync(string source, params DiagnosticResult[] expected)
    {
        var test = new CSharpAnalyzerTest<TryResultAnalyzer, DefaultVerifier>
        {
            TestCode = source,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
        };

        test.TestState.Sources.Add(("TriedExStubs.cs", TestSources.TriedExStubs));
        test.ExpectedDiagnostics.AddRange(expected);

        await test.RunAsync();
    }
}
