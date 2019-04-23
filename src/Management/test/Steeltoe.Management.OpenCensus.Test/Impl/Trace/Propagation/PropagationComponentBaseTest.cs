using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Management.Census.Trace.Propagation.Test
{
    public class PropagationComponentBaseTest
    {
        private readonly IPropagationComponent propagationComponent = PropagationComponentBase.NoopPropagationComponent;

        [Fact]
        public void ImplementationOfBinaryFormat()
        {
            Assert.Equal(BinaryFormat.NoopBinaryFormat, propagationComponent.BinaryFormat);
        }
    }
}
