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

using System.Collections.Generic;

namespace Steeltoe.Stream.Config
{
    public class ConsumerOptions : IConsumerOptions
    {
        private const bool AutoStartup_Default = true;
        private const int Concurrency_Default = 1;
        private const bool IsPartitioned_Default = false;
        private const int InstanceCount_Default = -1;
        private const int InstanceIndex_Default = -1;
        private const int MaxAttempts_Default = 3;
        private const int BackOffInitialInterval_Default = 1000;
        private const int BackOffMaxInterval_Default = 10000;
        private const double BackOffMultiplier_Default = 2.0;
        private const bool DefaultRetryable_Default = true;
        private const bool UseNativeDecoding_Default = false;
        private const bool Multiplex_Default = false;

        public ConsumerOptions()
        {
        }

        public bool? AutoStartup { get; set; }

        public int Concurrency { get; set; } = int.MinValue;

        public bool? Partitioned { get; set; }

        public int InstanceCount { get; set; } = int.MinValue;

        public int InstanceIndex { get; set; } = int.MinValue;

        public int MaxAttempts { get; set; } = int.MinValue;

        public int BackOffInitialInterval { get; set; } = int.MinValue;

        public int BackOffMaxInterval { get; set; } = int.MinValue;

        public double BackOffMultiplier { get; set; } = double.NaN;

        public bool? DefaultRetryable { get; set; }

        public List<string> RetryableExceptions { get; set; }

        public HeaderMode? HeaderMode { get; set; }

        public bool? UseNativeDecoding { get; set; }

        public bool? Multiplex { get; set; }

        bool IConsumerOptions.AutoStartup => AutoStartup.Value;

        bool IConsumerOptions.IsPartitioned => Partitioned.Value;

        bool IConsumerOptions.DefaultRetryable => DefaultRetryable.Value;

        HeaderMode IConsumerOptions.HeaderMode => HeaderMode.Value;

        bool IConsumerOptions.UseNativeDecoding => UseNativeDecoding.Value;

        bool IConsumerOptions.Multiplex => Multiplex.Value;

        internal void PostProcess(ConsumerOptions @default = null)
        {
            if (!Multiplex.HasValue)
            {
                Multiplex = (@default != null) ? @default.Multiplex : Multiplex_Default;
            }

            if (!UseNativeDecoding.HasValue)
            {
                UseNativeDecoding = (@default != null) ? @default.UseNativeDecoding : UseNativeDecoding_Default;
            }

            if (!HeaderMode.HasValue)
            {
                HeaderMode = (@default != null) ? @default.HeaderMode : Config.HeaderMode.None;
            }

            if (RetryableExceptions == null)
            {
                RetryableExceptions = (@default != null) ? @default.RetryableExceptions : new List<string>();
            }

            if (!DefaultRetryable.HasValue)
            {
                DefaultRetryable = (@default != null) ? @default.DefaultRetryable : DefaultRetryable_Default;
            }

            if (double.IsNaN(BackOffMultiplier))
            {
                BackOffMultiplier = (@default != null) ? @default.BackOffMultiplier : BackOffMultiplier_Default;
            }

            if (BackOffMaxInterval == int.MinValue)
            {
                BackOffMaxInterval = (@default != null) ? @default.BackOffMaxInterval : BackOffMaxInterval_Default;
            }

            if (BackOffInitialInterval == int.MinValue)
            {
                BackOffInitialInterval = (@default != null) ? @default.BackOffInitialInterval : BackOffInitialInterval_Default;
            }

            if (MaxAttempts == int.MinValue)
            {
                MaxAttempts = (@default != null) ? @default.MaxAttempts : MaxAttempts_Default;
            }

            if (InstanceIndex == int.MinValue)
            {
                InstanceIndex = (@default != null) ? @default.InstanceIndex : InstanceIndex_Default;
            }

            if (InstanceCount == int.MinValue)
            {
                InstanceCount = (@default != null) ? @default.InstanceCount : InstanceCount_Default;
            }

            if (!Partitioned.HasValue)
            {
                Partitioned = (@default != null) ? @default.Partitioned : IsPartitioned_Default;
            }

            if (Concurrency == int.MinValue)
            {
                Concurrency = (@default != null) ? @default.Concurrency : Concurrency_Default;
            }

            if (!AutoStartup.HasValue)
            {
                AutoStartup = (@default != null) ? @default.AutoStartup : AutoStartup_Default;
            }
        }

        internal ConsumerOptions Clone()
        {
            var clone = (ConsumerOptions)MemberwiseClone();
            clone.RetryableExceptions = new List<string>(RetryableExceptions);
            return clone;
        }
    }
}
