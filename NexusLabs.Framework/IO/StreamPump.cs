using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NexusLabs.IO
{
    public sealed class StreamPump
    {
        public async Task PumpAsync(
            Stream input,
            Stream output,
            int bufferSize,
            CancellationToken cancellationToken)
        {
            if (!input.CanSeek)
            {
                throw new ArgumentException(
                    $"The input stream must be seekable.");
            }

            await PumpAsync(
                input,
                output,
                bufferSize,
                () => input.Position >= input.Length,
                cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task PumpStandardOutputAsync(
            Process process,
            Stream output,
            int bufferSize,
            CancellationToken cancellationToken)
        {
            await PumpAsync(
                process.StandardOutput.BaseStream,
                output,
                bufferSize,
                () => process.HasExited,
                cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task PumpAsync(
            Stream input,
            Stream output,
            int bufferSize,
            Func<bool> shouldStopCallback,
            CancellationToken cancellationToken)
        {
            var buffer = new byte[bufferSize];
            int lastRead;

            while ((lastRead = await input.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) != -1 &&
                !shouldStopCallback() &&
                !cancellationToken.IsCancellationRequested)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                if (lastRead < 1)
                {
                    continue;
                }

                await output.WriteAsync(buffer, 0, lastRead, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
