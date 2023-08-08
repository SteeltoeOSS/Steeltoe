// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using k8s;
using k8s.Autorest;
using k8s.Models;
using Microsoft.Extensions.Logging;

namespace Steeltoe.Common.Kubernetes;

public sealed class PodUtilities
{
    private readonly KubernetesApplicationOptions _options;
    private readonly IKubernetes _kubernetes;
    private readonly ILogger<PodUtilities> _logger;

    public PodUtilities(KubernetesApplicationOptions options, IKubernetes kubernetes, ILogger<PodUtilities> logger)
    {
        ArgumentGuard.NotNull(options);
        ArgumentGuard.NotNull(kubernetes);
        ArgumentGuard.NotNull(logger);

        _options = options;
        _kubernetes = kubernetes;
        _logger = logger;
    }

    public async Task<V1Pod> GetCurrentPodAsync(CancellationToken cancellationToken)
    {
        try
        {
            string hostname = Environment.GetEnvironmentVariable("HOSTNAME");

            HttpOperationResponse<V1PodList> response =
                await _kubernetes.CoreV1.ListNamespacedPodWithHttpMessagesAsync(_options.NameSpace, cancellationToken: cancellationToken);

            return response.Body.Items?.FirstOrDefault(p => p.Metadata.Name == hostname);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to retrieve information about the current pod.");
            return null;
        }
    }
}
