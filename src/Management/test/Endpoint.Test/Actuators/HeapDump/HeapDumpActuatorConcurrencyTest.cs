// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common.TestResources;
using Steeltoe.Common.TestResources.IO;
using Steeltoe.Management.Endpoint.Actuators.HeapDump;

namespace Steeltoe.Management.Endpoint.Test.Actuators.HeapDump;

[Collection("TestsForMemoryDumpsMustRunSequentially")]
[Trait("Category", "MemoryDumps")]
public sealed class HeapDumpActuatorConcurrencyTest
{
    private static readonly Dictionary<string, string?> AppSettings = new()
    {
        ["Management:Endpoints:Actuator:Exposure:Include:0"] = "heapdump"
    };

    [Fact]
    public async Task Concurrent_heap_dump_request_returns_error()
    {
        using var dumper = new BlockingHeapDumper();

        await using HostWrapper host = HostBuilderType.WebApplication.Build(builder =>
        {
            builder.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.AddInMemoryCollection(AppSettings));

            builder.ConfigureServices(services =>
            {
                // ReSharper disable once AccessToDisposedClosure
                services.AddSingleton<IHeapDumper>(dumper);
                services.AddHeapDumpActuator();
            });
        });

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        Task<HttpResponseMessage> requestTask1 = httpClient.GetAsync(new Uri("http://localhost/actuator/heapdump"), TestContext.Current.CancellationToken);
        await dumper.Started.WaitAsync(TestContext.Current.CancellationToken);

        HttpResponseMessage response2 = await httpClient.GetAsync(new Uri("http://localhost/actuator/heapdump"), TestContext.Current.CancellationToken);
        response2.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);

        string responseBody2 = await response2.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        responseBody2.Should().Be("Another memory dump is currently in progress, please try again later.");

        dumper.AllowToFinish();
        HttpResponseMessage response1 = await requestTask1;
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    /// A heap dumper that blocks until told to finish, so tests can reliably observe another request happening while a dump is in progress.
    /// </summary>
    private sealed class BlockingHeapDumper : IHeapDumper, IDisposable
    {
        private readonly TaskCompletionSource _startedSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly ManualResetEventSlim _canFinish = new(false);
        private TempFile? _fileCreated;

        public Task Started => _startedSource.Task;

        public void AllowToFinish()
        {
            _canFinish.Set();
        }

        public string DumpHeapToFile(CancellationToken cancellationToken)
        {
            _startedSource.TrySetResult();
            _canFinish.Wait(cancellationToken);

            _fileCreated = new TempFile();
            File.WriteAllBytes(_fileCreated.FullPath, "FAKEDUMP"u8.ToArray());
            return _fileCreated.FullPath;
        }

        public void Dispose()
        {
            _canFinish.Dispose();
            _fileCreated?.Dispose();
        }
    }
}
