using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Stream.Config
{
    public class ExtendedConsumerOptions<T> : ConsumerOptions
    {
        public ExtendedConsumerOptions(T extension)
        {
            Extension = extension;
            PostProcess(string.Empty);
        }

        public T Extension { get; set; }
    }
}
