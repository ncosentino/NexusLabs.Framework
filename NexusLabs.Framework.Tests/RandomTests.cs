using Xunit;

namespace NexusLabs.Framework.Tests
{
    public sealed class RandomTests
    {
        private readonly Random _random;

        public RandomTests()
        {
            _random = new Random(new System.Random(1137));
        }

        [InlineData(0, 1, 2, new double[] { 0, 2 })]
        [InlineData(1, 1, 2, new double[] { 1 })]
        [InlineData(1, 1, 1, new double[] { 1 })]
        [InlineData(0, 1, 1, new double[] { 0, 1 })]
        [InlineData(0, 0, 1, new double[] { 0 })]
        [Theory]
        private void NextDouble_ValidScenarios(
            double min,
            double max,
            double step,
            double[] possibleExpectedValues)
        {
            var result = _random.NextDouble(min, max, step);
            Assert.Contains(
                result,
                possibleExpectedValues);
        }
    }
}
