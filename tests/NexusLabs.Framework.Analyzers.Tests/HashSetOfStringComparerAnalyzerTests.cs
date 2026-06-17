using System.Threading.Tasks;

using Microsoft.CodeAnalysis;

using NexusLabs.Framework.Analyzers;

using Xunit;

namespace NexusLabs.Framework.Analyzers.Tests;

public sealed class HashSetOfStringComparerAnalyzerTests
{
    [Fact]
    public async Task NewHashSetOfString_DefaultCtor_Reports()
    {
        var source =
            """
            using System.Collections.Generic;

            namespace App
            {
                public sealed class C
                {
                    public void M()
                    {
                        var s = new {|#0:HashSet<string>|}();
                    }
                }
            }
            """;

        var expected = AnalyzerVerifier<HashSetOfStringComparerAnalyzer>
            .Diagnostic(DiagnosticDescriptors.HashSetOfStringMustUseOrdinalIgnoreCase)
            .WithLocation(0)
            .WithArguments("new HashSet<string>(...)");

        await AnalyzerVerifier<HashSetOfStringComparerAnalyzer>.VerifyAnalyzerAsync(source, expected, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task NewHashSetOfString_CapacityOnly_Reports()
    {
        var source =
            """
            using System.Collections.Generic;

            namespace App
            {
                public sealed class C
                {
                    public void M()
                    {
                        var s = new {|#0:HashSet<string>|}(16);
                    }
                }
            }
            """;

        var expected = AnalyzerVerifier<HashSetOfStringComparerAnalyzer>
            .Diagnostic(DiagnosticDescriptors.HashSetOfStringMustUseOrdinalIgnoreCase)
            .WithLocation(0)
            .WithArguments("new HashSet<string>(...)");

        await AnalyzerVerifier<HashSetOfStringComparerAnalyzer>.VerifyAnalyzerAsync(source, expected, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task NewHashSetOfString_SourceOnly_Reports()
    {
        var source =
            """
            using System.Collections.Generic;

            namespace App
            {
                public sealed class C
                {
                    public void M(IEnumerable<string> src)
                    {
                        var s = new {|#0:HashSet<string>|}(src);
                    }
                }
            }
            """;

        var expected = AnalyzerVerifier<HashSetOfStringComparerAnalyzer>
            .Diagnostic(DiagnosticDescriptors.HashSetOfStringMustUseOrdinalIgnoreCase)
            .WithLocation(0)
            .WithArguments("new HashSet<string>(...)");

        await AnalyzerVerifier<HashSetOfStringComparerAnalyzer>.VerifyAnalyzerAsync(source, expected, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task NewHashSetOfString_CollectionInitializer_Reports()
    {
        var source =
            """
            using System.Collections.Generic;

            namespace App
            {
                public sealed class C
                {
                    public void M()
                    {
                        var s = new {|#0:HashSet<string>|} { "a", "b" };
                    }
                }
            }
            """;

        var expected = AnalyzerVerifier<HashSetOfStringComparerAnalyzer>
            .Diagnostic(DiagnosticDescriptors.HashSetOfStringMustUseOrdinalIgnoreCase)
            .WithLocation(0)
            .WithArguments("new HashSet<string>(...)");

        await AnalyzerVerifier<HashSetOfStringComparerAnalyzer>.VerifyAnalyzerAsync(source, expected, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task NewHashSetOfString_WithOrdinalIgnoreCase_NoDiagnostic()
    {
        var source =
            """
            using System;
            using System.Collections.Generic;

            namespace App
            {
                public sealed class C
                {
                    public void M()
                    {
                        var s = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    }
                }
            }
            """;

        await AnalyzerVerifier<HashSetOfStringComparerAnalyzer>.VerifyAnalyzerAsync(source, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task NewHashSetOfString_WithOrdinalIgnoreCase_AndSource_NoDiagnostic()
    {
        var source =
            """
            using System;
            using System.Collections.Generic;

            namespace App
            {
                public sealed class C
                {
                    public void M(IEnumerable<string> src)
                    {
                        var s = new HashSet<string>(src, StringComparer.OrdinalIgnoreCase);
                    }
                }
            }
            """;

        await AnalyzerVerifier<HashSetOfStringComparerAnalyzer>.VerifyAnalyzerAsync(source, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task NewHashSetOfString_WithOrdinal_Reports()
    {
        // Ordinal (case-sensitive) is NOT OrdinalIgnoreCase; rule fires.
        var source =
            """
            using System;
            using System.Collections.Generic;

            namespace App
            {
                public sealed class C
                {
                    public void M()
                    {
                        var s = new {|#0:HashSet<string>|}(StringComparer.Ordinal);
                    }
                }
            }
            """;

        var expected = AnalyzerVerifier<HashSetOfStringComparerAnalyzer>
            .Diagnostic(DiagnosticDescriptors.HashSetOfStringMustUseOrdinalIgnoreCase)
            .WithLocation(0)
            .WithArguments("new HashSet<string>(...)");

        await AnalyzerVerifier<HashSetOfStringComparerAnalyzer>.VerifyAnalyzerAsync(source, expected, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task NewHashSetOfString_WithCurrentCultureIgnoreCase_Reports()
    {
        var source =
            """
            using System;
            using System.Collections.Generic;

            namespace App
            {
                public sealed class C
                {
                    public void M()
                    {
                        var s = new {|#0:HashSet<string>|}(StringComparer.CurrentCultureIgnoreCase);
                    }
                }
            }
            """;

        var expected = AnalyzerVerifier<HashSetOfStringComparerAnalyzer>
            .Diagnostic(DiagnosticDescriptors.HashSetOfStringMustUseOrdinalIgnoreCase)
            .WithLocation(0)
            .WithArguments("new HashSet<string>(...)");

        await AnalyzerVerifier<HashSetOfStringComparerAnalyzer>.VerifyAnalyzerAsync(source, expected, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task NewHashSetOfString_WithInvariantCultureIgnoreCase_Reports()
    {
        var source =
            """
            using System;
            using System.Collections.Generic;

            namespace App
            {
                public sealed class C
                {
                    public void M()
                    {
                        var s = new {|#0:HashSet<string>|}(StringComparer.InvariantCultureIgnoreCase);
                    }
                }
            }
            """;

        var expected = AnalyzerVerifier<HashSetOfStringComparerAnalyzer>
            .Diagnostic(DiagnosticDescriptors.HashSetOfStringMustUseOrdinalIgnoreCase)
            .WithLocation(0)
            .WithArguments("new HashSet<string>(...)");

        await AnalyzerVerifier<HashSetOfStringComparerAnalyzer>.VerifyAnalyzerAsync(source, expected, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task TargetTypedNew_HashSetOfString_NoComparer_Reports()
    {
        var source =
            """
            using System.Collections.Generic;

            namespace App
            {
                public sealed class C
                {
                    public void M()
                    {
                        HashSet<string> s = {|#0:new|}();
                    }
                }
            }
            """;

        var expected = AnalyzerVerifier<HashSetOfStringComparerAnalyzer>
            .Diagnostic(DiagnosticDescriptors.HashSetOfStringMustUseOrdinalIgnoreCase)
            .WithLocation(0)
            .WithArguments("target-typed `new()` HashSet<string>");

        await AnalyzerVerifier<HashSetOfStringComparerAnalyzer>.VerifyAnalyzerAsync(source, expected, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task TargetTypedNew_HashSetOfString_WithOrdinalIgnoreCase_NoDiagnostic()
    {
        var source =
            """
            using System;
            using System.Collections.Generic;

            namespace App
            {
                public sealed class C
                {
                    public void M()
                    {
                        HashSet<string> s = new(StringComparer.OrdinalIgnoreCase);
                    }
                }
            }
            """;

        await AnalyzerVerifier<HashSetOfStringComparerAnalyzer>.VerifyAnalyzerAsync(source, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task FieldInitializer_HashSetOfString_NoComparer_Reports()
    {
        var source =
            """
            using System.Collections.Generic;

            namespace App
            {
                public sealed class C
                {
                    private readonly HashSet<string> _items = new {|#0:HashSet<string>|}();
                }
            }
            """;

        var expected = AnalyzerVerifier<HashSetOfStringComparerAnalyzer>
            .Diagnostic(DiagnosticDescriptors.HashSetOfStringMustUseOrdinalIgnoreCase)
            .WithLocation(0)
            .WithArguments("new HashSet<string>(...)");

        await AnalyzerVerifier<HashSetOfStringComparerAnalyzer>.VerifyAnalyzerAsync(source, expected, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task NonStringHashSet_DoesNotReport()
    {
        var source =
            """
            using System;
            using System.Collections.Generic;

            namespace App
            {
                public sealed class C
                {
                    public void M()
                    {
                        var a = new HashSet<int>();
                        var b = new HashSet<Guid>();
                        var c = new HashSet<object>();
                        HashSet<long> d = new();
                    }
                }
            }
            """;

        await AnalyzerVerifier<HashSetOfStringComparerAnalyzer>.VerifyAnalyzerAsync(source, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ImmutableHashSetOfString_DoesNotReport()
    {
        // Out of scope: ImmutableHashSet<string> is a separate type.
        var source =
            """
            using System.Collections.Immutable;

            namespace App
            {
                public sealed class C
                {
                    public void M()
                    {
                        var s = ImmutableHashSet.Create<string>("a", "b");
                    }
                }
            }
            """;

        await AnalyzerVerifier<HashSetOfStringComparerAnalyzer>.VerifyAnalyzerAsync(source, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ToHashSet_OnStringEnumerable_NoComparer_Reports()
    {
        var source =
            """
            using System.Collections.Generic;
            using System.Linq;

            namespace App
            {
                public sealed class C
                {
                    public void M(IEnumerable<string> items)
                    {
                        var s = items.{|#0:ToHashSet|}();
                    }
                }
            }
            """;

        var expected = AnalyzerVerifier<HashSetOfStringComparerAnalyzer>
            .Diagnostic(DiagnosticDescriptors.HashSetOfStringMustUseOrdinalIgnoreCase)
            .WithLocation(0)
            .WithArguments("ToHashSet() on IEnumerable<string>");

        await AnalyzerVerifier<HashSetOfStringComparerAnalyzer>.VerifyAnalyzerAsync(source, expected, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ToHashSet_OnStringEnumerable_WithOrdinalIgnoreCase_NoDiagnostic()
    {
        var source =
            """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            namespace App
            {
                public sealed class C
                {
                    public void M(IEnumerable<string> items)
                    {
                        var s = items.ToHashSet(StringComparer.OrdinalIgnoreCase);
                    }
                }
            }
            """;

        await AnalyzerVerifier<HashSetOfStringComparerAnalyzer>.VerifyAnalyzerAsync(source, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ToHashSet_OnStringEnumerable_WithOrdinal_Reports()
    {
        var source =
            """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            namespace App
            {
                public sealed class C
                {
                    public void M(IEnumerable<string> items)
                    {
                        var s = items.{|#0:ToHashSet|}(StringComparer.Ordinal);
                    }
                }
            }
            """;

        var expected = AnalyzerVerifier<HashSetOfStringComparerAnalyzer>
            .Diagnostic(DiagnosticDescriptors.HashSetOfStringMustUseOrdinalIgnoreCase)
            .WithLocation(0)
            .WithArguments("ToHashSet() on IEnumerable<string>");

        await AnalyzerVerifier<HashSetOfStringComparerAnalyzer>.VerifyAnalyzerAsync(source, expected, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ToHashSet_OnNonStringEnumerable_DoesNotReport()
    {
        var source =
            """
            using System.Collections.Generic;
            using System.Linq;

            namespace App
            {
                public sealed class C
                {
                    public void M(IEnumerable<int> items)
                    {
                        var s = items.ToHashSet();
                    }
                }
            }
            """;

        await AnalyzerVerifier<HashSetOfStringComparerAnalyzer>.VerifyAnalyzerAsync(source, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ToHashSet_OnTaskEnumerable_DoesNotReport()
    {
        // Real-world example from TaskExtensions: ToHashSet on IEnumerable<Task>
        // must NOT trigger because the resulting HashSet is HashSet<Task>, not
        // HashSet<string>.
        var source =
            """
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading.Tasks;

            namespace App
            {
                public sealed class C
                {
                    public void M(IEnumerable<Task> items)
                    {
                        var s = items.ToHashSet();
                    }
                }
            }
            """;

        await AnalyzerVerifier<HashSetOfStringComparerAnalyzer>.VerifyAnalyzerAsync(source, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ToHashSet_OnProjectionThatYieldsString_Reports()
    {
        // The whole point: even when ToHashSet sits at the end of a LINQ chain
        // that projects to strings, the result is HashSet<string> and needs the
        // comparer.
        var source =
            """
            using System.Collections.Generic;
            using System.Linq;

            namespace App
            {
                public sealed class Item { public string Name { get; set; } = ""; }

                public sealed class C
                {
                    public void M(IEnumerable<Item> items)
                    {
                        var s = items.Select(x => x.Name).{|#0:ToHashSet|}();
                    }
                }
            }
            """;

        var expected = AnalyzerVerifier<HashSetOfStringComparerAnalyzer>
            .Diagnostic(DiagnosticDescriptors.HashSetOfStringMustUseOrdinalIgnoreCase)
            .WithLocation(0)
            .WithArguments("ToHashSet() on IEnumerable<string>");

        await AnalyzerVerifier<HashSetOfStringComparerAnalyzer>.VerifyAnalyzerAsync(source, expected, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task PragmaWarningDisable_Suppresses()
    {
        // Verifies the documented suppression path works.
        var source =
            """
            using System;
            using System.Collections.Generic;

            namespace App
            {
                public sealed class C
                {
                    public void M()
                    {
                        // Case-sensitive deliberately: C# identifier names.
            #pragma warning disable NLF0016
                        var s = new HashSet<string>(StringComparer.Ordinal);
            #pragma warning restore NLF0016
                    }
                }
            }
            """;

        await AnalyzerVerifier<HashSetOfStringComparerAnalyzer>.VerifyAnalyzerAsync(source, TestContext.Current.CancellationToken);
    }
}
