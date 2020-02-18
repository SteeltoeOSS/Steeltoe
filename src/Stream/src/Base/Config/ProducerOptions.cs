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
