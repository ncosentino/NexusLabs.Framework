using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace NexusLabs.Framework.Diagnostics.Tracing;

/// <summary>
/// Default <see cref="ITracer"/> implementation backed by an externally-owned
/// <see cref="ActivitySource"/>. The caller owns the source's lifecycle and is responsible
/// for disposing it.
/// </summary>
/// <remarks>
/// A process-wide convenience instance is available via <see cref="Default"/>. By default
/// it traces against an <see cref="ActivitySource"/> named <c>"NexusLabs"</c>; consumers
/// typically reconfigure it at startup via <see cref="SetDefaultSourceName"/> so trace
/// data shows up under their own application name in observability tools.
/// </remarks>
public sealed class Tracer : ITracer
{
    private const string FallbackSourceName = "NexusLabs";

    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP008:Don't assign member with injected and created disposables",
        Justification = "_default is a process-lifetime singleton holding the fallback ActivitySource. " +
                        "SetDefault/SetDefaultSourceName replace it with either an injected or freshly-created " +
                        "instance; the previously-held source is intentionally not disposed (documented on Default).")]
    private static Tracer _default = new(new ActivitySource(FallbackSourceName));

    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP008:Don't assign member with injected and created disposables",
        Justification = "_activitySource always holds a caller-supplied ActivitySource; the only " +
                        "created-disposable path is the static _default initializer, which is itself " +
                        "a process-lifetime singleton. The class does not implement IDisposable because " +
                        "it never owns the source's lifecycle.")]
    private readonly ActivitySource _activitySource;

    /// <summary>Creates a tracer that starts activities on <paramref name="activitySource"/>.</summary>
    /// <exception cref="ArgumentNullException">If <paramref name="activitySource"/> is null.</exception>
    public Tracer(ActivitySource activitySource)
    {
        ArgumentNullException.ThrowIfNull(activitySource);
        _activitySource = activitySource;
    }

    /// <summary>
    /// Process-wide convenience tracer. Initially backed by an <see cref="ActivitySource"/>
    /// named <c>"NexusLabs"</c>; replace via <see cref="SetDefaultSourceName"/> (most common)
    /// or <see cref="SetDefault"/> (full substitution).
    /// </summary>
    /// <remarks>
    /// Reads are lock-free via <see cref="Volatile.Read{T}(ref T)"/>. Swaps replace the
    /// reference; previously-handed-out instances remain usable until callers drop their
    /// references. The underlying <see cref="ActivitySource"/> from a replaced tracer is
    /// not disposed by this class — for a startup-only swap this is a process-lifetime
    /// singleton anyway.
    /// </remarks>
    public static Tracer Default => Volatile.Read(ref _default);

    /// <summary>
    /// Replaces <see cref="Default"/> with a tracer backed by a freshly-created
    /// <see cref="ActivitySource"/> named <paramref name="sourceName"/>. Convenience wrapper
    /// around the common "set the name" case.
    /// </summary>
    /// <exception cref="ArgumentException">If <paramref name="sourceName"/> is null or whitespace.</exception>
    public static void SetDefaultSourceName(string sourceName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceName);
        SetDefault(new Tracer(new ActivitySource(sourceName)));
    }

    /// <summary>
    /// Replaces <see cref="Default"/> with the supplied tracer. Use when you need to fully
    /// substitute the <see cref="ActivitySource"/> (for example to pass a pre-configured
    /// source with version/tags) or to install a custom <see cref="ITracer"/>-shaped wrapper.
    /// </summary>
    /// <exception cref="ArgumentNullException">If <paramref name="tracer"/> is null.</exception>
    public static void SetDefault(Tracer tracer)
    {
        ArgumentNullException.ThrowIfNull(tracer);
        Volatile.Write(ref _default, tracer);
    }

    /// <inheritdoc />
    public async Task WithTracingAsync(
        Func<Task> func,
        string? operationName = null,
        [CallerMemberName] string? caller = null)
    {
        ArgumentNullException.ThrowIfNull(func);
        using var activity = StartActivity(operationName, caller);
        await func().ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<T> WithTracingAsync<T>(
        Func<Task<T>> func,
        string? operationName = null,
        [CallerMemberName] string? caller = null)
    {
        ArgumentNullException.ThrowIfNull(func);
        using var activity = StartActivity(operationName, caller);
        return await func().ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void WithTracing(
        Action action,
        string? operationName = null,
        [CallerMemberName] string? caller = null)
    {
        ArgumentNullException.ThrowIfNull(action);
        using var activity = StartActivity(operationName, caller);
        action();
    }

    /// <inheritdoc />
    public T WithTracing<T>(
        Func<T> func,
        string? operationName = null,
        [CallerMemberName] string? caller = null)
    {
        ArgumentNullException.ThrowIfNull(func);
        using var activity = StartActivity(operationName, caller);
        return func();
    }

    private Activity? StartActivity(string? operationName, string? caller)
    {
        var name = operationName ?? caller;
        if (string.IsNullOrEmpty(name))
        {
            throw new ArgumentException(
                "Either operationName must be supplied or the call must be invocable with " +
                "[CallerMemberName] (i.e. from a named method). Neither was available.",
                nameof(operationName));
        }

        return _activitySource.StartActivity(name);
    }
}
