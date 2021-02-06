namespace NexusLabs.Framework
{
    public sealed class Random : IRandom
    {
        private readonly System.Random _random;

        public Random(System.Random random)
        {
            _random = random;
        }

        public double NextDouble(double minInclusive, double maxExclusive)
        {
            return minInclusive + _random.NextDouble() * (maxExclusive - minInclusive);
        }

        public double NextDouble(double minInclusive, double maxExclusive, double step)
        {
            var possibleSteps = (int)((maxExclusive - minInclusive) / step);
            var result = minInclusive + _random.Next(0, possibleSteps) * step;
            return result;
        }

        public int Next(int minInclusive, int maxExclusive)
        {
            return _random.Next(minInclusive, maxExclusive);
        }

        public int Next(int minInclusive, int maxExclusive, int step)
        {
            // FIXME: this must be a bad hack...
            return (int)NextLong(minInclusive, maxExclusive, step);
        }

        public long NextLong(
            long minInclusive,
            long maxExclusive) => NextLong(
                minInclusive,
                maxExclusive,
                1);

        public long NextLong(long minInclusive, long maxExclusive, long step)
        {
            // FIXME: this must be a bad hack...
            var result = _random.Next((int)(minInclusive / step), (int)(maxExclusive / step)) * step;
            return result;
        }
    }
}
