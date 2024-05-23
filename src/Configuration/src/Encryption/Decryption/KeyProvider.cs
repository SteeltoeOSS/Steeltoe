// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Pkcs;
using Steeltoe.Common;

namespace Steeltoe.Configuration.Encryption.Decryption;

internal sealed class KeyProvider : IKeyProvider
{
    private readonly Pkcs12Store _store;

    public KeyProvider(string fileName, string pfxPassword)
    {
        ArgumentGuard.NotNull(fileName);
        ArgumentGuard.NotNull(pfxPassword);

        using var fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read);
        _store = CreateStore(fileStream, pfxPassword);
    }

    public KeyProvider(Stream fileStream, string pfxPassword)
    {
        ArgumentGuard.NotNull(fileStream);
        ArgumentGuard.NotNull(pfxPassword);

        _store = CreateStore(fileStream, pfxPassword);
    }

    private static Pkcs12Store CreateStore(Stream fileStream, string pfxPassword)
    {
        var builder = new Pkcs12StoreBuilder();
        Pkcs12Store store = builder.Build();

        store.Load(fileStream, pfxPassword.ToArray());
        return store;
    }

    public ICipherParameters? GetKey(string keyAlias)
    {
        return _store.GetKey(keyAlias)?.Key;
    }
}
