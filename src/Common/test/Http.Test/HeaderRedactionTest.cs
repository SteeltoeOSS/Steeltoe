// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.TestResources;

// ReSharper disable AccessToDisposedClosure

namespace Steeltoe.Common.Http.Test;

public sealed class HeaderRedactionTest
{
    [Fact]
    public async Task HttpClientFactory_uses_HTTP_header_redaction_by_default()
    {
        using var capturingLoggerProvider =
            new CapturingLoggerProvider(category => category.StartsWith("System.Net.Http.HttpClient", StringComparison.Ordinal));

        var services = new ServiceCollection();
        services.DisableHttpClientRotationTimers();
        services.AddLogging(options => options.SetMinimumLevel(LogLevel.Trace).AddProvider(capturingLoggerProvider));

        using var handler = new DelegateToMockHttpClientHandler();
        handler.RespondToAnyRequest(HttpStatusCode.OK);
        services.AddHttpClient("example").ConfigurePrimaryHttpMessageHandler(_ => handler);

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        using HttpClient httpClient = httpClientFactory.CreateClient("example");

        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("http://localhost/example"));
        request.Headers.Add("Authorization", "Bearer super-secret-token");

        using HttpResponseMessage response = await httpClient.SendAsync(request, TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string logMessages = string.Join(Environment.NewLine, capturingLoggerProvider.GetAll());
        logMessages.Should().Contain("Authorization: *");
        logMessages.Should().NotContain("super-secret-token");
    }
}
