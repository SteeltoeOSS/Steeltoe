using Steeltoe.Messaging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Integration.Transformer
{
    public interface ITransformer
    {
        IMessage Transform(IMessage message);
    }
}
