// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace Steeltoe.Integration.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class ServiceActivatorAttribute : Attribute
    {
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
            RequiresReply = requiresReply.ToString();
        }

        public ServiceActivatorAttribute(string inputChannel, string outputChannel, bool requiresReply, int sendTimeout)
        {
            InputChannel = inputChannel;
            OutputChannel = outputChannel;
            RequiresReply = requiresReply.ToString();
            SendTimeout = sendTimeout.ToString();
        }

        public ServiceActivatorAttribute(string inputChannel, string outputChannel, bool requiresReply, int sendTimeout, bool autoStartup)
        {
            InputChannel = inputChannel;
            OutputChannel = outputChannel;
            RequiresReply = requiresReply.ToString();
            SendTimeout = sendTimeout.ToString();
            AutoStartup = autoStartup.ToString();
        }

        public ServiceActivatorAttribute(string inputChannel, string outputChannel, bool requiresReply, int sendTimeout, bool autoStartup, int phase)
        {
            InputChannel = inputChannel;
            OutputChannel = outputChannel;
            RequiresReply = requiresReply.ToString();
            SendTimeout = sendTimeout.ToString();
            AutoStartup = autoStartup.ToString();
            Phase = phase.ToString();
        }

        public string InputChannel { get; set; } = string.Empty;

        public string OutputChannel { get; set; } = string.Empty;

        public string RequiresReply { get; set; } = string.Empty;

        public string SendTimeout { get; set; } = string.Empty;

        public string AutoStartup { get; set; } = string.Empty;

        public string Phase { get; set; } = string.Empty;
    }
}
