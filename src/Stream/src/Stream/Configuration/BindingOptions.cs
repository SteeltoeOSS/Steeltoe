// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;

namespace Steeltoe.Stream.Configuration;

public class BindingOptions : IBindingOptions
{
    public static readonly MimeType DefaultContentType = MimeTypeUtils.ApplicationJson;

    IConsumerOptions IBindingOptions.Consumer => Consumer;

    IProducerOptions IBindingOptions.Producer => Producer;

    public string Destination { get; set; }

    public string Group { get; set; }

    public string ContentType { get; set; }

    public string Binder { get; set; }

    public ConsumerOptions Consumer { get; set; }

    public ProducerOptions Producer { get; set; }

    internal void PostProcess(string name, BindingOptions @default)
    {
        Destination ??= @default?.Destination;
        Group ??= @default?.Group;
        ContentType ??= @default != null ? @default.ContentType : DefaultContentType.ToString();
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
