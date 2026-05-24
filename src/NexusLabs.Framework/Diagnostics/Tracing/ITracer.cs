using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace NexusLabs.Framework.Diagnostics.Tracing;

/// <summary>
/// Convenience facade over <see cref="System.Diagnostics.ActivitySource"/> for starting a
/// scoped trace activity around a synchronous or asynchronous callback. The activity is
/// disposed when the callback returns.
/// </summary>
/// <remarks>
/// <para>
/// The operation name defaults to <see cref="CallerMemberNameAttribute"/>; pass
/// <paramref name="operationName"/> explicitly to override. If neither is provided an
/// <see cref="ArgumentException"/> is thrown.
/// </para>
/// <para>
/// When no <see cref="System.Diagnostics.ActivityListener"/> is subscribed to the underlying
/// <see cref="System.Diagnostics.ActivitySource"/>, <c>StartActivity</c> returns null and the
/// callback still runs — the trace is simply not recorded. This is the standard
/// <c>ActivitySource</c> contract.
/// </para>
/// <para>
/// Exists primarily so consumers can substitute a no-op implementation in tests via DI /
/// mocking. Code that does not need substitutability can use <see cref="System.Diagnostics.ActivitySource"/>
/// directly.
/// </para>
/// </remarks>
public interface ITracer
{
    /// <summary>Executes an asynchronous operation inside a scoped trace activity.</summary>
    Task WithTracingAsync(
        Func<Task> func,
        string? operationName = null,
        [CallerMemberName] string? caller = null);

    /// <summary>Executes an asynchronous operation inside a scoped trace activity and returns its result.</summary>
    Task<T> WithTracingAsync<T>(
        Func<Task<T>> func,
        string? operationName = null,
        [CallerMemberName] string? caller = null);

    /// <summary>Executes a synchronous operation inside a scoped trace activity.</summary>
    void WithTracing(
        Action action,
        string? operationName = null,
        [CallerMemberName] string? caller = null);

    /// <summary>Executes a synchronous operation inside a scoped trace activity and returns its result.</summary>
    T WithTracing<T>(
        Func<T> func,
        string? operationName = null,
        [CallerMemberName] string? caller = null);
}
