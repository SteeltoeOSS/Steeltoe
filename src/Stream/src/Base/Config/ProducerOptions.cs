// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Steeltoe.Stream.Config
{
    public class ProducerOptions : IProducerOptions
    {
        private const bool AutoStartup_Default = true;
        private const int PartitionCount_Default = 1;
        private const bool UseNativeEncoding_Default = false;
        private const bool IsErrorChannelEnabled_Default = false;

        public ProducerOptions()
        {
        }

        public bool? AutoStartup { get; set; }

        public string PartitionKeyExpression { get; set; }

        public string PartitionKeyExtractorName { get; set; }

        public string PartitionSelectorName { get; set; }

        public string PartitionSelectorExpression { get; set; }

        public int PartitionCount { get; set; } = int.MinValue;

        public List<string> RequiredGroups { get; set; }

        public HeaderMode? HeaderMode { get; set; }

        public bool? UseNativeEncoding { get; set; }

        public bool? ErrorChannelEnabled { get; set; }

        public bool IsPartitioned
        {
            get
            {
                return PartitionKeyExpression != null
                    || PartitionCount > 1
                    || PartitionKeyExtractorName != null;
            }
        }

        bool IProducerOptions.AutoStartup => AutoStartup.Value;

        HeaderMode IProducerOptions.HeaderMode => HeaderMode.Value;

        bool IProducerOptions.UseNativeEncoding => UseNativeEncoding.Value;

        bool IProducerOptions.ErrorChannelEnabled => ErrorChannelEnabled.Value;

        internal void PostProcess(ProducerOptions @default = null)
        {
            if (!ErrorChannelEnabled.HasValue)
            {
                ErrorChannelEnabled = (@default != null) ? @default.ErrorChannelEnabled : IsErrorChannelEnabled_Default;
            }

            if (!UseNativeEncoding.HasValue)
            {
                UseNativeEncoding = (@default != null) ? @default.UseNativeEncoding : UseNativeEncoding_Default;
            }

            if (!HeaderMode.HasValue)
            {
                HeaderMode = (@default != null) ? @default.HeaderMode : Config.HeaderMode.None;
            }

            if (RequiredGroups == null)
            {
                RequiredGroups = (@default != null) ? @default.RequiredGroups : new List<string>();
            }

            if (PartitionCount == int.MinValue)
            {
                PartitionCount = (@default != null) ? @default.PartitionCount : PartitionCount_Default;
            }

            if (PartitionSelectorExpression == null)
            {
                PartitionSelectorExpression = @default?.PartitionSelectorExpression;
            }

            if (PartitionSelectorName == null)
            {
                PartitionSelectorName = @default?.PartitionSelectorName;
            }

            if (PartitionKeyExtractorName == null)
            {
                PartitionKeyExtractorName = @default?.PartitionKeyExtractorName;
            }

            if (PartitionKeyExpression == null)
            {
                PartitionKeyExpression = @default?.PartitionKeyExpression;
            }

            if (!AutoStartup.HasValue)
            {
                AutoStartup = (@default != null) ? @default.AutoStartup : AutoStartup_Default;
            }
        }

        internal ProducerOptions Clone()
        {
            var clone = (ProducerOptions)MemberwiseClone();
            clone.RequiredGroups = new List<string>(RequiredGroups);
            return clone;
        }
    }
}
