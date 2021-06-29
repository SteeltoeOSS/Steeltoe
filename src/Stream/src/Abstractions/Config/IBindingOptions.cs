// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Stream.Config
{
    /// <summary>
    /// Contains the configuration options for a binding
    /// </summary>
    public interface IBindingOptions
    {
        string Destination { get; set; }

        string Group { get; set; }

        string ContentType { get; set; }

        string Binder { get; set; }

        IConsumerOptions Consumer { get; }

        IProducerOptions Producer { get; }
    }
}
