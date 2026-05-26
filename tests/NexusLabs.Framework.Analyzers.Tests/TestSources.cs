namespace NexusLabs.Framework.Analyzers.Tests;

/// <summary>
/// Reusable C# source snippets injected into analyzer test inputs. The analyzer harness
/// compiles the test source in isolation with only the configured reference assemblies,
/// so any types the test code touches (e.g. <c>TriedEx&lt;T&gt;</c>) have to be visible
/// in the compilation. Defining minimal stubs here keeps the tests self-contained and
/// avoids dragging the real <c>NexusLabs.Framework</c> assembly into the analyzer's
/// in-memory compilation.
/// </summary>
internal static class TestSources
{
    /// <summary>
    /// Minimal <c>TriedEx</c> / <c>TriedNullEx</c> stubs in the real
    /// <c>NexusLabs.Framework</c> namespace. The Try-pattern analyzers gate on this exact
    /// namespace + type name pair, so the stubs must keep both. Append to a test source
    /// (after a trailing newline) so the analyzer's namespace check matches.
    /// </summary>
    public const string TriedExStubs =
        """

        namespace NexusLabs.Framework
        {
            public readonly struct TriedEx<T>
            {
                public bool Success { get; init; }
                public T Value { get; init; }
                public System.Exception Error { get; init; }
            }

            public readonly struct TriedNullEx<T>
            {
                public bool Success { get; init; }
                public T Value { get; init; }
                public System.Exception Error { get; init; }
            }
        }
        """;

    /// <summary>
    /// Minimal <c>Try</c> orchestrator stub in <c>NexusLabs.Framework</c> namespace, plus
    /// matching <c>TriedEx</c>/<c>TriedNullEx</c> structs and a minimal <c>ILogger</c>
    /// interface. Mirrors the shape of the real <c>NexusLabs.Framework.Try</c> overloads:
    /// <c>Async</c> with and without logger, <c>GetAsync&lt;T&gt;</c> with and without
    /// logger, and <c>GetOrNullAsync&lt;T&gt;</c> with and without logger. The Try-pattern
    /// analyzer checks the type name + namespace match, so the stub must keep both.
    /// </summary>
    public const string TryHelperStubs =
        """

        namespace Microsoft.Extensions.Logging
        {
            public interface ILogger
            {
                void LogError(System.Exception exception, string message);
            }

            public interface ILogger<T> : ILogger
            {
            }
        }

        namespace NexusLabs.Framework
        {
            using System;
            using System.Threading.Tasks;
            using Microsoft.Extensions.Logging;

            public readonly struct TriedEx<T>
            {
                public T Value { get; }
                public Exception? Error { get; }
                public bool Success => Error == null;

                public TriedEx(T value) { Value = value; Error = null; }
                public TriedEx(Exception error) { Value = default!; Error = error; }

                public static implicit operator TriedEx<T>(T value) => new(value);
                public static implicit operator TriedEx<T>(Exception error) => new(error);
            }

            public readonly struct TriedNullEx<T>
            {
                public T Value { get; }
                public Exception? Error { get; }
                public bool Success => Error == null;

                public TriedNullEx(T value) { Value = value; Error = null; }
                public TriedNullEx(Exception error) { Value = default!; Error = error; }

                public static implicit operator TriedNullEx<T>(T value) => new(value);
                public static implicit operator TriedNullEx<T>(Exception error) => new(error);
            }

            public static class Try
            {
                public static async Task<Exception?> Async(Func<Task> callback)
                {
                    try { await callback(); return null; }
                    catch (Exception ex) { return ex; }
                }

                public static async Task<Exception?> Async(ILogger logger, Func<Task> callback)
                {
                    try { await callback(); return null; }
                    catch (Exception ex) { logger.LogError(ex, "Error"); return ex; }
                }

                public static async Task<Exception?> Async(ILogger logger, Func<Task<Exception?>> callback)
                {
                    try { return await callback(); }
                    catch (Exception ex) { logger.LogError(ex, "Error"); return ex; }
                }

                public static async Task<TriedEx<T>> GetAsync<T>(Func<Task<TriedEx<T>>> callback)
                {
                    try { return await callback(); }
                    catch (Exception ex) { return ex; }
                }

                public static async Task<TriedEx<T>> GetAsync<T>(ILogger logger, Func<Task<TriedEx<T>>> callback)
                {
                    try { return await callback(); }
                    catch (Exception ex) { logger.LogError(ex, "Error"); return ex; }
                }

                public static async Task<TriedNullEx<T>> GetOrNullAsync<T>(Func<Task<TriedNullEx<T>>> callback)
                {
                    try { return await callback(); }
                    catch (Exception ex) { return ex; }
                }

                public static async Task<TriedNullEx<T>> GetOrNullAsync<T>(ILogger logger, Func<Task<TriedNullEx<T>>> callback)
                {
                    try { return await callback(); }
                    catch (Exception ex) { logger.LogError(ex, "Error"); return ex; }
                }
            }

            public class Tracer
            {
                public static Tracer Default { get; } = new Tracer();

                public async Task<T> WithTracingAsync<T>(Func<Task<T>> callback) => await callback();
            }
        }

        namespace System.Data
        {
            public interface IDbConnection
            {
                IDbTransaction BeginTransaction();
            }

            public interface IDbTransaction
            {
                void Commit();
                void Rollback();
            }
        }
        """;
}
