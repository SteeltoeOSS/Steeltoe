using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Trace.Propagation
{
    public abstract class PropagationComponentBase : IPropagationComponent
    {
        private static readonly IPropagationComponent NOOP_PROPAGATION_COMPONENT = new NoopPropagationComponent();
        public static IPropagationComponent NoopPropagationComponent
        {
            get
            {
                return NOOP_PROPAGATION_COMPONENT;
            }
        }

        public abstract IBinaryFormat BinaryFormat { get; }
        public abstract ITextFormat TextFormat { get;  }
    }
}
