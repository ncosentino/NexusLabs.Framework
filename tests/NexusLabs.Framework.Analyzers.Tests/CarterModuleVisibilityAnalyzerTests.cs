using System.Threading.Tasks;

using Microsoft.CodeAnalysis;

using NexusLabs.Framework.Analyzers;

using Xunit;

namespace NexusLabs.Framework.Analyzers.Tests;

public sealed class CarterModuleVisibilityAnalyzerTests
{
    [Fact]
    public async Task PublicSealedClass_NoDiagnostic()
    {
        var source =
            CarterStub +
            """

            namespace App
            {
                using Carter;

                public sealed class MyCarterModule : ICarterModule
                {
                    public void AddRoutes() { }
                }
            }
            """;

        await AnalyzerVerifier<CarterModuleVisibilityAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task PublicSealedPartialClass_NoDiagnostic()
    {
        var source =
            CarterStub +
            """

            namespace App
            {
                using Carter;

                public sealed partial class MyCarterModule : ICarterModule
                {
                    public void AddRoutes() { }
                }

                public sealed partial class MyCarterModule
                {
                    public void Helper() { }
                }
            }
            """;

        await AnalyzerVerifier<CarterModuleVisibilityAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task InternalSealedClass_Reports()
    {
        var source =
            CarterStub +
            """

            namespace App
            {
                using Carter;

                internal sealed class {|#0:MyCarterModule|} : ICarterModule
                {
                    public void AddRoutes() { }
                }
            }
            """;

        var expected = AnalyzerVerifier<CarterModuleVisibilityAnalyzer>
            .Diagnostic(DiagnosticDescriptors.CarterModuleMustBePublicSealedClass)
            .WithLocation(0)
            .WithArguments("MyCarterModule", "internal sealed class");

        await AnalyzerVerifier<CarterModuleVisibilityAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task PublicNonSealedClass_Reports()
    {
        var source =
            CarterStub +
            """

            namespace App
            {
                using Carter;

                public class {|#0:MyCarterModule|} : ICarterModule
                {
                    public void AddRoutes() { }
                }
            }
            """;

        var expected = AnalyzerVerifier<CarterModuleVisibilityAnalyzer>
            .Diagnostic(DiagnosticDescriptors.CarterModuleMustBePublicSealedClass)
            .WithLocation(0)
            .WithArguments("MyCarterModule", "public class");

        await AnalyzerVerifier<CarterModuleVisibilityAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task InternalNonSealedClass_Reports()
    {
        var source =
            CarterStub +
            """

            namespace App
            {
                using Carter;

                internal class {|#0:MyCarterModule|} : ICarterModule
                {
                    public void AddRoutes() { }
                }
            }
            """;

        var expected = AnalyzerVerifier<CarterModuleVisibilityAnalyzer>
            .Diagnostic(DiagnosticDescriptors.CarterModuleMustBePublicSealedClass)
            .WithLocation(0)
            .WithArguments("MyCarterModule", "internal class");

        await AnalyzerVerifier<CarterModuleVisibilityAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task PublicAbstractClass_Reports()
    {
        var source =
            CarterStub +
            """

            namespace App
            {
                using Carter;

                public abstract class {|#0:MyCarterModule|} : ICarterModule
                {
                    public abstract void AddRoutes();
                }
            }
            """;

        var expected = AnalyzerVerifier<CarterModuleVisibilityAnalyzer>
            .Diagnostic(DiagnosticDescriptors.CarterModuleMustBePublicSealedClass)
            .WithLocation(0)
            .WithArguments("MyCarterModule", "public abstract class");

        await AnalyzerVerifier<CarterModuleVisibilityAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task IndirectImplementationViaBase_FlagsBothBaseAndDerived()
    {
        // Base is public+abstract+not-sealed → flagged (abstract class).
        // Derived is internal+sealed → flagged (internal sealed class).
        // The rule fires once per declaration that fails the public-sealed contract.
        var source =
            CarterStub +
            """

            namespace App
            {
                using Carter;

                public abstract class {|#0:CarterModuleBase|} : ICarterModule
                {
                    public abstract void AddRoutes();
                }

                internal sealed class {|#1:ConcreteCarterModule|} : CarterModuleBase
                {
                    public override void AddRoutes() { }
                }
            }
            """;

        var baseExpected = AnalyzerVerifier<CarterModuleVisibilityAnalyzer>
            .Diagnostic(DiagnosticDescriptors.CarterModuleMustBePublicSealedClass)
            .WithLocation(0)
            .WithArguments("CarterModuleBase", "public abstract class");

        var derivedExpected = AnalyzerVerifier<CarterModuleVisibilityAnalyzer>
            .Diagnostic(DiagnosticDescriptors.CarterModuleMustBePublicSealedClass)
            .WithLocation(1)
            .WithArguments("ConcreteCarterModule", "internal sealed class");

        await AnalyzerVerifier<CarterModuleVisibilityAnalyzer>.VerifyAnalyzerAsync(source, baseExpected, derivedExpected);
    }

    [Fact]
    public async Task NonCarterClass_NoDiagnostic()
    {
        var source =
            CarterStub +
            """

            namespace App
            {
                internal class OrdinaryClass
                {
                }

                internal sealed class OrdinarySealedClass
                {
                }

                public class PublicNonSealed
                {
                }
            }
            """;

        await AnalyzerVerifier<CarterModuleVisibilityAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task ClassNamedCarterModule_ButNoInterface_NoDiagnostic()
    {
        // The rule keys off the interface, NOT the name.
        var source =
            CarterStub +
            """

            namespace App
            {
                internal class NotARealCarterModule
                {
                }
            }
            """;

        await AnalyzerVerifier<CarterModuleVisibilityAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task SameNamedInterfaceInDifferentNamespace_NoDiagnostic()
    {
        // A type called ICarterModule that lives in a non-Carter namespace
        // must NOT trigger the rule.
        var source =
            """
            namespace NotCarter
            {
                public interface ICarterModule
                {
                    void AddRoutes();
                }
            }

            namespace App
            {
                using NotCarter;

                internal sealed class MyModule : ICarterModule
                {
                    public void AddRoutes() { }
                }
            }
            """;

        await AnalyzerVerifier<CarterModuleVisibilityAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Struct_DoesNotReport()
    {
        // Rule is class-only — structs cannot implement Carter.ICarterModule
        // in a way Carter consumes, and the diagnostic is targeted at class
        // declarations.
        var source =
            CarterStub +
            """

            namespace App
            {
                using Carter;

                public struct MyStruct : ICarterModule
                {
                    public void AddRoutes() { }
                }
            }
            """;

        await AnalyzerVerifier<CarterModuleVisibilityAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task PragmaWarningDisable_Suppresses()
    {
        // Documented suppression path for intentional public abstract base.
        var source =
            CarterStub +
            """

            namespace App
            {
                using Carter;

            #pragma warning disable NLF0017
                public abstract class CarterModuleBase : ICarterModule
                {
                    public abstract void AddRoutes();
                }
            #pragma warning restore NLF0017
            }
            """;

        await AnalyzerVerifier<CarterModuleVisibilityAnalyzer>.VerifyAnalyzerAsync(source);
    }

    /// <summary>
    /// Minimal <c>Carter.ICarterModule</c> stub. Prepended to every test source
    /// so the analyzer can resolve the gating interface symbol without the test
    /// project taking a dependency on the Carter NuGet package.
    /// </summary>
    private const string CarterStub =
        """
        namespace Carter
        {
            public interface ICarterModule
            {
                void AddRoutes();
            }
        }
        """;
}
