﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;

namespace Steeltoe.Stream.Config
{
    public class BindingOptions : IBindingOptions
    {
        public BindingOptions()
        {
        }

        public static readonly MimeType DEFAULT_CONTENT_TYPE = MimeTypeUtils.APPLICATION_JSON;

        public string Destination { get; set; }

        public string Group { get; set; }

        public string ContentType { get; set; }

        public string Binder { get; set; }

        public ConsumerOptions Consumer { get; set; }

        public ProducerOptions Producer { get; set; }

        IConsumerOptions IBindingOptions.Consumer { get => Consumer; }

        IProducerOptions IBindingOptions.Producer { get => Producer; }

        internal void PostProcess(BindingOptions @default)
        {
            if (Destination == null)
            {
                Destination = @default?.Destination;
            }

            if (Group == null)
            {
                Group = @default?.Group;
            }

            if (ContentType == null)
            {
                ContentType = (@default != null) ? @default.ContentType : DEFAULT_CONTENT_TYPE.ToString();
            }

            if (Binder == null)
            {
                Binder = @default?.Binder;
            }

            Consumer?.PostProcess(@default?.Consumer);
            Producer?.PostProcess(@default?.Producer);
        }

        internal BindingOptions Clone(bool deep = false)
        {
            var clone = (BindingOptions)MemberwiseClone();

            if (deep)
            {
                if (Producer != null)
                {
                    Producer = Producer.Clone();
                }

                if (Consumer != null)
                {
                    Consumer = Consumer.Clone();
                }
            }

            return clone;
        }
    }
}
