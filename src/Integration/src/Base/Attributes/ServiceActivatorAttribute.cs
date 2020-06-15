// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace Steeltoe.Integration.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class ServiceActivatorAttribute : Attribute
    {
        public ServiceActivatorAttribute(string inputChannel)
        {
            InputChannel = inputChannel;
        }

        public ServiceActivatorAttribute()
        {
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
            RequiresReply = requiresReply;
        }

        public ServiceActivatorAttribute(string inputChannel, string outputChannel, bool requiresReply, int sendTimeout)
        {
            InputChannel = inputChannel;
            OutputChannel = outputChannel;
            RequiresReply = requiresReply;
            SendTimeout = sendTimeout;
        }

        public ServiceActivatorAttribute(string inputChannel, string outputChannel, bool requiresReply, int sendTimeout, bool autoStartup)
        {
            InputChannel = inputChannel;
            OutputChannel = outputChannel;
            RequiresReply = requiresReply;
            SendTimeout = sendTimeout;
            AutoStartup = autoStartup;
        }

        public ServiceActivatorAttribute(string inputChannel, string outputChannel, bool requiresReply, int sendTimeout, bool autoStartup, int phase)
        {
            InputChannel = inputChannel;
            OutputChannel = outputChannel;
            RequiresReply = requiresReply;
            SendTimeout = sendTimeout;
            AutoStartup = autoStartup;
            Phase = phase;
        }

        public string InputChannel { get; set; } = string.Empty;

        public string OutputChannel { get; set; } = string.Empty;

        public bool RequiresReply { get; set; } = false;

        public int SendTimeout { get; set; } = -1;

        public bool AutoStartup { get; set; } = true;

        public int Phase { get; set; } = int.MaxValue / 2;
    }
}
