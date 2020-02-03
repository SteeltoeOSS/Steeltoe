// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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
