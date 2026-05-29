// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text;
using Steeltoe.Configuration.Encryption.Cryptography;

namespace Steeltoe.Configuration.Encryption.Test.Cryptography;

public sealed class RsaKeyStoreDecryptorTest
{
    private readonly KeyProvider _keyProvider = new("./Cryptography/server.jks", "letmein");

    [Fact]
    public void Decrypt_WithGarbageBase64Throws()
    {
        var rsaKeyStoreDecryptor = new RsaKeyStoreDecryptor(_keyProvider, "nonexistingKey");

        Action action = () => rsaKeyStoreDecryptor.Decrypt("dGhpcyBpcyBjb21wbGV0ZSBnYXJiYWdl");

        action.Should().ThrowExactly<DecryptionException>();
    }

    [Fact]
    public void Decrypt_WithWrongCiphertextKeyThrows()
    {
        const string cipher =
            "AQAbWqohCeQ+TTqyJ3ZlNvAtx5cC2I3PmJetuSR82yRRyX+wWd7mTkUXuN/wANJ+nr1ySdzPudjml1lHaxZn42I9szkIKSkNT+6Yg+zNaREMetcE5SXA1awtSbEaFY2NcualSzPVWs8ulsUkKlYyyh6XP9gT/kODbmX0mS6DCtxalJgjei7WujLaJaPjc3jk+EhV9M1TovexqI7XoLlsgrGf6/1gQE+SSOamTFJopWpYEeSpSEwY2dXZfct5KCFWGJVA7eDPRJk0dT6EWIvqd6J4YoMWonxgVy4nG/Gq0NTisXv9XpJHAPYBg0c8B0WrWi2PG/Q00wvFRqGmYQ1hQIVmbJm8z+f0WoCxKwnCZvvdLlgrx2qeK1S21dPdgtmLXlj5bRUrektFrNhlevlENW7wgg==";

        var rsaKeyStoreDecryptor = new RsaKeyStoreDecryptor(_keyProvider, "mytestkey");

        Action action = () => rsaKeyStoreDecryptor.Decrypt(cipher);

        action.Should().ThrowExactly<DecryptionException>();
    }

    [Fact]
    public void Constructor_WithUnsupportedAlgorithmThrows()
    {
        Action action = () => _ = new RsaKeyStoreDecryptor(_keyProvider, "mytestkey", "deadbeef", false, " Exception");

        action.Should().ThrowExactly<ArgumentException>();
    }

    [Theory]
    [MemberData(nameof(GetSpringConfigServerTestVectors))]
    public void Decrypt_WithSpringCipherText_UsingDefaultKeyAlias(string salt, string strong, string algorithm, string cipher, string plainText)
    {
        var decryptor = new RsaKeyStoreDecryptor(_keyProvider, "mytestkey", salt, bool.Parse(strong), algorithm);
        string decrypted = decryptor.Decrypt(cipher);

        decrypted.Should().Be(plainText);
    }

    [Theory]
    [MemberData(nameof(GetSpringConfigServerTestVectors))]
    public void Decrypt_WithSpringCipherText_UsingExplicitKeyAlias(string salt, string strong, string algorithm, string cipher, string plainText)
    {
        var decryptor = new RsaKeyStoreDecryptor(_keyProvider, "someKey", salt, bool.Parse(strong), algorithm);
        string decrypted = decryptor.Decrypt(cipher, "mytestkey");

        decrypted.Should().Be(plainText);
    }

    // Requires Config Server to be running with OAEP encryption configured (see docker-compose.yml at the repo root)
    [Fact]
    [Trait("Category", "Integration")]
    public async Task Decrypt_WithOaepAlgorithm_CanDecryptSpringConfigServerCipherText()
    {
        // ReSharper disable once ShortLivedHttpClient
        using var httpClient = new HttpClient();

        HttpResponseMessage response = await httpClient.PostAsync(new Uri("http://localhost:8888/encrypt"),
            new StringContent("encrypt the world", Encoding.UTF8, "text/plain"), TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();
        string springCipherText = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        var decryptor = new RsaKeyStoreDecryptor(_keyProvider, "mytestkey", "deadbeef", false, "OAEP");
        string decrypted = decryptor.Decrypt(springCipherText);

        decrypted.Should().Be("encrypt the world");
    }

    // Pre-generated ciphertext is from Spring Cloud Config Server (steeltoe.azurecr.io/config-server:4.3.1)
    public static TheoryData<string, string, string, string, string> GetSpringConfigServerTestVectors()
    {
        List<(string Salt, string Strong, string Algorithm, string Cipher, string PlainText)> data =
        [
            ("deadbeef", "false", "OAEP",
                "AQA5BZk2Pg7/nbcuTrJ/i4MDOIc831GfHLUg1GQlBtOvRJm2iXngfbPKcnTjbtZZ9X+qPnbdkUcTVbgYcsszY3uoqWIN5Yybwg+dHsqZTv1/XSvwR/kwflDg+I6C+dxg3GepoCAZSPi+J5/MsfCYJAp6KI3WW34tbqkqNlJq1TmF4b/AQHmP7Pth6cIsFE7svQ0xDQRhY61lJESLvLZ1Em4XpA4cfiye1YQhNud7/AKTtyENf7oPT/7siWBN82gyCB63/HEMRNtSLobOKO1XzgWNc96ms4pIhACOA3cZarTDUqaKY+B84ATV5QKgfkQ/ihI6r2oeYB24ApKwjNyE4F4b0bFH1cchdsbooreJlflgn0U9gK0oo8t7cVGnih3lccJs3t0uAFVm+SrJGMG/8rgp",
                "encrypt the world"),
            ("beefdead", "true", "OAEP",
                "AQAwoGhHV/Z82UWIrmqmTT92L510iKkwiF+EhlroV/No3dLwamUovEB9n/4IF+j6wfv8q1Baqekn5y6folcQmiMJd86JHW2n+WNeKUlbjf3Rk5uwgSTL2ST1JZ8w6sZ0PZVE2tqaQoc9mHRmjT7hqRm1lQVsHsic5tCxdTmhYVdGp5J6UGTRPQNfyJBR34w+LFjtgyaOrF//o8Z5ZF9XUx3MGaoe4HnURIYRq5HHcd4yVFINaBpW19ndgPV+nWRANxnmltLgPUbLWBJSvJ8czHOfZvZnTSJrWDBp1GIHN0OFkObJAIl7hmOdCh3vFPkxOL9gH4690VlMOCWYI8elsvuFsOdPG++FtJVSbuGgYC3AnuFo955yBfy8tgdegQ5Zzb2sOS2mwqsWr4mly4Pis+bgpw==",
                "encrypt the world"),
            ("deadbeef", "false", "DEFAULT",
                "AQBSAVVzUP21aZXVAnuTMBDQ+/HGatD/+6H2YT/EbVofx5pWNJIjOlq1ioDpLHRB/JS86nI5oC9scEVBajc/gcWiYJAOtG4+g0Sw2ixzmi3jmho/CYxhtbxGFrkrTOC0r/0I6gcGgCo5ZrQCtaQDUMnHn+aFwo8baduKQ2N6qMyGHvfXIqqJFabnTkYDlLlqgNa3jpI3oKicaDTvPU3jFO42fJyVFWyAdQ8YS0RZdOXV+0xQdRnHrHHjhR8W7D7e0Jyx05RKq1ZEXvN7+x+YSE7ajrwy8riGuxR9a8smZAKkXC8T7KcZMRqtkd/9bpNS10bpw21KSxxp2GF52ekbu0xZYIIPdIj57me7HGubwNd1kXXgV+3L6sZ1IUAN0xnOOEUQD3z6hOWkrTEAmSbNRdYM",
                "encrypt the world"),
            ("beefdead", "true", "DEFAULT",
                "AQAhwKArLZqxrc44G2sG6+EwWeqn9JytaIyBpf/Yz2UZ0bLZthR3HPtGgOoKY9AkWpBuRzrw3zQ20ZRkq6q7XU+Stp1kB4OXhrmgbwydNUtYJmuTlpGohtHH8wVoT2n0bd7NuL9rJ2OAbkPXg8K1kJMSgen7Hyg3b+LFZmaA8wCHXdmHuP63Rk4NhSec4Ul/gRRn5jftojmbxVVQ6xRGAeFTZi70oAZ+tzdyXZmukorRZsUtnlgj94aSmGdMCGkukanCiEHHrh130Nigxba4qZ2F2e5n46De7+7EVwnIWWYa2sQH+3BQ+cp5OCebWMiGPdylqZzyTagkwo2jHv/OzW0/ytIF1Qo3AblMQgympSL3/PMPggllopaf2al4o7w63vWczXdv6YzdLchQMrdXRdkLrw==",
                "encrypt the world")
        ];

        TheoryData<string, string, string, string, string> theoryData = [];

        foreach ((string salt, string strong, string algorithm, string cipher, string plainText) in data)
        {
            theoryData.Add(salt, strong, algorithm, cipher, plainText);
        }

        return theoryData;
    }
}
