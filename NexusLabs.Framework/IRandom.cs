namespace NexusLabs.Framework
{
    public interface IRandom
    {
        int Next(
            int minInclusive,
            int maxExclusive);

        int Next(
            int minInclusive,
            int maxExclusive,
            int step);

        double NextDouble(
            double minInclusive,
            double maxExclusive);

        double NextDouble(
            double minInclusive,
            double maxExclusive,
            double step);

        long NextLong(
            long minInclusive,
            long maxExclusive);

        long NextLong(
            long minInclusive,
            long maxExclusive,
            long step);
    }
}
