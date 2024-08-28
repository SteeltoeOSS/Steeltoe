// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Steeltoe.Configuration.Encryption.Test;

public sealed class StartupForConfigureEncryptionResolver
{
    private readonly IConfiguration _configuration;
    private readonly TextDecryptorForTest _textDecryptor = new();

    public StartupForConfigureEncryptionResolver(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        _configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.ConfigureEncryptionResolver(_configuration, _textDecryptor);
    }

    public void Configure(IApplicationBuilder app)
    {
    }

    private sealed class TextDecryptorForTest : ITextDecryptor
    {
        public string Decrypt(string fullCipher)
        {
            return "DECRYPTED";
        }

        public string Decrypt(string fullCipher, string alias)
        {
            return "DECRYPTEDWITHALIAS";
        }
    }
}
