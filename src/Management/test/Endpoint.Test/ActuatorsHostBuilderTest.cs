// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using RichardSzalay.MockHttp;
using Steeltoe.Common.Http.HttpClientPooling;
using Steeltoe.Common.TestResources;
using Steeltoe.Configuration.CloudFoundry;
using Steeltoe.Management.Endpoint.Actuators.CloudFoundry;

namespace Steeltoe.Management.Endpoint.Test;

public sealed class ActuatorsHostBuilderTest
{
    [Theory]
    [InlineData(HostBuilderType.Host)]
    [InlineData(HostBuilderType.WebHost)]
    [InlineData(HostBuilderType.WebApplication)]
    public async Task CloudFoundryActuator(HostBuilderType hostBuilderType)
    {
        const string token =
            "eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJpc3MiOiJodHRwczovL3VhYS5jbG91ZC5jb20vb2F1dGgvdG9rZW4iLCJpYXQiOjE3MzcwNjMxNzYsImV4cCI6MTc2ODU5OTE3NiwiYXVkIjoiYWN0dWF0b3IiLCJzdWIiOiJ1c2VyQGVtYWlsLmNvbSIsInNjb3BlIjpbImFjdHVhdG9yLnJlYWQiLCJjbG91ZF9jb250cm9sbGVyLnVzZXIiXSwiRW1haWwiOiJ1c2VyQGVtYWlsLmNvbSIsImNsaWVudF9pZCI6ImFwcHNfbWFuYWdlcl9qcyIsInVzZXJfbmFtZSI6InVzZXJAZW1haWwuY29tIiwidXNlcl9pZCI6InVzZXIifQ.bfCtDFxcWF8Yuie2p89S8_fTuUkAOd3i9M8PyKDV-N0";

        using var scope = new EnvironmentVariableScope("VCAP_APPLICATION", """
            {
                "cf_api": "https://api.cloud.com",
                "application_id": "fa05c1a9-0fc1-4fbd-bae1-139850dec7a3"
            }
            """);

        await using HostWrapper host = hostBuilderType.Build(builder =>
        {
            builder.ConfigureAppConfiguration(configurationBuilder =>
            {
                configurationBuilder.AddCloudFoundry();
            });

            builder.ConfigureServices(services => services.AddCloudFoundryActuator());
        });

        var handler = new DelegateToMockHttpClientHandler();

        handler.Mock.Expect(HttpMethod.Get, "https://api.cloud.com/v2/apps/fa05c1a9-0fc1-4fbd-bae1-139850dec7a3/permissions")
            .WithHeaders("Authorization", $"bearer {token}").Respond("application/json", """
                {
                    "read_sensitive_data": true
                }
                """);

        host.Services.GetRequiredService<HttpClientHandlerFactory>().Using(handler);

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        var request = new HttpRequestMessage(HttpMethod.Get, new Uri("http://localhost/cloudfoundryapplication"));
        request.Headers.Authorization = AuthenticationHeaderValue.Parse($"bearer {token}");

        HttpResponseMessage response = await httpClient.SendAsync(request, TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseText = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        responseText.Should().Contain("\"http://localhost/cloudfoundryapplication\"");

        handler.Mock.VerifyNoOutstandingExpectation();
    }
}
