using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Stream.Config
{
    public class ExtendedProducerOptions<T> : ProducerOptions
    {
        public ExtendedProducerOptions(T extension)
        {
            Extension = extension;
            PostProcess(string.Empty);
        }

        public T Extension { get; set; }
    }
}
