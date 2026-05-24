using System;
using System.Diagnostics;
using System.Threading.Tasks;

using NexusLabs.Framework.Diagnostics.Tracing;

using Xunit;

namespace NexusLabs.Framework.Tests.Diagnostics.Tracing;

public sealed class TracerTests
{
    [Fact]
    public void Ctor_NullActivitySource_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new Tracer(null!));
    }

    [Fact]
    public void WithTracing_Sync_NullAction_Throws()
    {
        using var source = new ActivitySource(NewSourceName());
        var sut = new Tracer(source);

        Assert.Throws<ArgumentNullException>(
            () => sut.WithTracing((Action)null!, "op"));
    }

    [Fact]
    public void WithTracingT_Sync_NullFunc_Throws()
    {
        using var source = new ActivitySource(NewSourceName());
        var sut = new Tracer(source);

        Assert.Throws<ArgumentNullException>(
            () => sut.WithTracing<int>((Func<int>)null!, "op"));
    }

    [Fact]
    public async Task WithTracingAsync_NullFunc_Throws()
    {
        using var source = new ActivitySource(NewSourceName());
        var sut = new Tracer(source);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.WithTracingAsync((Func<Task>)null!, "op"));
    }

    [Fact]
    public async Task WithTracingAsyncT_NullFunc_Throws()
    {
        using var source = new ActivitySource(NewSourceName());
        var sut = new Tracer(source);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.WithTracingAsync<int>((Func<Task<int>>)null!, "op"));
    }

    [Fact]
    public void WithTracing_NoOperationNameAndNoCallerName_Throws()
    {
        using var source = new ActivitySource(NewSourceName());
        var sut = new Tracer(source);

        var ex = Assert.Throws<ArgumentException>(() =>
            sut.WithTracing(() => { }, operationName: null, caller: null));
        Assert.Equal("operationName", ex.ParamName);
    }

    [Fact]
    public void WithTracing_Sync_RunsAction()
    {
        using var source = new ActivitySource(NewSourceName());
        var sut = new Tracer(source);
        var ran = false;

        sut.WithTracing(() => ran = true, "op");

        Assert.True(ran);
    }

    [Fact]
    public void WithTracingT_Sync_ReturnsValue()
    {
        using var source = new ActivitySource(NewSourceName());
        var sut = new Tracer(source);

        var result = sut.WithTracing(() => 42, "op");

        Assert.Equal(42, result);
    }

    [Fact]
    public async Task WithTracingAsync_RunsFunc()
    {
        using var source = new ActivitySource(NewSourceName());
        var sut = new Tracer(source);
        var ran = false;

        await sut.WithTracingAsync(() => { ran = true; return Task.CompletedTask; }, "op");

        Assert.True(ran);
    }

    [Fact]
    public async Task WithTracingAsyncT_ReturnsValue()
    {
        using var source = new ActivitySource(NewSourceName());
        var sut = new Tracer(source);

        var result = await sut.WithTracingAsync(() => Task.FromResult(99), "op");

        Assert.Equal(99, result);
    }

    [Fact]
    public void WithTracing_Sync_OperationName_StartsActivityWithThatName()
    {
        var sourceName = NewSourceName();
        using var source = new ActivitySource(sourceName);
        using var capture = new ActivityCapture(sourceName);
        var sut = new Tracer(source);

        sut.WithTracing(() => { }, "explicit-name");

        var started = capture.Started.ToArray();
        Assert.Single(started);
        Assert.Equal("explicit-name", started[0].OperationName);
    }

    [Fact]
    public void WithTracing_Sync_DefaultsToCallerMemberName()
    {
        var sourceName = NewSourceName();
        using var source = new ActivitySource(sourceName);
        using var capture = new ActivityCapture(sourceName);
        var sut = new Tracer(source);

        sut.WithTracing(() => { });

        var started = capture.Started.ToArray();
        Assert.Single(started);
        Assert.Equal(nameof(WithTracing_Sync_DefaultsToCallerMemberName), started[0].OperationName);
    }

    [Fact]
    public async Task WithTracingAsync_StartsActivityAroundFunc()
    {
        var sourceName = NewSourceName();
        using var source = new ActivitySource(sourceName);
        using var capture = new ActivityCapture(sourceName);
        var sut = new Tracer(source);

        Activity? insideFunc = null;
        await sut.WithTracingAsync(() =>
        {
            insideFunc = Activity.Current;
            return Task.CompletedTask;
        }, "async-op");

        var started = capture.Started.ToArray();
        Assert.Single(started);
        Assert.Equal("async-op", started[0].OperationName);
        Assert.Same(started[0], insideFunc);
        Assert.Single(capture.Stopped);
    }

    [Fact]
    public async Task WithTracingAsync_WhenFuncThrows_ExceptionPropagates_AndActivityStillStops()
    {
        var sourceName = NewSourceName();
        using var source = new ActivitySource(sourceName);
        using var capture = new ActivityCapture(sourceName);
        var sut = new Tracer(source);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.WithTracingAsync(
                () => Task.FromException(new InvalidOperationException("boom")),
                "async-op"));

        Assert.Single(capture.Stopped);
    }

    [Fact]
    public void WithTracing_Sync_NoListenerAttached_FuncStillRuns()
    {
        using var source = new ActivitySource(NewSourceName());
        var sut = new Tracer(source);
        var ran = false;

        sut.WithTracing(() => ran = true, "op");

        Assert.True(ran);
    }

    private static string NewSourceName() =>
        "NexusLabs.Framework.Tests.Tracer." + Guid.NewGuid().ToString("N");
}

[CollectionDefinition("Tracer.Default static state", DisableParallelization = true)]
public sealed class TracerDefaultCollection { }

/// <summary>
/// Tests for the <see cref="Tracer.Default"/> static surface. Serialized via the collection
/// definition above because they mutate process-wide static state. Each test snapshots
/// <see cref="Tracer.Default"/> on entry and restores it via <see cref="Tracer.SetDefault"/>
/// on exit so cross-test interference cannot cause flakes.
/// </summary>
[Collection("Tracer.Default static state")]
public sealed class TracerDefaultTests : IDisposable
{
    private readonly Tracer _originalDefault;

    public TracerDefaultTests()
    {
        _originalDefault = Tracer.Default;
    }

    public void Dispose() => Tracer.SetDefault(_originalDefault);

    [Fact]
    public void Default_IsNonNull()
    {
        Assert.NotNull(Tracer.Default);
    }

    [Fact]
    public void Default_ReturnsSameInstanceAcrossCalls_BetweenSwaps()
    {
        var a = Tracer.Default;
        var b = Tracer.Default;

        Assert.Same(a, b);
    }

    [Fact]
    public void SetDefaultSourceName_SwapsTheUnderlyingSource()
    {
        var sourceName = "NexusLabs.Framework.Tests.Tracer.Default." + Guid.NewGuid().ToString("N");
        using var capture = new ActivityCapture(sourceName);

        Tracer.SetDefaultSourceName(sourceName);
        Tracer.Default.WithTracing(() => { }, "op-after-rename");

        Assert.Contains(capture.Started, a => a.OperationName == "op-after-rename");
    }

    [Fact]
    public void SetDefaultSourceName_AfterSwap_PreviousDefaultStillUsable()
    {
        var nameA = "NexusLabs.Framework.Tests.Tracer.Default." + Guid.NewGuid().ToString("N");
        var nameB = "NexusLabs.Framework.Tests.Tracer.Default." + Guid.NewGuid().ToString("N");

        Tracer.SetDefaultSourceName(nameA);
        var firstDefault = Tracer.Default;

        Tracer.SetDefaultSourceName(nameB);

        Assert.NotSame(firstDefault, Tracer.Default);
        firstDefault.WithTracing(() => { }, "op");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SetDefaultSourceName_NullOrWhitespace_Throws(string? sourceName)
    {
        Assert.ThrowsAny<ArgumentException>(() => Tracer.SetDefaultSourceName(sourceName!));
    }

    [Fact]
    public void SetDefault_Null_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => Tracer.SetDefault(null!));
    }

    [Fact]
    public void SetDefault_ReplacesTheInstance()
    {
        using var source = new ActivitySource(
            "NexusLabs.Framework.Tests.Tracer.Default." + Guid.NewGuid().ToString("N"));
        var replacement = new Tracer(source);

        Tracer.SetDefault(replacement);

        Assert.Same(replacement, Tracer.Default);
    }
}
