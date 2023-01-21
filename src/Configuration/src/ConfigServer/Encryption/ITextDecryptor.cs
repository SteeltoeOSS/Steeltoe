namespace Steeltoe.Configuration.ConfigServer.Encryption;

public interface ITextDecryptor
{
    string Decrypt(string cipher);
}
