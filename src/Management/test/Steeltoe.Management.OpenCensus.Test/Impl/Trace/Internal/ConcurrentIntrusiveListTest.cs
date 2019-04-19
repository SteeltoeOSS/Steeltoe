using Steeltoe.Management.Census.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Management.Census.Trace.Internal.Test
{
    public class ConcurrentIntrusiveListTest
    {
        private readonly ConcurrentIntrusiveList<FakeElement> intrusiveList = new ConcurrentIntrusiveList<FakeElement>();

        [Fact]
        public void EmptyList()
        {
            Assert.Equal(0, intrusiveList.Count);
            Assert.Empty(intrusiveList.Copy());
        }

        [Fact]
        public void AddRemoveAdd_SameElement()
        {
            FakeElement element = new FakeElement();
            intrusiveList.AddElement(element);
            Assert.Equal(1, intrusiveList.Count);
            intrusiveList.RemoveElement(element);
            Assert.Equal(0, intrusiveList.Count);
            intrusiveList.AddElement(element);
            Assert.Equal(1, intrusiveList.Count);
        }

        [Fact]
        public void addAndRemoveElements()
        {
            FakeElement element1 = new FakeElement();
            FakeElement element2 = new FakeElement();
            FakeElement element3 = new FakeElement();
            intrusiveList.AddElement(element1);
            intrusiveList.AddElement(element2);
            intrusiveList.AddElement(element3);
            Assert.Equal(3, intrusiveList.Count);
            var copy = intrusiveList.Copy();
            Assert.Equal(element3, copy[0]);
            Assert.Equal(element2, copy[1]);
            Assert.Equal(element1, copy[2]);
            // Remove element from the middle of the list.
            intrusiveList.RemoveElement(element2);
            Assert.Equal(2, intrusiveList.Count);
            copy = intrusiveList.Copy();
            Assert.Equal(element3, copy[0]);
            Assert.Equal(element1, copy[1]);
            // Remove element from the tail of the list.
            intrusiveList.RemoveElement(element1);
            Assert.Equal(1, intrusiveList.Count);
            copy = intrusiveList.Copy();
            Assert.Equal(element3, copy[0]);

            intrusiveList.AddElement(element1);
            Assert.Equal(2, intrusiveList.Count);
            copy = intrusiveList.Copy();
            Assert.Equal(element1, copy[0]);
            Assert.Equal(element3, copy[1]);
            // Remove element from the head of the list when there are other elements after.
            intrusiveList.RemoveElement(element1);
            Assert.Equal(1, intrusiveList.Count);
            copy = intrusiveList.Copy();
            Assert.Equal(element3, copy[0]);
            // Remove element from the head of the list when no more other elements in the list.
            intrusiveList.RemoveElement(element3);
            Assert.Equal(0, intrusiveList.Count);
            Assert.Empty(intrusiveList.Copy());
        }

        [Fact]
        public void AddAlreadyAddedElement()
        {
            FakeElement element = new FakeElement();
            intrusiveList.AddElement(element);

            Assert.Throws<ArgumentOutOfRangeException>(() => intrusiveList.AddElement(element));
        }

        [Fact]
        public void removeNotAddedElement()
        {
            FakeElement element = new FakeElement();
            Assert.Throws<ArgumentOutOfRangeException>(() => intrusiveList.RemoveElement(element));
        }


        private sealed class FakeElement : IElement<FakeElement>
        {
            public FakeElement Next { get; set; }
            public FakeElement Previous { get; set; }
        }
    }
}
