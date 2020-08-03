// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using System;

namespace Steeltoe.Common.Security
{
    public class PemCertificateProvider : ConfigurationProvider
    {
        private readonly IConfigurationRoot _certFileProvider;
        private readonly IConfigurationRoot _keyFileProvider;

        public PemCertificateProvider(IConfigurationRoot certFileProvider, IConfigurationRoot keyFileProvider)
        {
            _certFileProvider = certFileProvider;
            _keyFileProvider = keyFileProvider;
            _certFileProvider.GetReloadToken().RegisterChangeCallback(NotifyCertChanged, null);
            _keyFileProvider.GetReloadToken().RegisterChangeCallback(NotifyKeyChanged, null);
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
            value = _certFileProvider[key];
            if (!string.IsNullOrEmpty(value))
            {
                return true;
            }

            value = _keyFileProvider[key];
            if (!string.IsNullOrEmpty(value))
            {
                return true;
            }

            return false;
        }

        private void NotifyCertChanged(object state)
        {
            OnReload();
            _certFileProvider.GetReloadToken().RegisterChangeCallback(NotifyCertChanged, null);
        }

        private void NotifyKeyChanged(object state)
        {
            OnReload();
            _keyFileProvider.GetReloadToken().RegisterChangeCallback(NotifyKeyChanged, null);
        }
    }
}
