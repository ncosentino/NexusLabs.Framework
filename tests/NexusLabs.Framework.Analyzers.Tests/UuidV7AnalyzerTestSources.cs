namespace NexusLabs.Framework.Analyzers.Tests;

/// <summary>
/// Source snippets for UUIDv7 identifier analyzer and code-fix tests.
/// </summary>
internal static class UuidV7AnalyzerTestSources
{
    public const string IdentifierStubs =
        """

        namespace NexusLabs.StronglyTypedIds
        {
            public interface IUuidV7Identifier<TSelf>
                where TSelf : struct, IUuidV7Identifier<TSelf>
            {
            }
        }

        namespace App
        {
            [System.CodeDom.Compiler.GeneratedCode("StronglyTypedId", "1.0")]
            public readonly struct OrderId :
                NexusLabs.StronglyTypedIds.IUuidV7Identifier<OrderId>
            {
                public OrderId(System.Guid value)
                {
                    Value = value;
                }

                public System.Guid Value { get; }

                public static OrderId New() => default;

                public static OrderId New<T>() => default;

                public static OrderId Create() => default;
            }

            public readonly struct OtherId
            {
                public OtherId(System.Guid value)
                {
                    Value = value;
                }

                public System.Guid Value { get; }

                public static OtherId New() => default;
            }

            public readonly struct ManualUuidV7Id :
                NexusLabs.StronglyTypedIds.IUuidV7Identifier<ManualUuidV7Id>
            {
                public static ManualUuidV7Id New() => default;

                public static ManualUuidV7Id Create() => default;
            }
        }
        """;
}
