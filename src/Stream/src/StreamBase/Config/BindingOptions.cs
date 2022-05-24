// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;

namespace Steeltoe.Stream.Config
{
    public class BindingOptions : IBindingOptions
    {
        public static readonly MimeType DEFAULT_CONTENT_TYPE = MimeTypeUtils.APPLICATION_JSON;

        public string Destination { get; set; }

        public string Group { get; set; }

        public string ContentType { get; set; }

        public string Binder { get; set; }

        public ConsumerOptions Consumer { get; set; }

        public ProducerOptions Producer { get; set; }

        IConsumerOptions IBindingOptions.Consumer { get => Consumer; }

        IProducerOptions IBindingOptions.Producer { get => Producer; }

        internal void PostProcess(string name, BindingOptions @default)
        {
            Destination ??= @default?.Destination;
            Group ??= @default?.Group;
            ContentType ??= @default != null ? @default.ContentType : DEFAULT_CONTENT_TYPE.ToString();
            Binder ??= @default?.Binder;

            Consumer?.PostProcess(name, @default?.Consumer);
            Producer?.PostProcess(name, @default?.Producer);
        }

        internal BindingOptions Clone(bool deep = false)
        {
            var clone = (BindingOptions)MemberwiseClone();

            if (deep)
            {
                if (Producer != null)
                {
                    Producer = (ProducerOptions)Producer.Clone();
                }

                if (Consumer != null)
                {
                    Consumer = (ConsumerOptions)Consumer.Clone();
                }
            }

            return clone;
        }
    }
}
