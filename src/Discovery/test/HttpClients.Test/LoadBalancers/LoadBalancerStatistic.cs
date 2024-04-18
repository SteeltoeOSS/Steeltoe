// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Discovery.HttpClients.Test.LoadBalancers;

internal sealed class LoadBalancerStatistic
{
    public Uri RequestUri { get; }
    public Uri ServiceInstanceUri { get; }
    public TimeSpan? ResponseTime { get; }
    public Exception? Exception { get; }

    public LoadBalancerStatistic(Uri requestUri, Uri serviceInstanceUri, TimeSpan? responseTime, Exception? exception)
    {
        RequestUri = requestUri;
        ServiceInstanceUri = serviceInstanceUri;
        ResponseTime = responseTime;
        Exception = exception;
    }
}
