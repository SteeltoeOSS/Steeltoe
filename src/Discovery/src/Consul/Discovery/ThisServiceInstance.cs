﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Discovery;
using Steeltoe.Discovery.Consul.Registry;
using System;
using System.Collections.Generic;

namespace Steeltoe.Discovery.Consul.Discovery
{
    internal class ThisServiceInstance : IServiceInstance
    {
        public ThisServiceInstance(IConsulRegistration registration)
        {
            ServiceId = registration.Service.Name;
            Host = registration.Service.Address;
            IsSecure = registration.IsSecure;
            Port = registration.Port;
            Metadata = registration.Metadata;
            Uri = registration.Uri;
        }

        #region Implementation of IServiceInstance

        /// <inheritdoc/>
        public string ServiceId { get; }

        /// <inheritdoc/>
        public string Host { get; }

        /// <inheritdoc/>
        public int Port { get; }

        /// <inheritdoc/>
        public bool IsSecure { get; }

        /// <inheritdoc/>
        public Uri Uri { get; }

        /// <inheritdoc/>
        public IDictionary<string, string> Metadata { get; }

        #endregion Implementation of IServiceInstance
    }
}