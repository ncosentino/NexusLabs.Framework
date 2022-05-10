using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace NexusLabs.IO
{
    public sealed class BlockingBufferStream : Stream
    {
        private readonly object _writeSyncRoot;
        private readonly object _readSyncRoot;
        private readonly LinkedList<ArraySegment<byte>> _pendingSegments;
        private readonly ManualResetEventSlim _dataAvailableResetEvent;
        private readonly ManualResetEventSlim _capacityAvailableResetEvent;
        private readonly CancellationTokenSource _closeCancellationTokenSource;

        private int _readTimeout;
        private int _writeTimeout;
        private int _currentCapacity;

        public BlockingBufferStream(int capacity)
        {
            _writeSyncRoot = new object();
            _readSyncRoot = new object();
            _pendingSegments = new LinkedList<ArraySegment<byte>>();
            _dataAvailableResetEvent = new ManualResetEventSlim();
            _capacityAvailableResetEvent = new ManualResetEventSlim(true);
            _closeCancellationTokenSource = new CancellationTokenSource();

            ReadTimeout = -1;
            WriteTimeout = -1;
            Capacity = capacity;
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }
        public int Capacity { get; }

        public override bool CanTimeout => true;

        public override int ReadTimeout { get => _readTimeout; set => _readTimeout = value; }

        public override int WriteTimeout { get => _writeTimeout; set => _writeTimeout = value; }

        public override void Flush()
        {
        }

        public override void Close()
        {
            _closeCancellationTokenSource.Cancel();
            base.Close();
        }

        public override int Read(
            byte[] buffer,
            int offset,
            int count)
        {
            try
            {
                if (!_dataAvailableResetEvent.Wait(ReadTimeout, _closeCancellationTokenSource.Token))
                {
                    throw new TimeoutException("No data available.");
                }
            }
            catch (OperationCanceledException)
            {
                return -1;
            }

            lock (_readSyncRoot)
            {
                int currentCount = 0;
                try
                {
                    var currentOffset = offset;
                    while (currentCount != count)
                    {
                        lock (_pendingSegments)
                        {
                            var segment = _pendingSegments.First.Value;
                            _pendingSegments.RemoveFirst();

                            var currentSegmentLengthToCopy = Math.Min(segment.Count, count - currentCount);

                            Array.Copy(segment.Array, segment.Offset, buffer, currentOffset, currentSegmentLengthToCopy);
                            currentOffset += currentSegmentLengthToCopy;

                            if (currentSegmentLengthToCopy < segment.Count)
                            {
                                _pendingSegments.AddFirst(new ArraySegment<byte>(segment.Array, segment.Offset + currentSegmentLengthToCopy, segment.Count - currentSegmentLengthToCopy));
                            }

                            currentCount += currentSegmentLengthToCopy;

                            if (_pendingSegments.Count == 0)
                            {
                                _dataAvailableResetEvent.Reset();
                                return currentCount;
                            }
                        }
                    }

                    return currentCount;
                }
                finally
                {
                    _currentCapacity -= currentCount;

                    if (_currentCapacity < Capacity)
                    {
                        _capacityAvailableResetEvent.Set();
                    }
                }
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            lock (_writeSyncRoot)
            {
                var remainingToWrite = count;

                while (remainingToWrite > 0)
                {
                    try
                    {
                        if (!_capacityAvailableResetEvent.Wait(WriteTimeout, _closeCancellationTokenSource.Token))
                        {
                            throw new TimeoutException("No capacity available.");
                        }
                    }
                    catch (OperationCanceledException ex)
                    {
                        throw new InvalidOperationException("The stream has been closed.", ex);
                    }

                    var currentWriteCount = Math.Min(remainingToWrite, Capacity - _currentCapacity);
                    remainingToWrite -= currentWriteCount;
                    _currentCapacity += currentWriteCount;
                    if (_currentCapacity == Capacity)
                    {
                        _capacityAvailableResetEvent.Reset();
                    }

                    byte[] copy = new byte[currentWriteCount];
                    Array.Copy(buffer, offset, copy, 0, currentWriteCount);
                    offset += currentWriteCount;

                    lock (_pendingSegments)
                    {
                        _pendingSegments.AddLast(new ArraySegment<byte>(copy));
                    }

                    _dataAvailableResetEvent.Set();
                }
            }
        }
    }
}
