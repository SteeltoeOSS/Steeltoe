// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Text.Json;
using RichardSzalay.MockHttp;
using Steeltoe.Security.DataProtection.CredHub.Credentials;
using Steeltoe.Security.DataProtection.CredHub.Credentials.Certificate;
using Steeltoe.Security.DataProtection.CredHub.Credentials.Json;
using Steeltoe.Security.DataProtection.CredHub.Credentials.Password;
using Steeltoe.Security.DataProtection.CredHub.Credentials.Permissions;
using Steeltoe.Security.DataProtection.CredHub.Credentials.Rsa;
using Steeltoe.Security.DataProtection.CredHub.Credentials.Ssh;
using Steeltoe.Security.DataProtection.CredHub.Credentials.User;
using Steeltoe.Security.DataProtection.CredHub.Credentials.Value;
using Xunit;

namespace Steeltoe.Security.DataProtection.CredHub.Test;

public class CredHubClientTests
{
    private const string CredHubBase = "http://credhubServer/api/";

    [Fact]
    public async Task CreateAsync_RequestsToken_Once()
    {
        MockHttpMessageHandler mockHttpMessageHandler = InitializedHandlerWithLogin();

        await InitializeClientAsync(mockHttpMessageHandler);

        mockHttpMessageHandler.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task WriteAsync_Sets_Values()
    {
        MockHttpMessageHandler mockHttpMessageHandler = InitializedHandlerWithLogin();

        MockedRequest mockRequest = mockHttpMessageHandler.Expect(HttpMethod.Put, $"{CredHubBase}/v1/data").Respond("application/json",
            "{\"type\":\"value\",\"version_created_at\":\"2017-11-10T15:55:24Z\",\"id\":\"2af5191f-9c05-4746-b72c-78b3283aef46\",\"name\":\"/example\",\"value\":\"sample\"}");

        CredHubClient client = await InitializeClientAsync(mockHttpMessageHandler);

        CredHubCredential<ValueCredential> response = await client.WriteAsync<ValueCredential>(new ValueSetRequest("example", "sample"));

        mockHttpMessageHandler.VerifyNoOutstandingExpectation();
        Assert.Equal(1, mockHttpMessageHandler.GetMatchCount(mockRequest));
        Assert.Equal(CredentialType.Value, response.Type);
        Assert.Equal(new DateTime(2017, 11, 10, 15, 55, 24, DateTimeKind.Utc), response.VersionCreatedAt);
        Assert.Equal(Guid.Parse("2af5191f-9c05-4746-b72c-78b3283aef46"), response.Id);
        Assert.Equal("/example", response.Name);
        Assert.Equal("sample", response.Value.ToString());
    }

    [Fact]
    public async Task WriteAsync_Sets_Json()
    {
        var jsonObject = JsonSerializer.Deserialize<JsonElement>(@"{""key"": 123,""key_list"": [""val1"",""val2""],""is_true"": true}");
        MockHttpMessageHandler mockHttpMessageHandler = InitializedHandlerWithLogin();

        MockedRequest mockRequest = mockHttpMessageHandler.Expect(HttpMethod.Put, $"{CredHubBase}/v1/data").Respond("application/json",
            $"{{\"type\":\"json\",\"version_created_at\":\"2017-11-10T15:55:24Z\",\"id\":\"b84cd415-2218-41c9-9455-b3e4c6a5ec0f\",\"name\":\"/example-json\",\"value\":{jsonObject}}}");

        CredHubClient client = await InitializeClientAsync(mockHttpMessageHandler);

        CredHubCredential<JsonCredential> response = await client.WriteAsync<JsonCredential>(new JsonSetRequest("example", jsonObject));

        mockHttpMessageHandler.VerifyNoOutstandingExpectation();
        Assert.Equal(1, mockHttpMessageHandler.GetMatchCount(mockRequest));
        Assert.Equal(CredentialType.JSON, response.Type);
        Assert.Equal(new DateTime(2017, 11, 10, 15, 55, 24, DateTimeKind.Utc), response.VersionCreatedAt);
        Assert.Equal(Guid.Parse("b84cd415-2218-41c9-9455-b3e4c6a5ec0f"), response.Id);
        Assert.Equal("/example-json", response.Name);
        Assert.Equal(jsonObject.ToString(), response.Value.ToString());
    }

    [Fact]
    public async Task WriteAsync_Sets_Password()
    {
        MockHttpMessageHandler mockHttpMessageHandler = InitializedHandlerWithLogin();

        MockedRequest mockRequest = mockHttpMessageHandler.Expect(HttpMethod.Put, $"{CredHubBase}/v1/data").Respond("application/json",
            "{\"type\":\"password\",\"version_created_at\":\"2017-11-10T15:55:24Z\",\"id\":\"73ef170e-12b7-4f91-94a0-e3a1686cbe2b\",\"name\":\"/example-password\",\"value\":\"sample\"}");

        CredHubClient client = await InitializeClientAsync(mockHttpMessageHandler);

        CredHubCredential<PasswordCredential> response = await client.WriteAsync<PasswordCredential>(new PasswordSetRequest("example", "sample"));

        mockHttpMessageHandler.VerifyNoOutstandingExpectation();
        Assert.Equal(1, mockHttpMessageHandler.GetMatchCount(mockRequest));
        Assert.Equal(CredentialType.Password, response.Type);
        Assert.Equal(new DateTime(2017, 11, 10, 15, 55, 24, DateTimeKind.Utc), response.VersionCreatedAt);
        Assert.Equal(Guid.Parse("73ef170e-12b7-4f91-94a0-e3a1686cbe2b"), response.Id);
        Assert.Equal("/example-password", response.Name);
        Assert.Equal("sample", response.Value.ToString());
    }

    [Fact]
    public async Task WriteAsyncUserWithPermissions_Sets_UserWithPermissions()
    {
        MockHttpMessageHandler mockHttpMessageHandler = InitializedHandlerWithLogin();

        MockedRequest mockRequest = mockHttpMessageHandler.Expect(HttpMethod.Put, $"{CredHubBase}/v1/data")
            .WithContent("{\"value\":{\"username\":\"testUser\",\"password\":\"testPassword\"},\"name\":\"example-user\",\"type\":\"User\"}").Respond(
                "application/json",
                "{\"type\":\"user\",\"version_created_at\":\"2017-11-22T21:49:09Z\",\"id\":\"b6dffbd6-ccca-4703-a4fd-8d39ca7b564a\",\"name\":\"/example-user\",\"value\":{\"username\":\"testUser\",\"password\":\"testPassword\",\"password_hash\":\"$6$rzwQOeLD$uuZp.sh9mT/bUGSB9i9x8pr.MG8hvo7bsUf2BNEKMVUErzEtYxGdmG6AfHtM1s087DUE1NeC01LOwtDLg3tLb/\"}}");

        CredHubClient client = await InitializeClientAsync(mockHttpMessageHandler);
        var request = new UserSetRequest("example-user", "testUser", "testPassword");

        CredHubCredential<UserCredential> response = await client.WriteAsync<UserCredential>(request);

        mockHttpMessageHandler.VerifyNoOutstandingExpectation();
        Assert.Equal(1, mockHttpMessageHandler.GetMatchCount(mockRequest));
        Assert.Equal(CredentialType.User, response.Type);
        Assert.Equal(new DateTime(2017, 11, 22, 21, 49, 09, DateTimeKind.Utc), response.VersionCreatedAt);
        Assert.Equal(Guid.Parse("b6dffbd6-ccca-4703-a4fd-8d39ca7b564a"), response.Id);
        Assert.Equal("/example-user", response.Name);
        Assert.Equal("testUser", response.Value.Username);
        Assert.Equal("testPassword", response.Value.Password);
        Assert.Equal("$6$rzwQOeLD$uuZp.sh9mT/bUGSB9i9x8pr.MG8hvo7bsUf2BNEKMVUErzEtYxGdmG6AfHtM1s087DUE1NeC01LOwtDLg3tLb/", response.Value.PasswordHash);
    }

    [Fact]
    public async Task WriteAsync_Sets_RootCertificate()
    {
        MockHttpMessageHandler mockHttpMessageHandler = InitializedHandlerWithLogin();

        MockedRequest mockRequest = mockHttpMessageHandler.Expect(HttpMethod.Put, $"{CredHubBase}/v1/data").Respond("application/json",
            "{\"type\":\"certificate\",\"version_created_at\":\"2017-11-10T15:55:24Z\",\"id\":\"657dd2f0-c2e4-4e28-b84a-171a730916b2\",\"name\":\"/example-certificate\",\"value\":{\"ca\":null,\"certificate\":\"-----BEGIN PUBLIC KEY-----\\nFakePublicKeyTextEAAQ==\\n-----END PUBLIC KEY-----\\n\",\"private_key\":\"-----BEGIN RSA PRIVATE KEY-----\\nFakePrivateKeyTextEAAQ==\\n-----END RSA PRIVATE KEY-----\\n\"}}");

        CredHubClient client = await InitializeClientAsync(mockHttpMessageHandler);
        const string privateKey = "-----BEGIN RSA PRIVATE KEY-----\nFakePrivateKeyTextEAAQ==\n-----END RSA PRIVATE KEY-----\n";
        const string certificate = "-----BEGIN PUBLIC KEY-----\nFakePublicKeyTextEAAQ==\n-----END PUBLIC KEY-----\n";

        CredHubCredential<CertificateCredential> response =
            await client.WriteAsync<CertificateCredential>(new CertificateSetRequest("example-certificate", null, certificate, privateKey));

        mockHttpMessageHandler.VerifyNoOutstandingExpectation();
        Assert.Equal(1, mockHttpMessageHandler.GetMatchCount(mockRequest));
        Assert.Equal(CredentialType.Certificate, response.Type);
        Assert.Equal(new DateTime(2017, 11, 10, 15, 55, 24, DateTimeKind.Utc), response.VersionCreatedAt);
        Assert.Equal(Guid.Parse("657dd2f0-c2e4-4e28-b84a-171a730916b2"), response.Id);
        Assert.Equal("/example-certificate", response.Name);
        Assert.Null(response.Value.CertificateAuthority);
        Assert.Equal("-----BEGIN PUBLIC KEY-----\nFakePublicKeyTextEAAQ==\n-----END PUBLIC KEY-----\n", response.Value.Certificate);
        Assert.Equal("-----BEGIN RSA PRIVATE KEY-----\nFakePrivateKeyTextEAAQ==\n-----END RSA PRIVATE KEY-----\n", response.Value.PrivateKey);
    }

    [Fact]
    public async Task WriteAsync_Sets_NonRootCertificate()
    {
        MockHttpMessageHandler mockHttpMessageHandler = InitializedHandlerWithLogin();

        MockedRequest mockRequest = mockHttpMessageHandler.Expect(HttpMethod.Put, $"{CredHubBase}/v1/data").Respond("application/json",
            "{\"type\":\"certificate\",\"version_created_at\":\"2017-11-10T15:55:24Z\",\"id\":\"657dd2f0-c2e4-4e28-b84a-171a730916b2\",\"name\":\"/example-certificate\",\"value\":{\"ca\":\"-----BEGIN PUBLIC KEY-----\\nFakeCAKeyTextEAAQ==\\n-----END PUBLIC KEY-----\\n\",\"certificate\":\"-----BEGIN PUBLIC KEY-----\\nFakePublicKeyTextEAAQ==\\n-----END PUBLIC KEY-----\\n\",\"private_key\":\"-----BEGIN RSA PRIVATE KEY-----\\nFakePrivateKeyTextEAAQ==\\n-----END RSA PRIVATE KEY-----\\n\"}}");

        CredHubClient client = await InitializeClientAsync(mockHttpMessageHandler);
        const string certificateAuthority = "-----BEGIN PUBLIC KEY-----\nFakeCAKeyTextEAAQ==\n-----END PUBLIC KEY-----\n";
        const string privateKey = "-----BEGIN RSA PRIVATE KEY-----\nFakePrivateKeyTextEAAQ==\n-----END RSA PRIVATE KEY-----\n";
        const string certificate = "-----BEGIN PUBLIC KEY-----\nFakePublicKeyTextEAAQ==\n-----END PUBLIC KEY-----\n";

        CredHubCredential<CertificateCredential> response =
            await client.WriteAsync<CertificateCredential>(new CertificateSetRequest("example-certificate", privateKey, certificate, certificateAuthority));

        mockHttpMessageHandler.VerifyNoOutstandingExpectation();
        Assert.Equal(1, mockHttpMessageHandler.GetMatchCount(mockRequest));
        Assert.Equal(CredentialType.Certificate, response.Type);
        Assert.Equal(new DateTime(2017, 11, 10, 15, 55, 24, DateTimeKind.Utc), response.VersionCreatedAt);
        Assert.Equal(Guid.Parse("657dd2f0-c2e4-4e28-b84a-171a730916b2"), response.Id);
        Assert.Equal("/example-certificate", response.Name);
        Assert.Equal("-----BEGIN PUBLIC KEY-----\nFakeCAKeyTextEAAQ==\n-----END PUBLIC KEY-----\n", response.Value.CertificateAuthority);
        Assert.Equal("-----BEGIN PUBLIC KEY-----\nFakePublicKeyTextEAAQ==\n-----END PUBLIC KEY-----\n", response.Value.Certificate);
        Assert.Equal("-----BEGIN RSA PRIVATE KEY-----\nFakePrivateKeyTextEAAQ==\n-----END RSA PRIVATE KEY-----\n", response.Value.PrivateKey);
    }

    [Fact]
    public async Task WriteAsync_Sets_RSA()
    {
        MockHttpMessageHandler mockHttpMessageHandler = InitializedHandlerWithLogin();

        MockedRequest mockRequest = mockHttpMessageHandler.Expect(HttpMethod.Put, $"{CredHubBase}/v1/data").Respond("application/json",
            "{\"type\":\"rsa\",\"version_created_at\":\"2017-11-10T15:55:24Z\",\"id\":\"2af5191f-9c05-4746-b72c-78b3283aef46\",\"name\":\"/example-rsa\",\"value\":{\"public_key\":\"-----BEGIN PUBLIC KEY-----\\nFakePublicKeyTextEAAQ==\\n-----END PUBLIC KEY-----\\n\",\"private_key\":\"-----BEGIN RSA PRIVATE KEY-----\\nFakePrivateKeyTextEAAQ==\\n-----END RSA PRIVATE KEY-----\\n\"}}");

        CredHubClient client = await InitializeClientAsync(mockHttpMessageHandler);
        const string privateKey = "-----BEGIN RSA PRIVATE KEY-----\nFakePrivateKeyTextEAAQ==\n-----END RSA PRIVATE KEY-----\n";
        const string publicKey = "-----BEGIN PUBLIC KEY-----\nFakePublicKeyTextEAAQ==\n-----END PUBLIC KEY-----\n";

        CredHubCredential<RsaCredential> response = await client.WriteAsync<RsaCredential>(new RsaSetRequest("example-rsa", privateKey, publicKey));

        mockHttpMessageHandler.VerifyNoOutstandingExpectation();
        Assert.Equal(1, mockHttpMessageHandler.GetMatchCount(mockRequest));
        Assert.Equal(CredentialType.RSA, response.Type);
        Assert.Equal(new DateTime(2017, 11, 10, 15, 55, 24, DateTimeKind.Utc), response.VersionCreatedAt);
        Assert.Equal(Guid.Parse("2af5191f-9c05-4746-b72c-78b3283aef46"), response.Id);
        Assert.Equal("/example-rsa", response.Name);
        Assert.Equal("-----BEGIN PUBLIC KEY-----\nFakePublicKeyTextEAAQ==\n-----END PUBLIC KEY-----\n", response.Value.PublicKey);
        Assert.Equal("-----BEGIN RSA PRIVATE KEY-----\nFakePrivateKeyTextEAAQ==\n-----END RSA PRIVATE KEY-----\n", response.Value.PrivateKey);
    }

    [Fact]
    public async Task WriteAsync_Sets_SSH()
    {
        MockHttpMessageHandler mockHttpMessageHandler = InitializedHandlerWithLogin();

        MockedRequest mockRequest = mockHttpMessageHandler.Expect(HttpMethod.Put, $"{CredHubBase}/v1/data").Respond("application/json",
            "{\"type\":\"ssh\",\"version_created_at\":\"2017-11-10T15:55:24Z\",\"id\":\"2af5191f-9c05-4746-b72c-78b3283aef46\",\"name\":\"/example-ssh\",\"value\":{\"public_key\":\"-----BEGIN PUBLIC KEY-----\\nFakePublicKeyTextEAAQ==\\n-----END PUBLIC KEY-----\\n\",\"private_key\":\"-----BEGIN RSA PRIVATE KEY-----\\nFakePrivateKeyTextEAAQ==\\n-----END RSA PRIVATE KEY-----\\n\"}}");

        CredHubClient client = await InitializeClientAsync(mockHttpMessageHandler);
        const string privateKey = "-----BEGIN RSA PRIVATE KEY-----\nFakePrivateKeyTextEAAQ==\n-----END RSA PRIVATE KEY-----\n";
        const string publicKey = "-----BEGIN PUBLIC KEY-----\nFakePublicKeyTextEAAQ==\n-----END PUBLIC KEY-----\n";

        CredHubCredential<SshCredential> response = await client.WriteAsync<SshCredential>(new SshSetRequest("example-ssh", privateKey, publicKey));

        mockHttpMessageHandler.VerifyNoOutstandingExpectation();
        Assert.Equal(1, mockHttpMessageHandler.GetMatchCount(mockRequest));
        Assert.Equal(CredentialType.SSH, response.Type);
        Assert.Equal(new DateTime(2017, 11, 10, 15, 55, 24, DateTimeKind.Utc), response.VersionCreatedAt);
        Assert.Equal(Guid.Parse("2af5191f-9c05-4746-b72c-78b3283aef46"), response.Id);
        Assert.Equal("/example-ssh", response.Name);
        Assert.Equal("-----BEGIN PUBLIC KEY-----\nFakePublicKeyTextEAAQ==\n-----END PUBLIC KEY-----\n", response.Value.PublicKey);
        Assert.Equal("-----BEGIN RSA PRIVATE KEY-----\nFakePrivateKeyTextEAAQ==\n-----END RSA PRIVATE KEY-----\n", response.Value.PrivateKey);
    }

    [Fact]
    public async Task GetByIdAsync_Gets()
    {
        Guid credId = Guid.Parse("f82cc4a6-4490-4ed7-92c9-5115006bd691");
        MockHttpMessageHandler mockHttpMessageHandler = InitializedHandlerWithLogin();

        MockedRequest mockRequest = mockHttpMessageHandler.Expect(HttpMethod.Get, $"{CredHubBase}v1/data/{credId}").Respond("application/json",
            "{\"type\":\"ssh\",\"version_created_at\":\"2017-11-20T17:37:57Z\",\"id\":\"f82cc4a6-4490-4ed7-92c9-5115006bd691\",\"name\":\"/example-ssh\",\"value\":{\"public_key\":\"ssh-rsa FakePublicKeyText\",\"private_key\":\"-----BEGIN RSA PRIVATE KEY-----\\nFakePrivateKeyText\\n-----END RSA PRIVATE KEY-----\\n\",\"public_key_fingerprint\":\"mkiqcOCEUhYsp/0Uu5ZsJlLkKt74/lV4Yz/FKslHxR8\"}}");

        CredHubClient client = await InitializeClientAsync(mockHttpMessageHandler);

        CredHubCredential<SshCredential> response = await client.GetByIdAsync<SshCredential>(credId);

        Assert.Equal(1, mockHttpMessageHandler.GetMatchCount(mockRequest));
        Assert.Equal(CredentialType.SSH, response.Type);
        Assert.Equal(new DateTime(2017, 11, 20, 17, 37, 57, DateTimeKind.Utc), response.VersionCreatedAt);
        Assert.Equal(credId, response.Id);
        Assert.Equal("/example-ssh", response.Name);
        Assert.Equal("ssh-rsa FakePublicKeyText", response.Value.PublicKey);
        Assert.Equal("-----BEGIN RSA PRIVATE KEY-----\nFakePrivateKeyText\n-----END RSA PRIVATE KEY-----\n", response.Value.PrivateKey);
        Assert.Equal("mkiqcOCEUhYsp/0Uu5ZsJlLkKt74/lV4Yz/FKslHxR8", response.Value.PublicKeyFingerprint);
    }

    [Fact]
    public async Task GetByNameAsync_Gets()
    {
        MockHttpMessageHandler mockHttpMessageHandler = InitializedHandlerWithLogin();

        MockedRequest mockRequest = mockHttpMessageHandler.Expect(HttpMethod.Get, $"{CredHubBase}/v1/data?name=/example-rsa").Respond("application/json",
            "{\"data\":[{\"type\":\"rsa\",\"version_created_at\":\"2017-11-10T15:55:24Z\",\"id\":\"2af5191f-9c05-4746-b72c-78b3283aef46\",\"name\":\"/example-rsa\",\"value\":{\"public_key\":\"-----BEGIN PUBLIC KEY-----\\nFakePublicKeyTextEAAQ==\\n-----END PUBLIC KEY-----\\n\",\"private_key\":\"-----BEGIN RSA PRIVATE KEY-----\\nFakePrivateKeyTextEAAQ==\\n-----END RSA PRIVATE KEY-----\\n\"}}]}");

        CredHubClient client = await InitializeClientAsync(mockHttpMessageHandler);

        CredHubCredential<RsaCredential> response = await client.GetByNameAsync<RsaCredential>("/example-rsa");

        mockHttpMessageHandler.VerifyNoOutstandingExpectation();
        Assert.Equal(1, mockHttpMessageHandler.GetMatchCount(mockRequest));
        Assert.Equal(CredentialType.RSA, response.Type);
        Assert.Equal(new DateTime(2017, 11, 10, 15, 55, 24, DateTimeKind.Utc), response.VersionCreatedAt);
        Assert.Equal(Guid.Parse("2af5191f-9c05-4746-b72c-78b3283aef46"), response.Id);
        Assert.Equal("/example-rsa", response.Name);
        Assert.Equal("-----BEGIN PUBLIC KEY-----\nFakePublicKeyTextEAAQ==\n-----END PUBLIC KEY-----\n", response.Value.PublicKey);
        Assert.Equal("-----BEGIN RSA PRIVATE KEY-----\nFakePrivateKeyTextEAAQ==\n-----END RSA PRIVATE KEY-----\n", response.Value.PrivateKey);
    }

    [Fact]
    public async Task GetByNameAsync_Throws_WithoutName()
    {
        var client = new CredHubClient();

        await Assert.ThrowsAsync<ArgumentException>(() => client.GetByNameAsync<ValueCredential>(string.Empty));
    }

    [Fact]
    public async Task GetByNameWithHistoryAsync_Gets()
    {
        const int revisionCount = 3;
        MockHttpMessageHandler mockHttpMessageHandler = InitializedHandlerWithLogin();

        MockedRequest mockRequest = mockHttpMessageHandler.Expect(HttpMethod.Get, $"{CredHubBase}/v1/data?name=/example&versions={revisionCount}").Respond(
            "application/json",
            "{\"data\":[{\"type\":\"value\",\"version_created_at\":\"2017-11-20T23:03:32Z\",\"id\":\"2af5191f-9c05-4746-b72c-78b3283aef43\",\"name\":\"/example\",\"value\":\"Value example 3\"},{\"type\":\"value\",\"version_created_at\":\"2017-11-20T23:03:28Z\",\"id\":\"2af5191f-9c05-4746-b72c-78b3283aef42\",\"name\":\"/example\",\"value\":\"Value example 2\"},{\"type\":\"value\",\"version_created_at\":\"2017-11-20T23:03:22Z\",\"id\":\"2af5191f-9c05-4746-b72c-78b3283aef41\",\"name\":\"/example\",\"value\":\"Value example 1\"}]}");

        CredHubClient client = await InitializeClientAsync(mockHttpMessageHandler);

        List<CredHubCredential<string>> response = await client.GetByNameWithHistoryAsync<string>("/example", revisionCount);

        mockHttpMessageHandler.VerifyNoOutstandingExpectation();
        Assert.Equal(1, mockHttpMessageHandler.GetMatchCount(mockRequest));
        int index = 3;

        foreach (CredHubCredential<string> item in response)
        {
            Assert.Equal(CredentialType.Value, item.Type);
            Assert.Equal(Guid.Parse($"2af5191f-9c05-4746-b72c-78b3283aef4{index}"), item.Id);
            Assert.Equal("/example", item.Name);
            Assert.Equal($"Value example {index}", item.Value);
            index--;
        }
    }

    [Fact]
    public async Task GenerateAsync_Creates_Password()
    {
        MockHttpMessageHandler mockHttpMessageHandler = InitializedHandlerWithLogin();

        MockedRequest mockRequest = mockHttpMessageHandler.Expect(HttpMethod.Post, $"{CredHubBase}/v1/data")
            .WithContent("{\"mode\":\"converge\",\"parameters\":{\"length\":40},\"name\":\"generated-password\",\"type\":\"Password\"}").Respond(
                "application/json",
                "{\"type\":\"password\",\"version_created_at\":\"2017-11-21T18:18:28Z\",\"id\":\"1a129eff-f467-42bc-b959-772f4dec1f5e\",\"name\":\"/generated-password\",\"value\":\"W9VwGfI3gvV0ypMDUaFvYDnui84elZPtfGaKaILO\"}");

        CredHubClient client = await InitializeClientAsync(mockHttpMessageHandler);

        var request = new PasswordGenerationRequest("generated-password", new PasswordGenerationParameters
        {
            Length = 40
        });

        CredHubCredential<PasswordCredential> response = await client.GenerateAsync<PasswordCredential>(request);

        mockHttpMessageHandler.VerifyNoOutstandingExpectation();
        Assert.Equal(1, mockHttpMessageHandler.GetMatchCount(mockRequest));
        Assert.Equal(CredentialType.Password, response.Type);
        Assert.Equal(new DateTime(2017, 11, 21, 18, 18, 28, DateTimeKind.Utc), response.VersionCreatedAt);
        Assert.Equal(Guid.Parse("1a129eff-f467-42bc-b959-772f4dec1f5e"), response.Id);
        Assert.Equal("/generated-password", response.Name);
        Assert.Equal("W9VwGfI3gvV0ypMDUaFvYDnui84elZPtfGaKaILO", response.Value.ToString());
    }

    [Fact]
    public async Task GenerateAsync_Creates_User()
    {
        MockHttpMessageHandler mockHttpMessageHandler = InitializedHandlerWithLogin();

        MockedRequest mockRequest = mockHttpMessageHandler.Expect(HttpMethod.Post, $"{CredHubBase}/v1/data")
            .WithContent("{\"mode\":\"converge\",\"parameters\":{\"length\":40},\"name\":\"generated-user\",\"type\":\"User\"}").Respond("application/json",
                "{\"type\":\"user\",\"version_created_at\":\"2017-11-21T18:18:28Z\",\"id\":\"1a129eff-f467-42bc-b959-772f4dec1f5e\",\"name\":\"/generated-user\",\"value\":{\"username\":\"HzFFMbHuRbtImAWdGmML\",\"password\":\"zVNmqtSHakqRCMb2OtUFtwnoOSJ0T4NCSaaYdIku\",\"password_hash\":\"$6$8Oq5Fmmr$dVjMXUCk.r9I6jpQYapnwtoK80qrpSSCBqezyeB7AFJFPvQQy.tw0LBHSBJjaT9L9W3u1nodrDol8U.knd17y0\"}}");

        CredHubClient client = await InitializeClientAsync(mockHttpMessageHandler);

        var request = new UserGenerationRequest("generated-user", new UserGenerationParameters
        {
            Length = 40
        });

        CredHubCredential<UserCredential> response = await client.GenerateAsync<UserCredential>(request);

        mockHttpMessageHandler.VerifyNoOutstandingExpectation();
        Assert.Equal(1, mockHttpMessageHandler.GetMatchCount(mockRequest));
        Assert.Equal(CredentialType.User, response.Type);
        Assert.Equal(new DateTime(2017, 11, 21, 18, 18, 28, DateTimeKind.Utc), response.VersionCreatedAt);
        Assert.Equal(Guid.Parse("1a129eff-f467-42bc-b959-772f4dec1f5e"), response.Id);
        Assert.Equal("/generated-user", response.Name);
        Assert.Equal("HzFFMbHuRbtImAWdGmML", response.Value.Username);
        Assert.Equal("zVNmqtSHakqRCMb2OtUFtwnoOSJ0T4NCSaaYdIku", response.Value.Password);
        Assert.Equal("$6$8Oq5Fmmr$dVjMXUCk.r9I6jpQYapnwtoK80qrpSSCBqezyeB7AFJFPvQQy.tw0LBHSBJjaT9L9W3u1nodrDol8U.knd17y0", response.Value.PasswordHash);
    }

    [Fact]
    public async Task GenerateAsync_Creates_Certificate()
    {
        MockHttpMessageHandler mockHttpMessageHandler = InitializedHandlerWithLogin();

        MockedRequest mockRequest = mockHttpMessageHandler.Expect(HttpMethod.Post, $"{CredHubBase}/v1/data")
            .WithContent(
                "{\"mode\":\"converge\",\"parameters\":{\"common_name\":\"TestCA\",\"duration\":365,\"is_ca\":true,\"self_sign\":false,\"key_length\":2048},\"name\":\"example-ca\",\"type\":\"Certificate\"}")
            .Respond("application/json",
                "{\"type\":\"certificate\",\"transitional\":false,\"version_created_at\":\"2017-11-20T15:55:24Z\",\"id\":\"0d698309-cca6-4626-aae3-a72ed664304a\",\"name\":\"/example-ca\",\"value\":{\"ca\":null,\"certificate\":\"-----BEGIN CERTIFICATE-----\\nFakeCertificateText\\n-----END CERTIFICATE-----\\n\",\"private_key\":\"-----BEGIN RSA PRIVATE KEY-----\\nFakePrivateKeyTextEAAQ==\\n-----END RSA PRIVATE KEY-----\\n\"}}");

        CredHubClient client = await InitializeClientAsync(mockHttpMessageHandler);

        var parameters = new CertificateGenerationParameters
        {
            CommonName = "TestCA",
            IsCertificateAuthority = true
        };

        var request = new CertificateGenerationRequest("example-ca", parameters);

        CredHubCredential<CertificateCredential> response = await client.GenerateAsync<CertificateCredential>(request);

        mockHttpMessageHandler.VerifyNoOutstandingExpectation();
        Assert.Equal(1, mockHttpMessageHandler.GetMatchCount(mockRequest));
        Assert.Equal(CredentialType.Certificate, response.Type);
        Assert.Equal(new DateTime(2017, 11, 20, 15, 55, 24, DateTimeKind.Utc), response.VersionCreatedAt);
        Assert.Equal(Guid.Parse("0d698309-cca6-4626-aae3-a72ed664304a"), response.Id);
        Assert.Equal("/example-ca", response.Name);
        Assert.Null(response.Value.CertificateAuthority);
        Assert.Equal("-----BEGIN CERTIFICATE-----\nFakeCertificateText\n-----END CERTIFICATE-----\n", response.Value.Certificate);
        Assert.Equal("-----BEGIN RSA PRIVATE KEY-----\nFakePrivateKeyTextEAAQ==\n-----END RSA PRIVATE KEY-----\n", response.Value.PrivateKey);
    }

    [Fact]
    public async Task GenerateAsync_Creates_RSA()
    {
        MockHttpMessageHandler mockHttpMessageHandler = InitializedHandlerWithLogin();

        MockedRequest mockRequest = mockHttpMessageHandler.Expect(HttpMethod.Post, $"{CredHubBase}/v1/data")
            .WithContent("{\"mode\":\"converge\",\"parameters\":{\"key_length\":2048},\"name\":\"example-rsa\",\"type\":\"RSA\"}").Respond("application/json",
                "{\"type\":\"rsa\",\"version_created_at\":\"2017-11-10T15:55:24Z\",\"id\":\"2af5191f-9c05-4746-b72c-78b3283aef46\",\"name\":\"/example-rsa\",\"value\":{\"public_key\":\"-----BEGIN PUBLIC KEY-----\\nFakePublicKeyTextEAAQ==\\n-----END PUBLIC KEY-----\\n\",\"private_key\":\"-----BEGIN RSA PRIVATE KEY-----\\nFakePrivateKeyTextEAAQ==\\n-----END RSA PRIVATE KEY-----\\n\"}}");

        CredHubClient client = await InitializeClientAsync(mockHttpMessageHandler);
        var request = new RsaGenerationRequest("example-rsa");

        CredHubCredential<RsaCredential> response = await client.GenerateAsync<RsaCredential>(request);

        mockHttpMessageHandler.VerifyNoOutstandingExpectation();
        Assert.Equal(1, mockHttpMessageHandler.GetMatchCount(mockRequest));
        Assert.Equal(CredentialType.RSA, response.Type);
        Assert.Equal(new DateTime(2017, 11, 10, 15, 55, 24, DateTimeKind.Utc), response.VersionCreatedAt);
        Assert.Equal(Guid.Parse("2af5191f-9c05-4746-b72c-78b3283aef46"), response.Id);
        Assert.Equal("/example-rsa", response.Name);
        Assert.Equal("-----BEGIN PUBLIC KEY-----\nFakePublicKeyTextEAAQ==\n-----END PUBLIC KEY-----\n", response.Value.PublicKey);
        Assert.Equal("-----BEGIN RSA PRIVATE KEY-----\nFakePrivateKeyTextEAAQ==\n-----END RSA PRIVATE KEY-----\n", response.Value.PrivateKey);
    }

    [Fact]
    public async Task GenerateAsync_Creates_SSH()
    {
        MockHttpMessageHandler mockHttpMessageHandler = InitializedHandlerWithLogin();

        MockedRequest mockRequest = mockHttpMessageHandler.Expect(HttpMethod.Post, $"{CredHubBase}/v1/data")
            .WithContent("{\"mode\":\"converge\",\"parameters\":{\"key_length\":2048},\"name\":\"example-ssh\",\"type\":\"SSH\"}").Respond("application/json",
                "{\"type\":\"ssh\",\"version_created_at\":\"2017-11-10T15:55:24Z\",\"id\":\"2af5191f-9c05-4746-b72c-78b3283aef46\",\"name\":\"/example-ssh\",\"value\":{\"public_key\":\"ssh-rsa FakePublicKeyText asdf\",\"private_key\":\"-----BEGIN RSA PRIVATE KEY-----\\nFakePrivateKeyTextEAAQ==\\n-----END RSA PRIVATE KEY-----\\n\",\"public_key_fingerprint\":\"mkiqcOCEUhYsp/0Uu5ZsJlLkKt74/lV4Yz/FKslHxR8\"}}");

        CredHubClient client = await InitializeClientAsync(mockHttpMessageHandler);
        var request = new SshGenerationRequest("example-ssh");

        CredHubCredential<SshCredential> response = await client.GenerateAsync<SshCredential>(request);

        mockHttpMessageHandler.VerifyNoOutstandingExpectation();
        Assert.Equal(1, mockHttpMessageHandler.GetMatchCount(mockRequest));
        Assert.Equal(CredentialType.SSH, response.Type);
        Assert.Equal(new DateTime(2017, 11, 10, 15, 55, 24, DateTimeKind.Utc), response.VersionCreatedAt);
        Assert.Equal(Guid.Parse("2af5191f-9c05-4746-b72c-78b3283aef46"), response.Id);
        Assert.Equal("/example-ssh", response.Name);
        Assert.Equal("ssh-rsa FakePublicKeyText asdf", response.Value.PublicKey);
        Assert.Equal("-----BEGIN RSA PRIVATE KEY-----\nFakePrivateKeyTextEAAQ==\n-----END RSA PRIVATE KEY-----\n", response.Value.PrivateKey);
        Assert.Equal("mkiqcOCEUhYsp/0Uu5ZsJlLkKt74/lV4Yz/FKslHxR8", response.Value.PublicKeyFingerprint);
    }

    [Fact]
    public async Task RegenerateAsync_Regenerates_Password()
    {
        MockHttpMessageHandler mockHttpMessageHandler = InitializedHandlerWithLogin();

        MockedRequest mockRequest = mockHttpMessageHandler.Expect(HttpMethod.Post, $"{CredHubBase}/v1/regenerate")
            .WithContent("{\"name\":\"generated-password\"}").Respond("application/json",
                "{\"type\":\"password\",\"version_created_at\":\"2017-11-21T18:18:28Z\",\"id\":\"1a129eff-f467-42bc-b959-772f4dec1f5e\",\"name\":\"/generated-password\",\"value\":\"W9VwGfI3gvV0ypMDUaFvYDnui84elZPtfGaKaILO\"}");

        CredHubClient client = await InitializeClientAsync(mockHttpMessageHandler);

        CredHubCredential<PasswordCredential> response = await client.RegenerateAsync<PasswordCredential>("generated-password");

        mockHttpMessageHandler.VerifyNoOutstandingExpectation();
        Assert.Equal(1, mockHttpMessageHandler.GetMatchCount(mockRequest));
        Assert.Equal(CredentialType.Password, response.Type);
        Assert.Equal(new DateTime(2017, 11, 21, 18, 18, 28, DateTimeKind.Utc), response.VersionCreatedAt);
        Assert.Equal(Guid.Parse("1a129eff-f467-42bc-b959-772f4dec1f5e"), response.Id);
        Assert.Equal("/generated-password", response.Name);
        Assert.Equal("W9VwGfI3gvV0ypMDUaFvYDnui84elZPtfGaKaILO", response.Value.ToString());
    }

    [Fact]
    public async Task RegenerateAsync_Regenerates_RSA()
    {
        MockHttpMessageHandler mockHttpMessageHandler = InitializedHandlerWithLogin();

        MockedRequest mockRequest = mockHttpMessageHandler.Expect(HttpMethod.Post, $"{CredHubBase}/v1/regenerate").WithContent("{\"name\":\"regenerated-rsa\"}")
            .Respond("application/json",
                "{\"type\":\"rsa\",\"version_created_at\":\"2017-11-21T18:18:28Z\",\"id\":\"1a129eff-f467-42bc-b959-772f4dec1f5e\",\"name\":\"/regenerated-rsa\",\"value\":{\"public_key\":\"-----BEGIN PUBLIC KEY-----\\nFakePublicKeyTextEAAQ==\\n-----END PUBLIC KEY-----\\n\",\"private_key\":\"-----BEGIN RSA PRIVATE KEY-----\\nFakePrivateKeyTextEAAQ==\\n-----END RSA PRIVATE KEY-----\\n\"}}");

        CredHubClient client = await InitializeClientAsync(mockHttpMessageHandler);

        CredHubCredential<RsaCredential> response = await client.RegenerateAsync<RsaCredential>("regenerated-rsa");

        mockHttpMessageHandler.VerifyNoOutstandingExpectation();
        Assert.Equal(1, mockHttpMessageHandler.GetMatchCount(mockRequest));
        Assert.Equal(CredentialType.RSA, response.Type);
        Assert.Equal(new DateTime(2017, 11, 21, 18, 18, 28, DateTimeKind.Utc), response.VersionCreatedAt);
        Assert.Equal(Guid.Parse("1a129eff-f467-42bc-b959-772f4dec1f5e"), response.Id);
        Assert.Equal("/regenerated-rsa", response.Name);
        Assert.Equal("-----BEGIN PUBLIC KEY-----\nFakePublicKeyTextEAAQ==\n-----END PUBLIC KEY-----\n", response.Value.PublicKey);
        Assert.Equal("-----BEGIN RSA PRIVATE KEY-----\nFakePrivateKeyTextEAAQ==\n-----END RSA PRIVATE KEY-----\n", response.Value.PrivateKey);
    }

    [Fact]
    public async Task RegenerateAsync_Throws_WithoutName()
    {
        var client = new CredHubClient();

        await Assert.ThrowsAsync<ArgumentException>(() => client.RegenerateAsync<PasswordCredential>(string.Empty));
    }

    [Fact]
    public async Task BulkRegenerateAsync_Regenerates_Certificates()
    {
        MockHttpMessageHandler mockHttpMessageHandler = InitializedHandlerWithLogin();

        MockedRequest mockRequest = mockHttpMessageHandler.Expect(HttpMethod.Post, $"{CredHubBase}/v1/bulk-regenerate")
            .WithContent("{\"signed_by\":\"example-ca\"}")
            .Respond("application/json", "{\"regenerated_credentials\":[\"/example-certificate3\",\"/example-certificate2\"]}");

        CredHubClient client = await InitializeClientAsync(mockHttpMessageHandler);

        RegeneratedCertificates response = await client.BulkRegenerateAsync("example-ca");

        mockHttpMessageHandler.VerifyNoOutstandingExpectation();
        Assert.Equal(1, mockHttpMessageHandler.GetMatchCount(mockRequest));
        Assert.Equal(2, response.RegeneratedCredentials.Count);
        Assert.Equal("/example-certificate3", response.RegeneratedCredentials.First());
    }

    [Fact]
    public async Task BulkRegenerateAsync_Throws_WithoutCA()
    {
        var client = new CredHubClient();

        await Assert.ThrowsAsync<ArgumentException>(() => client.BulkRegenerateAsync(string.Empty));
    }

    [Fact]
    public async Task DeleteByNameAsync_ReturnsTrue_WhenCredDeleted()
    {
        MockHttpMessageHandler mockHttpMessageHandler = InitializedHandlerWithLogin();
        mockHttpMessageHandler.Expect(HttpMethod.Delete, $"{CredHubBase}/v1/data?name=/example-rsa").Respond(HttpStatusCode.NoContent);
        CredHubClient client = await InitializeClientAsync(mockHttpMessageHandler);

        bool response = await client.DeleteByNameAsync("/example-rsa");

        Assert.True(response);
    }

    [Fact]
    public async Task DeleteByNameAsync_ReturnsTrue_WhenCredNotFound()
    {
        MockHttpMessageHandler mockHttpMessageHandler = InitializedHandlerWithLogin();
        mockHttpMessageHandler.Expect(HttpMethod.Delete, $"{CredHubBase}/v1/data?name=/example-rsa").Respond(HttpStatusCode.NotFound);
        CredHubClient client = await InitializeClientAsync(mockHttpMessageHandler);

        bool response = await client.DeleteByNameAsync("/example-rsa");

        Assert.True(response);
    }

    [Fact]
    public async Task DeleteByNameAsync_Throws_WithoutName()
    {
        var client = new CredHubClient();

        await Assert.ThrowsAsync<ArgumentException>(() => client.DeleteByNameAsync(string.Empty));
    }

    [Fact]
    public async Task FindByNameAsync_Returns_Credentials_WithQuery()
    {
        MockHttpMessageHandler mockHttpMessageHandler = InitializedHandlerWithLogin();

        MockedRequest mockRequest = mockHttpMessageHandler.Expect(HttpMethod.Get, $"{CredHubBase}/v1/data?name-like=example").Respond("application/json",
            "{\"credentials\":[{\"version_created_at\":\"2017-11-21T20:39:59Z\",\"name\":\"/example-certificate\"},{\"version_created_at\":\"2017-11-21T18:59:26Z\",\"name\":\"/example-user\"},{\"version_created_at\":\"2017-11-20T22:40:00Z\",\"name\":\"/example-ssh\"}]}");

        CredHubClient client = await InitializeClientAsync(mockHttpMessageHandler);

        List<FoundCredential> response = await client.FindByNameAsync("example");

        mockHttpMessageHandler.VerifyNoOutstandingExpectation();
        Assert.Equal(1, mockHttpMessageHandler.GetMatchCount(mockRequest));
        Assert.Equal(3, response.Count);
        Assert.Contains(response, i => i.Name == "/example-certificate");
        Assert.Contains(response, i => i.Name == "/example-user");
        Assert.Contains(response, i => i.Name == "/example-ssh");
    }

    [Fact]
    public async Task FindByNameAsync_Throws_WithoutQuery()
    {
        var client = new CredHubClient();

        await Assert.ThrowsAsync<ArgumentException>(() => client.FindByNameAsync(string.Empty));
    }

    [Fact]
    public async Task FindByPathAsync_Returns_Credentials_WithQuery()
    {
        MockHttpMessageHandler mockHttpMessageHandler = InitializedHandlerWithLogin();

        MockedRequest mockRequest = mockHttpMessageHandler.Expect(HttpMethod.Get, $"{CredHubBase}/v1/data?path=/").Respond("application/json",
            "{\"credentials\":[{\"version_created_at\":\"2017-11-21T20:39:59Z\",\"name\":\"/example-certificate\"},{\"version_created_at\":\"2017-11-21T18:59:26Z\",\"name\":\"/example-user\"},{\"version_created_at\":\"2017-11-20T22:40:00Z\",\"name\":\"/example-ssh\"}]}");

        CredHubClient client = await InitializeClientAsync(mockHttpMessageHandler);

        List<FoundCredential> response = await client.FindByPathAsync("/");

        mockHttpMessageHandler.VerifyNoOutstandingExpectation();
        Assert.Equal(1, mockHttpMessageHandler.GetMatchCount(mockRequest));
        Assert.Equal(3, response.Count);
        Assert.Contains(response, i => i.Name == "/example-certificate");
        Assert.Contains(response, i => i.Name == "/example-user");
        Assert.Contains(response, i => i.Name == "/example-ssh");
    }

    [Fact]
    public async Task FindByPathAsync_Throws_WithoutQuery()
    {
        var client = new CredHubClient();

        await Assert.ThrowsAsync<ArgumentException>(() => client.FindByPathAsync(string.Empty));
    }

    [Fact]
    public async Task GetPermissionsAsync_Throws_WithoutName()
    {
        var client = new CredHubClient();

        await Assert.ThrowsAsync<ArgumentException>(() => client.GetPermissionsAsync(string.Empty));
    }

    [Fact]
    public async Task GetPermissionsAsync_ReturnsPermittedActors()
    {
        MockHttpMessageHandler mockHttpMessageHandler = InitializedHandlerWithLogin();

        MockedRequest mockRequest = mockHttpMessageHandler.Expect(HttpMethod.Get, $"{CredHubBase}/v1/permissions?credential_name=/example-password").Respond(
            "application/json",
            "{\"credential_name\":\"/example-password\",\"permissions\":[{\"actor\":\"uaa-user:credhub_client\",\"operations\":[\"read\",\"write\",\"delete\",\"read_acl\",\"write_acl\"]}]}");

        CredHubClient client = await InitializeClientAsync(mockHttpMessageHandler);

        List<CredentialPermission> response = await client.GetPermissionsAsync("/example-password");

        mockHttpMessageHandler.VerifyNoOutstandingExpectation();
        Assert.Equal(1, mockHttpMessageHandler.GetMatchCount(mockRequest));
        Assert.Single(response);
        CredentialPermission permission = response.First();
        Assert.Equal("uaa-user:credhub_client", permission.Actor);
        Assert.Equal(5, permission.Operations.Count);
        Assert.Contains(OperationPermissions.Read, permission.Operations);
        Assert.Contains(OperationPermissions.Write, permission.Operations);
        Assert.Contains(OperationPermissions.ReadAcl, permission.Operations);
        Assert.Contains(OperationPermissions.WriteAcl, permission.Operations);
        Assert.Contains(OperationPermissions.Delete, permission.Operations);
    }

    [Fact]
    public async Task AddPermissionsAsync_Throws_WithoutName()
    {
        var client = new CredHubClient();

        await Assert.ThrowsAsync<ArgumentException>(() => client.AddPermissionsAsync(string.Empty, null));
    }

    [Fact]
    public async Task AddPermissionsAsync_Throws_WithNoPermissions()
    {
        var client = new CredHubClient();

        await Assert.ThrowsAsync<ArgumentException>(() => client.AddPermissionsAsync("user", new List<CredentialPermission>()));
    }

    [Fact]
    public async Task AddPermissionsAsync_Throws_WithNullPermissions()
    {
        var client = new CredHubClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.AddPermissionsAsync("user", null));
    }

    [Fact]
    public async Task AddPermissionsAsync_AddsAndReturnsPermissions()
    {
        const string credentialName = "/generated-password";
        MockHttpMessageHandler mockHttpMessageHandler = InitializedHandlerWithLogin();

        MockedRequest mockAddRequest = mockHttpMessageHandler.Expect(HttpMethod.Post, $"{CredHubBase}/v1/permissions")
            .WithContent(
                $"{{\"credential_name\":\"{credentialName}\",\"permissions\":[{{\"actor\":\"uaa-user:credhub_client\",\"operations\":[\"read\",\"write\",\"delete\"]}}]}}")
            .Respond(HttpStatusCode.Created);

        MockedRequest mockVerifyRequest = mockHttpMessageHandler.Expect(HttpMethod.Get, $"{CredHubBase}/v1/permissions?credential_name={credentialName}")
            .Respond("application/json",
                $"{{\"credential_name\":\"{credentialName}\",\"permissions\":[{{\"actor\":\"uaa-user:credhub_client\",\"operations\":[\"read\",\"write\",\"delete\"]}}]}}");

        CredHubClient client = await InitializeClientAsync(mockHttpMessageHandler);

        var newPermissions = new CredentialPermission
        {
            Actor = "uaa-user:credhub_client",
            Operations = new List<OperationPermissions>
            {
                OperationPermissions.Read,
                OperationPermissions.Write,
                OperationPermissions.Delete
            }
        };

        List<CredentialPermission> response = await client.AddPermissionsAsync(credentialName, new List<CredentialPermission>
        {
            newPermissions
        });

        mockHttpMessageHandler.VerifyNoOutstandingExpectation();
        Assert.Equal(1, mockHttpMessageHandler.GetMatchCount(mockAddRequest));
        Assert.Equal(1, mockHttpMessageHandler.GetMatchCount(mockVerifyRequest));
        Assert.Single(response);
        CredentialPermission permission = response.First();
        Assert.Equal("uaa-user:credhub_client", permission.Actor);
        Assert.Equal(3, permission.Operations.Count);
        Assert.Contains(OperationPermissions.Read, permission.Operations);
        Assert.Contains(OperationPermissions.Write, permission.Operations);
        Assert.DoesNotContain(OperationPermissions.ReadAcl, permission.Operations);
        Assert.DoesNotContain(OperationPermissions.WriteAcl, permission.Operations);
        Assert.Contains(OperationPermissions.Delete, permission.Operations);
    }

    [Fact]
    public async Task DeletePermissionAsync_Throws_WithoutActor()
    {
        var client = new CredHubClient();

        await Assert.ThrowsAsync<ArgumentException>(() => client.DeletePermissionAsync(string.Empty, "actor"));
    }

    [Fact]
    public async Task DeletePermissionAsync_Throws_WithoutPermissionToDelete()
    {
        var client = new CredHubClient();

        await Assert.ThrowsAsync<ArgumentException>(() => client.DeletePermissionAsync("credential", string.Empty));
    }

    [Fact]
    public async Task DeletePermissionAsync_ReturnsTrue_WhenDeleted()
    {
        MockHttpMessageHandler mockHttpMessageHandler = InitializedHandlerWithLogin();

        var queryString = new List<KeyValuePair<string, string>>
        {
            new("credential_name", "/example-password"),
            new("actor", "uaa-user:credhub_client")
        };

        mockHttpMessageHandler.Expect(HttpMethod.Delete, $"{CredHubBase}/v1/permissions").WithQueryString(queryString).Respond(HttpStatusCode.NoContent);
        CredHubClient client = await InitializeClientAsync(mockHttpMessageHandler);

        bool response = await client.DeletePermissionAsync("/example-password", "uaa-user:credhub_client");

        Assert.True(response);
    }

    [Fact]
    public async Task DeletePermissionAsync_ReturnsTrue_WhenNotFound()
    {
        MockHttpMessageHandler mockHttpMessageHandler = InitializedHandlerWithLogin();

        var queryString = new List<KeyValuePair<string, string>>
        {
            new("credential_name", "/example-password"),
            new("actor", "uaa-user:credhub_client")
        };

        mockHttpMessageHandler.Expect(HttpMethod.Delete, $"{CredHubBase}/v1/permissions").WithQueryString(queryString).Respond(HttpStatusCode.NotFound);
        CredHubClient client = await InitializeClientAsync(mockHttpMessageHandler);

        bool response = await client.DeletePermissionAsync("/example-password", "uaa-user:credhub_client");

        Assert.True(response);
    }

    [Fact]
    public async Task InterpolateServiceDataAsync_Throws_WithoutServiceData()
    {
        var client = new CredHubClient();

        await Assert.ThrowsAsync<ArgumentException>(() => client.InterpolateServiceDataAsync(string.Empty));
    }

    [Fact]
    public async Task InterpolateServiceDataAsync_CallsEndpoint_ReturnsInterpolatedString()
    {
        MockHttpMessageHandler mockHttpMessageHandler = InitializedHandlerWithLogin();

        MockedRequest mockRequest = mockHttpMessageHandler.Expect(HttpMethod.Post, $"{CredHubBase}/v1/interpolate").Respond("application/json",
            "{\"p-config-server\":[{\"credentials\":{\"key\":123,\"key_list\":[\"val1\",\"val2\"],\"is_true\":true},\"label\":\"p-config-server\",\"name\":\"config-server\",\"plan\":\"standard\",\"provider\":null,\"syslog_drain_url\":null,\"tags\":[\"configuration\",\"spring-cloud\"],\"volume_mounts\":[]}]}");

        CredHubClient client = await InitializeClientAsync(mockHttpMessageHandler);

        const string serviceData =
            "{\"p-config-server\":[{\"credentials\":{\"credhub-ref\":\"((/config-server/credentials))\"},\"label\":\"p-config-server\",\"name\":\"config-server\",\"plan\":\"standard\",\"provider\":null,\"syslog_drain_url\":null,\"tags\":[\"configuration\",\"spring-cloud\"],\"volume_mounts\":[]}]}";

        string response = await client.InterpolateServiceDataAsync(serviceData);

        mockHttpMessageHandler.VerifyNoOutstandingExpectation();
        Assert.Equal(1, mockHttpMessageHandler.GetMatchCount(mockRequest));
        Assert.Contains("\"key\":123", response, StringComparison.Ordinal);
    }

    private Task<CredHubClient> InitializeClientAsync(MockHttpMessageHandler mockHttpMessageHandler)
    {
        return CredHubClient.CreateUaaClientAsync(new CredHubOptions
        {
            CredHubUrl = CredHubBase,
            ClientId = "credHubUser",
            ClientSecret = "credHubPassword"
        }, httpClient: new HttpClient(mockHttpMessageHandler));
    }

    private MockHttpMessageHandler InitializedHandlerWithLogin()
    {
        var mockHttpMessageHandler = new MockHttpMessageHandler();
        string infoUrl = CredHubBase.Replace("/api", "/info", StringComparison.Ordinal);

        mockHttpMessageHandler.Expect(HttpMethod.Get, infoUrl)
            .Respond("application/json", "{\"auth-server\": {\"url\": \"http://uaa-server\"},\"app\":{\"name\":\"CredHub\" }}");

        mockHttpMessageHandler.Expect(HttpMethod.Post, "http://uaa-server/oauth/token").Respond("application/json", "{\"access_token\" : \"fake token\"}");
        return mockHttpMessageHandler;
    }
}
