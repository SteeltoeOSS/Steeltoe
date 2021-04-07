// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
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
        private const bool IsBatchMode_Default = false;

        public ConsumerOptions()
        {
        }

        public ConsumerOptions(string bindingName)
        {
            if (bindingName == null)
            {
                throw new ArgumentNullException(nameof(bindingName));
            }

            BindingName = bindingName;
        }

        public string BindingName { get; set; }

        public bool? AutoStartup { get; set; }

        public int Concurrency { get; set; } = int.MinValue;

        public bool? Partitioned { get; set; }

        public int InstanceCount { get; set; } = int.MinValue;

        public int InstanceIndex { get; set; } = int.MinValue;

        public List<int> InstanceIndexList { get; set; }

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

        bool IConsumerOptions.UseNativeDecoding => UseNativeDecoding.GetValueOrDefault();

        bool IConsumerOptions.Multiplex => Multiplex.GetValueOrDefault();

        public IConsumerOptions Clone()
        {
            var clone = (ConsumerOptions)MemberwiseClone();
            clone.RetryableExceptions = new List<string>(RetryableExceptions);
            clone.InstanceIndexList = new List<int>(InstanceIndexList);
            return clone;
        }

        internal void PostProcess(string name, ConsumerOptions @default = null)
        {
            BindingName = name;

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

            if (InstanceIndexList == null)
            {
                InstanceIndexList = (@default != null) ? @default.InstanceIndexList : new List<int>();
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
    }
}
