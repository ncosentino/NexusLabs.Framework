namespace NexusLabs.Framework.Analyzers.Tests;

/// <summary>
/// Minimal in-source surface of the Moq API, prepended to analyzer test sources
/// so the Moq analyzers (which match <c>Moq.*</c> types by namespace + name)
/// have something to resolve against without the test compilation referencing
/// the real Moq package. The stub deliberately contains no <c>new Mock&lt;T&gt;()</c>
/// of its own so it never trips the analyzers under test.
/// </summary>
internal static class MoqTestSource
{
    public const string Stub =
        """
        namespace Moq
        {
            public enum MockBehavior { Default, Strict, Loose }

            public class Mock<T> where T : class
            {
                public Mock() { }
                public Mock(MockBehavior behavior) { }
                public Mock(params object[] args) { }
                public T Object { get { return default!; } }
            }

            public static class Mock
            {
                public static T Of<T>() where T : class { return default!; }
                public static T Of<T>(MockBehavior behavior) where T : class { return default!; }
            }

            public class MockRepository
            {
                public MockRepository(MockBehavior defaultBehavior) { }
                public Mock<T> Create<T>() where T : class { return default!; }
                public Mock<T> Create<T>(MockBehavior behavior) where T : class { return default!; }
            }

            public static class It
            {
                public static T IsAny<T>() { return default!; }
                public static T Is<T>(System.Linq.Expressions.Expression<System.Func<T, bool>> match) { return default!; }
            }
        }
        """;

    public static string Wrap(string code) => code + "\n\n" + Stub;
}
