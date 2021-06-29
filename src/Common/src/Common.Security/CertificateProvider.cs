﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace Steeltoe.Common.Security
{
    public class CertificateProvider : ConfigurationProvider
    {
        private readonly IConfigurationProvider _certificateProvider;
        private readonly string _certificatePath;

        internal CertificateProvider(FileSource certificateSource)
        {
            _certificateProvider = certificateSource.Build(new ConfigurationBuilder());
            _certificateProvider.GetReloadToken().RegisterChangeCallback(NotifyCertChanged, null);
            _certificatePath = Path.Combine(certificateSource.BasePath, certificateSource.Path);
        }

        public override void Load()
        {
            // for future use
        }

        public override void Set(string key, string value)
        {
            throw new InvalidOperationException();
        }

        public override bool TryGet(string key, out string value)
        {
            value = null;

            if (key == "certificate")
            {
                value = _certificatePath;
            }

            if (!string.IsNullOrEmpty(value))
            {
                return true;
            }

            return false;
        }

        private void NotifyCertChanged(object state)
        {
            OnReload();
            _certificateProvider.GetReloadToken().RegisterChangeCallback(NotifyCertChanged, null);
        }
    }
}
