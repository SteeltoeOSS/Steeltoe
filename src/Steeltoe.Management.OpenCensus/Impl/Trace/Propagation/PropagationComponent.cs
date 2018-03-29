using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Trace.Propagation
{
    public sealed class PropagationComponent : PropagationComponentBase
    {
        private readonly BinaryFormat binaryFormat = new BinaryFormat();
        private readonly B3Format b3Format = new B3Format();

        public override IBinaryFormat BinaryFormat
        {
            get
            {
                return binaryFormat;
            }
        }

        public override ITextFormat TextFormat
        {
            get
            {
                return b3Format;
            }
        }
    }
}
