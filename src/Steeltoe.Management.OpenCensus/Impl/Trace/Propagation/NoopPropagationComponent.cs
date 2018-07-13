using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Trace.Propagation
{
    internal class NoopPropagationComponent : IPropagationComponent
    {
        public IBinaryFormat BinaryFormat
        {
            get
            {
                return Propagation.BinaryFormatBase.NoopBinaryFormat;
            }
        }

        public ITextFormat TextFormat
        {
            get
            {
                return Propagation.TextFormatBase.NoopTextFormat;
            }
        }

    }
}
