// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Steeltoe.Configuration.Encryption.Cryptography;

namespace Steeltoe.Configuration.Encryption;

internal sealed partial class DecryptionConfigurationProvider(
    IList<IConfigurationProvider> providers, ITextDecryptor? textDecryptor, ILoggerFactory loggerFactory)
    : CompositeConfigurationProvider(providers, loggerFactory)
{
#pragma warning disable IDE0052 // Remove unread private members
    private readonly ILogger<DecryptionConfigurationProvider> _logger = loggerFactory.CreateLogger<DecryptionConfigurationProvider>();
#pragma warning restore IDE0052 // Remove unread private members
    private ITextDecryptor? _textDecryptor = textDecryptor;

    [GeneratedRegex("^{cipher}({key:(?<alias>.*)})?(?<cipher>.*)$", RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture, 1000)]
    private static partial Regex CipherRegex();

    public override bool TryGet(string key, out string? value)
    {
        bool found = base.TryGet(key, out value);

        if (found && ConfigurationRoot != null && !string.IsNullOrEmpty(value))
        {
            Match match = CipherRegex().Match(value);

            if (match.Success)
            {
                string alias = match.Groups["alias"].Value;
                string cipher = match.Groups["cipher"].Value;

                ITextDecryptor decryptor = EnsureDecryptor(ConfigurationRoot);
                string decryptedValue = !string.IsNullOrEmpty(alias) ? decryptor.Decrypt(cipher, alias) : decryptor.Decrypt(cipher);

                if (decryptedValue != value)
                {
                    LogDecrypt(key, value, decryptedValue);
                    value = decryptedValue;
                }
            }
        }

        return value != null;
    }

    private ITextDecryptor EnsureDecryptor(IConfigurationRoot configurationRoot)
    {
        _textDecryptor ??= ConfigServerDecryptionSettings.CreateTextDecryptor(configurationRoot);
        return _textDecryptor;
    }

    [LoggerMessage(Level = LogLevel.Trace, Message = "Decrypted value '{CipherValue}' at key '{Key}' to '{PlainTextValue}'.")]
    private partial void LogDecrypt(string key, string? cipherValue, string? plainTextValue);
}
