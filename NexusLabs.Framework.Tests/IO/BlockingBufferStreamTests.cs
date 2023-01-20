using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NexusLabs.IO;

using Xunit;

namespace NexusLabs.Tests.IO
{
    public sealed class BlockingBufferStreamTests
    {
        [Fact]
        private void Read_ExactAvailableByteCount_ExpectedResults()
        {
            var stream = new BlockingBufferStream(2);
            stream.Write(new byte[] { 1, 2 }, 0, 2);

            var resultBytes = new byte[2];
            var resultRead = stream.Read(resultBytes, 0, resultBytes.Length);

            Assert.Equal(2, resultRead);
            Assert.Equal(new byte[] { 1, 2 }, resultBytes);
        }

        [Fact]
        private void Read_ReadCapacityAfterPreviousRead_ExpectedResults()
        {
            var stream = new BlockingBufferStream(4);
            stream.Write(new byte[] { 1, 2 }, 0, 2);

            var resultBytes = new byte[2];
            var resultRead = stream.Read(resultBytes, 0, resultBytes.Length);

            stream.Write(new byte[] { 6, 7, 8, 9 }, 0, 4);

            resultBytes = new byte[4];
            resultRead = stream.Read(resultBytes, 0, resultBytes.Length);

            Assert.Equal(4, resultRead);
            Assert.Equal(new byte[] { 6, 7, 8, 9 }, resultBytes);
        }

        [InlineData(100, 10, 100, 100)]
        [InlineData(100, 10, 10, 100)]
        [InlineData(10, 10, 100, 100)]
        [InlineData(10, 10, 10, 100)]
        [InlineData(1, 10, 100, 100)]
        [InlineData(1, 10, 10, 100)]
        [Theory]
        private async Task Functional_ReadAndWrite_VariousSizes(
            int bufferSize, 
            int readSize,
            int writeSize,
            int dataSetSize)
        {
            var inputBytes = new byte[dataSetSize];
            for (var i = 0; i < inputBytes.Length; i++)
            {
                inputBytes[i] = (byte)(i % byte.MaxValue);
            }

            using var stream = new BlockingBufferStream(bufferSize);

            var resultBytes = new byte[readSize];
            var readTask = Task.Run(() => 
            {
                int offset = 0;
                int read;
                while ((read = stream.Read(resultBytes, offset, resultBytes.Length - offset)) >= 0)
                {
                    offset += read;
                    if (offset >= readSize)
                    {
                        break;
                    }
                }

                stream.Close();
                return offset;
            });

            var allowWriteThrow = writeSize > readSize;
            Exception writeException = null;
            var writeTask = Task.Run(() =>
            {
                try
                {
                    stream.Write(inputBytes, 0, writeSize);
                }
                catch (InvalidOperationException ex)
                {
                    if (!allowWriteThrow)
                    {
                        throw;
                    }

                    writeException = ex;
                }
            });

            await Task.WhenAll(writeTask, readTask);

            Assert.Equal(readSize, readTask.Result);
            Assert.Equal(inputBytes.Take(readSize), resultBytes);

            var expectWriteThrow = allowWriteThrow && writeSize != dataSetSize;
            if (expectWriteThrow)
            {
                Assert.NotNull(writeException);
            }
            else if (!allowWriteThrow)
            {
                Assert.Null(writeException);
            }
        }
    }
}
