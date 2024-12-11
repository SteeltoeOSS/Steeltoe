// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Text;
using Steeltoe.Common;
using Steeltoe.Logging;

namespace Steeltoe.Management.Tracing;

/// <summary>
/// An <see cref="IDynamicMessageProcessor" /> that adds tracing details from <see cref="Activity.Current" /> (if found) to log messages.
/// </summary>
public sealed class TracingLogProcessor : IDynamicMessageProcessor
{
    private readonly IApplicationInstanceInfo _applicationInstanceInfo;

    public TracingLogProcessor(IApplicationInstanceInfo applicationInstanceInfo)
    {
        ArgumentNullException.ThrowIfNull(applicationInstanceInfo);

        _applicationInstanceInfo = applicationInstanceInfo;
    }

    public string Process(string message)
    {
        ArgumentNullException.ThrowIfNull(message);

        Activity? currentActivity = Activity.Current;

        if (currentActivity == null)
        {
            return message;
        }

        var builder = new StringBuilder(" [");
        builder.Append(_applicationInstanceInfo.ApplicationName);
        builder.Append(',');

        string traceId = currentActivity.TraceId.ToHexString();

        builder.Append(traceId);
        builder.Append(',');

        builder.Append(currentActivity.SpanId.ToHexString());
        builder.Append(',');

        builder.Append(currentActivity.ParentSpanId.ToString());
        builder.Append(',');

        builder.Append(currentActivity.IsAllDataRequested ? "true" : "false");

        builder.Append("] ");
        builder.Append(message);

        return builder.ToString();
    }
}
