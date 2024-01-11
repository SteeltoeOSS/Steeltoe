// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Common;
using Steeltoe.Common.Hosting;
using Steeltoe.Configuration.Encryption.Decryption;

namespace Steeltoe.Configuration.Encryption;

internal static class HostBuilderWrapperExtensions
{
    public static HostBuilderWrapper AddEncryptionResolver(this HostBuilderWrapper wrapper, ILoggerFactory loggerFactory)
    {
        ArgumentGuard.NotNull(wrapper);
        ArgumentGuard.NotNull(loggerFactory);

        wrapper.ConfigureAppConfiguration((context, configurationBuilder) =>
        {
            ITextDecryptor textDecryptor = ConfigServerEncryptionSettings.CreateTextDecryptor(context.Configuration);
            configurationBuilder.AddEncryptionResolver(textDecryptor, loggerFactory);
        });

        return wrapper;
    }
}
