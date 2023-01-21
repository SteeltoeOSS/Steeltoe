using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Pkcs;

namespace Steeltoe.Configuration.ConfigServer.Encryption;

public interface IKeyProvider
{
    ICipherParameters GetKey(string keyAlias);
}

public class KeyProvider : IKeyProvider
{
    private readonly Pkcs12Store _pkcs12;

    public KeyProvider(string fileName, string pfxPassword): this(new FileStream(fileName, FileMode.Open, FileAccess.Read), pfxPassword)
    {
    }

    public KeyProvider(FileStream fileStream,  string pfxPassword)
    {
        _pkcs12 = new Pkcs12Store(fileStream, pfxPassword.ToArray());
    }

    public ICipherParameters GetKey(string keyAlias)
    {
        return _pkcs12.GetKey(keyAlias).Key;
    }
}
