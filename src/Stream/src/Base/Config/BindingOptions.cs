// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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
