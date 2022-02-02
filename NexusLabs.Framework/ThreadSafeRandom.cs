namespace NexusLabs.Framework
{
    public sealed class ThreadSafeRandom : IRandom
    {
        private readonly object _lock;
        private readonly IRandom _random;

        public ThreadSafeRandom(IRandom random)
        {
            _lock = new object();
            _random = random;
        }

        public double NextDouble(
            double minInclusive,
            double maxExclusive)
        {
            lock (_lock)
            {
                return _random.NextDouble(minInclusive, maxExclusive);
            }
        }

        public double NextDouble(
            double minInclusive, 
            double maxExclusive, 
            double step)
        {
            lock (_lock)
            {
                return _random.NextDouble(minInclusive, maxExclusive, step);
            }
        }

        public int Next(int minInclusive, int maxExclusive)
        {
            lock (_lock)
            {
                return _random.Next(minInclusive, maxExclusive);
            }
        }

        public int Next(int minInclusive, int maxExclusive, int step)
        {
            lock (_lock)
            {
                return _random.Next(minInclusive, maxExclusive, step);
            }
        }

        public long NextLong(
            long minInclusive,
            long maxExclusive)
        {
            lock (_lock)
            {
                return _random.NextLong(minInclusive, maxExclusive);
            }
        }

        public long NextLong(long minInclusive, long maxExclusive, long step)
        {
            lock (_lock)
            {
                return _random.NextLong(minInclusive, maxExclusive, step);
            }
        }
    }
}
