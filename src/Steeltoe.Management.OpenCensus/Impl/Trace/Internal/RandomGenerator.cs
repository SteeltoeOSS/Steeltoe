
using System;

namespace Steeltoe.Management.Census.Trace.Internal
{
    [Obsolete("Use OpenCensus project packages")]
    internal class RandomGenerator : IRandomGenerator
    {
        private Random _random;
        internal RandomGenerator()
        {
            _random = new Random();
        }

        internal RandomGenerator(int seed)
        {
            _random = new Random(seed);
        }

        public void NextBytes(byte[] bytes)
        {
            _random.NextBytes(bytes);
        }
    }
}
