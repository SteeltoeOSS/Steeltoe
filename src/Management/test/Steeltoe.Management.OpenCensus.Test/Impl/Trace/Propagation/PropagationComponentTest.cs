using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Management.Census.Trace.Propagation.Test
{
    public class PropagationComponentTest
    {
        private readonly PropagationComponent propagationComponent = new PropagationComponent();

        [Fact]
        public void ImplementationOfBinary()
        {
            Assert.IsType<BinaryFormat>(propagationComponent.BinaryFormat);
        }

        [Fact]
        public void ImplementationOfB3Format()
        {
            Assert.IsType<B3Format>(propagationComponent.TextFormat);
        }
    }
}
