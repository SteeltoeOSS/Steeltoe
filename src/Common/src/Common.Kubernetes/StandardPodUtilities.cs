// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using k8s;
using k8s.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Rest;
using Steeltoe.Common.Kubernetes;

namespace Steeltoe.Extensions.Configuration.Kubernetes;

public class StandardPodUtilities : IPodUtilities
{
    private readonly KubernetesApplicationOptions _applicationOptions;
    private readonly ILogger _logger;
    private readonly IKubernetes _kubernetes;

    public StandardPodUtilities(KubernetesApplicationOptions kubernetesApplicationOptions, ILogger logger = null, IKubernetes kubernetes = null)
    {
        _applicationOptions = kubernetesApplicationOptions ?? throw new ArgumentNullException(nameof(kubernetesApplicationOptions));
        _logger = logger;
        _kubernetes = kubernetes ?? throw new ArgumentNullException(nameof(kubernetes), "A Kubernetes client is required");
    }

    public async Task<V1Pod> GetCurrentPodAsync()
    {
        V1Pod pod = null;

        try
        {
            string hostname = Environment.GetEnvironmentVariable("HOSTNAME");
            HttpOperationResponse<V1PodList> rsp = await _kubernetes.ListNamespacedPodWithHttpMessagesAsync(_applicationOptions.NameSpace);
            pod = rsp.Body.Items?.FirstOrDefault(p => p.Metadata.Name.Equals(hostname));
        }
        catch (Exception e)
        {
            _logger?.LogCritical(e, "Failed to retrieve information about the current pod");
        }

        return pod;
    }
}
