using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace NexusLabs.Framework.Diagnostics;

public static class ProcessExtensions
{
    public static Task WaitForExitAsync(
        this Process process,
        Action<Process> beforeWaitCallback,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(process);

        if (process.SafeCheckHasExited() == true)
        {
            return Task.CompletedTask;
        }

        var tcs = new TaskCompletionSource<object?>();

        process.EnableRaisingEvents = true;
        process.Exited += (sender, args) => tcs.TrySetResult(null);

        if (cancellationToken != default)
        {
            cancellationToken.Register(() => tcs.SetCanceled());
        }

        beforeWaitCallback?.Invoke(process);
        return process.SafeCheckHasExited() == true
            ? Task.CompletedTask
            : tcs.Task;
    }

    public static async Task StartAndWaitForExitAsync(
        this Process process,
        ProcessStartInfo processStartInfo,
        CancellationToken cancellationToken,
        Action<Process>? afterStartCallback = null)
    {
        ArgumentNullException.ThrowIfNull(process);

        await WaitForExitAsync(
            process,
            p =>
            {
                process.StartInfo = processStartInfo;
                process.Start();
                afterStartCallback?.Invoke(p);
            },
            cancellationToken);
    }

    public static bool? SafeCheckHasExited(this Process process)
    {
        ArgumentNullException.ThrowIfNull(process);

        try
        {
            return process.HasExited;
        }
        catch
        {
            return null;
        }
    }
}
