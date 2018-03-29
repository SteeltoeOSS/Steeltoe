using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Management.Census.Stats.Test
{
    public class BucketBoundariesTest
    {

        [Fact]
        public void TestConstructBoundaries()
        {
            List<Double> buckets = new List<double>() { 0.0, 1.0, 2.0 };
            IBucketBoundaries bucketBoundaries = BucketBoundaries.Create(buckets);
            Assert.Equal(buckets, bucketBoundaries.Boundaries);
        }

        [Fact]
        public void TestBoundariesDoesNotChangeWithOriginalList()
        {
            List<double> original = new List<double>();
            original.Add(0.0);
            original.Add(1.0);
            original.Add(2.0);
            IBucketBoundaries bucketBoundaries = BucketBoundaries.Create(original);
            original[2] = 3.0;
            original.Add(4.0);
            List<double> expected = new List<double>() { 0.0, 1.0, 2.0 };
            Assert.NotEqual(original, bucketBoundaries.Boundaries);
            Assert.Equal(expected, bucketBoundaries.Boundaries);
        }

        [Fact]
        public void TestNullBoundaries()
        {
            Assert.Throws<ArgumentNullException>(() => BucketBoundaries.Create(null));
        }

        [Fact]
        public void TestUnsortedBoundaries()
        {
            List<double> buckets = new List<double>() { 0.0, 1.0, 1.0 };
            Assert.Throws<ArgumentOutOfRangeException>(() => BucketBoundaries.Create(buckets));
        }

        [Fact]
        public void TestNoBoundaries()
        {
            List<double> buckets = new List<double>();
            IBucketBoundaries bucketBoundaries = BucketBoundaries.Create(buckets);
            Assert.Equal(buckets, bucketBoundaries.Boundaries);
        }

        [Fact]
        public void TestBucketBoundariesEquals()
        {
            var b1 = BucketBoundaries.Create(new List<double>() { -1.0, 2.0 });
            var b2 = BucketBoundaries.Create(new List<double>() { -1.0, 2.0 });
            var b3 = BucketBoundaries.Create(new List<double>() { -1.0 });
            Assert.Equal(b1, b2);
            Assert.Equal(b3, b3);
            Assert.NotEqual(b1, b3);
            Assert.NotEqual(b2, b3);

        }
    }
}
