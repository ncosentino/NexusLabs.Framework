using System.Threading;
using System.Threading.Tasks;

using NexusLabs.Contracts;

namespace System.Diagnostics
{
    public static class ProcessExtensions
    {
        public static Task WaitForExitAsync(
            this Process process,
            Action<Process> beforeWaitCallback,
            CancellationToken cancellationToken = default)
        {
            ArgumentContract.RequiresNotNull(process, nameof(process));

            if (process.SafeCheckHasExited() == true)
            {
                return Task.CompletedTask;
            }

            var tcs = new TaskCompletionSource<object>();

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
            Action<Process> afterStartCallback = default,
            CancellationToken cancellationToken = default)
        {
            ArgumentContract.RequiresNotNull(process, nameof(process));

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
            ArgumentContract.RequiresNotNull(process, nameof(process));

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
}
