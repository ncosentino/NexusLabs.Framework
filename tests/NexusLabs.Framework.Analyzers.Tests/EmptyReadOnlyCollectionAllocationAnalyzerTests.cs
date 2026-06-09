using System.Threading.Tasks;

using NexusLabs.Framework.Analyzers;

using Xunit;

namespace NexusLabs.Framework.Analyzers.Tests;

public sealed class EmptyReadOnlyCollectionAllocationAnalyzerTests
{
    private const string ListReplacement =
        "the collection expression `[]` (equivalently `Array.Empty<int>()`)";

    [Fact]
    public async Task ReturnNewList_AsIReadOnlyList_Reports()
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

        var expected = AnalyzerVerifier<EmptyReadOnlyCollectionAllocationAnalyzer>
            .Diagnostic(DiagnosticDescriptors.DoNotAllocateEmptyReadOnlyCollection)
            .WithLocation(0)
            .WithArguments("new List<int>()", "IReadOnlyList<int>", ListReplacement);

        await AnalyzerVerifier<EmptyReadOnlyCollectionAllocationAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task ReturnNewDictionary_AsIReadOnlyDictionary_Reports()
    {
        var source =
            """
            using System.Collections.Generic;

            namespace App
            {
                public sealed class C
                {
                    public IReadOnlyDictionary<string, int> M()
                    {
                        return new {|#0:Dictionary<string, int>|}();
                    }
                }
            }
            """;

        var expected = AnalyzerVerifier<EmptyReadOnlyCollectionAllocationAnalyzer>
            .Diagnostic(DiagnosticDescriptors.DoNotAllocateEmptyReadOnlyCollection)
            .WithLocation(0)
            .WithArguments(
                "new Dictionary<string, int>()",
                "IReadOnlyDictionary<string, int>",
                "`ReadOnlyDictionary<string, int>.Empty`");

        await AnalyzerVerifier<EmptyReadOnlyCollectionAllocationAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task ReturnNewHashSet_AsIReadOnlySet_Reports()
    {
        var source =
            """
            using System.Collections.Generic;

            namespace App
            {
                public sealed class C
                {
                    public IReadOnlySet<int> M()
                    {
                        return new {|#0:HashSet<int>|}();
                    }
                }
            }
            """;

        var expected = AnalyzerVerifier<EmptyReadOnlyCollectionAllocationAnalyzer>
            .Diagnostic(DiagnosticDescriptors.DoNotAllocateEmptyReadOnlyCollection)
            .WithLocation(0)
            .WithArguments("new HashSet<int>()", "IReadOnlySet<int>", "`FrozenSet<int>.Empty`");

        await AnalyzerVerifier<EmptyReadOnlyCollectionAllocationAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task ExpressionBodied_AsIEnumerable_Reports()
    {
        var source =
            """
            using System.Collections.Generic;

            namespace App
            {
                public sealed class C
                {
                    public IEnumerable<int> M() => new {|#0:List<int>|}();
                }
            }
            """;

        var expected = AnalyzerVerifier<EmptyReadOnlyCollectionAllocationAnalyzer>
            .Diagnostic(DiagnosticDescriptors.DoNotAllocateEmptyReadOnlyCollection)
            .WithLocation(0)
            .WithArguments("new List<int>()", "IEnumerable<int>", ListReplacement);

        await AnalyzerVerifier<EmptyReadOnlyCollectionAllocationAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task FieldInitializer_AsIReadOnlyList_Reports()
    {
        var source =
            """
            using System.Collections.Generic;

            namespace App
            {
                public sealed class C
                {
                    private static readonly IReadOnlyList<int> Seed = new {|#0:List<int>|}();
                }
            }
            """;

        var expected = AnalyzerVerifier<EmptyReadOnlyCollectionAllocationAnalyzer>
            .Diagnostic(DiagnosticDescriptors.DoNotAllocateEmptyReadOnlyCollection)
            .WithLocation(0)
            .WithArguments("new List<int>()", "IReadOnlyList<int>", ListReplacement);

        await AnalyzerVerifier<EmptyReadOnlyCollectionAllocationAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task Argument_AsIReadOnlyList_Reports()
    {
        var source =
            """
            using System.Collections.Generic;

            namespace App
            {
                public sealed class C
                {
                    public void M() => Process(new {|#0:List<int>|}());

                    private static void Process(IReadOnlyList<int> items)
                    {
                    }
                }
            }
            """;

        var expected = AnalyzerVerifier<EmptyReadOnlyCollectionAllocationAnalyzer>
            .Diagnostic(DiagnosticDescriptors.DoNotAllocateEmptyReadOnlyCollection)
            .WithLocation(0)
            .WithArguments("new List<int>()", "IReadOnlyList<int>", ListReplacement);

        await AnalyzerVerifier<EmptyReadOnlyCollectionAllocationAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task EmptyInitializer_AsIReadOnlyList_Reports()
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
                        return new {|#0:List<int>|}() { };
                    }
                }
            }
            """;

        var expected = AnalyzerVerifier<EmptyReadOnlyCollectionAllocationAnalyzer>
            .Diagnostic(DiagnosticDescriptors.DoNotAllocateEmptyReadOnlyCollection)
            .WithLocation(0)
            .WithArguments("new List<int>()", "IReadOnlyList<int>", ListReplacement);

        await AnalyzerVerifier<EmptyReadOnlyCollectionAllocationAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task ReturnNewList_AsConcreteList_NoReport()
    {
        var source =
            """
            using System.Collections.Generic;

            namespace App
            {
                public sealed class C
                {
                    public List<int> M()
                    {
                        return new List<int>();
                    }
                }
            }
            """;

        await AnalyzerVerifier<EmptyReadOnlyCollectionAllocationAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task ReturnNewList_AsMutableIList_NoReport()
    {
        var source =
            """
            using System.Collections.Generic;

            namespace App
            {
                public sealed class C
                {
                    public IList<int> M()
                    {
                        return new List<int>();
                    }
                }
            }
            """;

        await AnalyzerVerifier<EmptyReadOnlyCollectionAllocationAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task ReturnNewHashSet_AsMutableICollection_NoReport()
    {
        var source =
            """
            using System.Collections.Generic;

            namespace App
            {
                public sealed class C
                {
                    public ICollection<int> M()
                    {
                        return new HashSet<int>();
                    }
                }
            }
            """;

        await AnalyzerVerifier<EmptyReadOnlyCollectionAllocationAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task NonEmptyInitializer_NoReport()
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
                        return new List<int> { 1, 2 };
                    }
                }
            }
            """;

        await AnalyzerVerifier<EmptyReadOnlyCollectionAllocationAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task CapacityConstructor_NoReport()
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
                        return new List<int>(16);
                    }
                }
            }
            """;

        await AnalyzerVerifier<EmptyReadOnlyCollectionAllocationAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task MutateThenReturn_NoReport()
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
                        var items = new List<int>();
                        items.Add(1);
                        return items;
                    }
                }
            }
            """;

        await AnalyzerVerifier<EmptyReadOnlyCollectionAllocationAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task CollectionExpression_NoReport()
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
                        return [];
                    }
                }
            }
            """;

        await AnalyzerVerifier<EmptyReadOnlyCollectionAllocationAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task ReturnNewSortedDictionary_AsIReadOnlyDictionary_Reports()
    {
        var source =
            """
            using System.Collections.Generic;

            namespace App
            {
                public sealed class C
                {
                    public IReadOnlyDictionary<string, int> M()
                    {
                        return new {|#0:SortedDictionary<string, int>|}();
                    }
                }
            }
            """;

        var expected = AnalyzerVerifier<EmptyReadOnlyCollectionAllocationAnalyzer>
            .Diagnostic(DiagnosticDescriptors.DoNotAllocateEmptyReadOnlyCollection)
            .WithLocation(0)
            .WithArguments(
                "new SortedDictionary<string, int>()",
                "IReadOnlyDictionary<string, int>",
                "`ReadOnlyDictionary<string, int>.Empty`");

        await AnalyzerVerifier<EmptyReadOnlyCollectionAllocationAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task ReturnNewObservableCollection_AsIReadOnlyList_Reports()
    {
        var source =
            """
            using System.Collections.Generic;
            using System.Collections.ObjectModel;

            namespace App
            {
                public sealed class C
                {
                    public IReadOnlyList<int> M()
                    {
                        return new {|#0:ObservableCollection<int>|}();
                    }
                }
            }
            """;

        var expected = AnalyzerVerifier<EmptyReadOnlyCollectionAllocationAnalyzer>
            .Diagnostic(DiagnosticDescriptors.DoNotAllocateEmptyReadOnlyCollection)
            .WithLocation(0)
            .WithArguments("new ObservableCollection<int>()", "IReadOnlyList<int>", ListReplacement);

        await AnalyzerVerifier<EmptyReadOnlyCollectionAllocationAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task ReturnNewSortedSet_AsIReadOnlySet_Reports()
    {
        var source =
            """
            using System.Collections.Generic;

            namespace App
            {
                public sealed class C
                {
                    public IReadOnlySet<int> M()
                    {
                        return new {|#0:SortedSet<int>|}();
                    }
                }
            }
            """;

        var expected = AnalyzerVerifier<EmptyReadOnlyCollectionAllocationAnalyzer>
            .Diagnostic(DiagnosticDescriptors.DoNotAllocateEmptyReadOnlyCollection)
            .WithLocation(0)
            .WithArguments("new SortedSet<int>()", "IReadOnlySet<int>", "`FrozenSet<int>.Empty`");

        await AnalyzerVerifier<EmptyReadOnlyCollectionAllocationAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task ReturnNewUserDefinedCollection_AsIReadOnlyList_Reports()
    {
        var source =
            """
            using System.Collections;
            using System.Collections.Generic;

            namespace App
            {
                public sealed class Bag : IReadOnlyList<int>
                {
                    public int Count => 0;
                    public int this[int index] => throw new System.IndexOutOfRangeException();
                    public IEnumerator<int> GetEnumerator() { yield break; }
                    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
                }

                public sealed class C
                {
                    public IReadOnlyList<int> M()
                    {
                        return new {|#0:Bag|}();
                    }
                }
            }
            """;

        var expected = AnalyzerVerifier<EmptyReadOnlyCollectionAllocationAnalyzer>
            .Diagnostic(DiagnosticDescriptors.DoNotAllocateEmptyReadOnlyCollection)
            .WithLocation(0)
            .WithArguments("new Bag()", "IReadOnlyList<int>", ListReplacement);

        await AnalyzerVerifier<EmptyReadOnlyCollectionAllocationAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }
}
