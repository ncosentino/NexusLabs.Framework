using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

using Xunit;

namespace NexusLabs.Framework.Analyzers.Tests;

public sealed class EmptyReadOnlyCollectionCodeFixProviderTests
{
    [Fact]
    public async Task List_Fix_UsesCollectionExpression()
    {
        var source =
            """
            using System.Collections.Generic;

            namespace App
            {
                public sealed class C
                {
                    public IReadOnlyList<int> M() => new {|#0:List<int>|}();
                }
            }
            """;

        var fixedSource =
            """
            using System.Collections.Generic;

            namespace App
            {
                public sealed class C
                {
                    public IReadOnlyList<int> M() => [];
                }
            }
            """;

        var expected = new DiagnosticResult("NLF0019", DiagnosticSeverity.Warning).WithLocation(0);
        await VerifyCodeFixAsync(source, fixedSource, expected);
    }

    [Fact]
    public async Task Dictionary_Fix_UsesReadOnlyDictionaryEmpty()
    {
        var source =
            """
            using System.Collections.Generic;

            namespace App
            {
                public sealed class C
                {
                    public IReadOnlyDictionary<string, int> M() => new {|#0:Dictionary<string, int>|}();
                }
            }
            """;

        var fixedSource =
            """
            using System.Collections.Generic;
            using System.Collections.ObjectModel;

            namespace App
            {
                public sealed class C
                {
                    public IReadOnlyDictionary<string, int> M() => ReadOnlyDictionary<string, int>.Empty;
                }
            }
            """;

        var expected = new DiagnosticResult("NLF0019", DiagnosticSeverity.Warning).WithLocation(0);
        await VerifyCodeFixAsync(source, fixedSource, expected);
    }

    [Fact]
    public async Task Set_Fix_UsesFrozenSetEmpty()
    {
        var source =
            """
            using System.Collections.Generic;

            namespace App
            {
                public sealed class C
                {
                    public IReadOnlySet<int> M() => new {|#0:HashSet<int>|}();
                }
            }
            """;

        var fixedSource =
            """
            using System.Collections.Frozen;
            using System.Collections.Generic;

            namespace App
            {
                public sealed class C
                {
                    public IReadOnlySet<int> M() => FrozenSet<int>.Empty;
                }
            }
            """;

        var expected = new DiagnosticResult("NLF0019", DiagnosticSeverity.Warning).WithLocation(0);
        await VerifyCodeFixAsync(source, fixedSource, expected);
    }

    [Fact]
    public async Task Return_List_Fix_UsesCollectionExpression()
    {
        var source =
            """
            using System.Collections.Generic;

            namespace App
            {
                public sealed class C
                {
                    public IReadOnlyList<int> M()
                    {
                        return new {|#0:List<int>|}();
                    }
                }
            }
            """;

        var fixedSource =
            """
            using System.Collections.Generic;

            namespace App
            {
                public sealed class C
                {
                    public IReadOnlyList<int> M()
                    {
                        return [];
                    }
                }
            }
            """;

        var expected = new DiagnosticResult("NLF0019", DiagnosticSeverity.Warning).WithLocation(0);
        await VerifyCodeFixAsync(source, fixedSource, expected);
    }

    [Fact]
    public async Task SortedDictionary_Fix_UsesReadOnlyDictionaryEmpty()
    {
        var source =
            """
            using System.Collections.Generic;

            namespace App
            {
                public sealed class C
                {
                    public IReadOnlyDictionary<string, int> M() => new {|#0:SortedDictionary<string, int>|}();
                }
            }
            """;

        var fixedSource =
            """
            using System.Collections.Generic;
            using System.Collections.ObjectModel;

            namespace App
            {
                public sealed class C
                {
                    public IReadOnlyDictionary<string, int> M() => ReadOnlyDictionary<string, int>.Empty;
                }
            }
            """;

        var expected = new DiagnosticResult("NLF0019", DiagnosticSeverity.Warning).WithLocation(0);
        await VerifyCodeFixAsync(source, fixedSource, expected);
    }

    private static async Task VerifyCodeFixAsync(
        string source,
        string fixedSource,
        params DiagnosticResult[] expected)
    {
        var test = new CSharpCodeFixTest<EmptyReadOnlyCollectionAllocationAnalyzer, EmptyReadOnlyCollectionCodeFixProvider, DefaultVerifier>
        {
            TestCode = source,
            FixedCode = fixedSource,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
        };

        test.ExpectedDiagnostics.AddRange(expected);

        await test.RunAsync();
    }
}
