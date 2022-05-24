// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO.Compression;

namespace Steeltoe.Stream.Binder.Rabbit.Config
{
    public class RabbitBinderOptions
    {
        public const string PREFIX = "spring:cloud:stream:rabbit:binder";

        public RabbitBinderOptions()
        {
        }

        internal RabbitBinderOptions(IConfiguration config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            config.Bind(this);
        }

        public List<string> AdminAddresses { get; set; } = new ();

        public List<string> Nodes { get; set; } = new ();

        public CompressionLevel CompressionLevel { get; set; }

        public string ConnectionNamePrefix { get; set; }
    }
}
