// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Globalization;

namespace Steeltoe.Integration.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public sealed class ServiceActivatorAttribute : Attribute
{
    public string InputChannel { get; set; } = string.Empty;

    public string OutputChannel { get; set; } = string.Empty;

    public string RequiresReply { get; set; } = string.Empty;

    public string SendTimeout { get; set; } = string.Empty;

    public string AutoStartup { get; set; } = string.Empty;

    public string Phase { get; set; } = string.Empty;

    public ServiceActivatorAttribute()
    {
    }

    public ServiceActivatorAttribute(string inputChannel)
    {
        InputChannel = inputChannel;
    }

    public ServiceActivatorAttribute(string inputChannel, string outputChannel)
    {
        InputChannel = inputChannel;
        OutputChannel = outputChannel;
    }

    public ServiceActivatorAttribute(string inputChannel, string outputChannel, bool requiresReply)
    {
        InputChannel = inputChannel;
        OutputChannel = outputChannel;
        RequiresReply = requiresReply.ToString(CultureInfo.InvariantCulture);
    }

    public ServiceActivatorAttribute(string inputChannel, string outputChannel, bool requiresReply, int sendTimeout)
    {
        InputChannel = inputChannel;
        OutputChannel = outputChannel;
        RequiresReply = requiresReply.ToString(CultureInfo.InvariantCulture);
        SendTimeout = sendTimeout.ToString(CultureInfo.InvariantCulture);
    }

    public ServiceActivatorAttribute(string inputChannel, string outputChannel, bool requiresReply, int sendTimeout, bool autoStartup)
    {
        InputChannel = inputChannel;
        OutputChannel = outputChannel;
        RequiresReply = requiresReply.ToString(CultureInfo.InvariantCulture);
        SendTimeout = sendTimeout.ToString(CultureInfo.InvariantCulture);
        AutoStartup = autoStartup.ToString(CultureInfo.InvariantCulture);
    }

    public ServiceActivatorAttribute(string inputChannel, string outputChannel, bool requiresReply, int sendTimeout, bool autoStartup, int phase)
    {
        InputChannel = inputChannel;
        OutputChannel = outputChannel;
        RequiresReply = requiresReply.ToString(CultureInfo.InvariantCulture);
        SendTimeout = sendTimeout.ToString(CultureInfo.InvariantCulture);
        AutoStartup = autoStartup.ToString(CultureInfo.InvariantCulture);
        Phase = phase.ToString(CultureInfo.InvariantCulture);
    }
}
