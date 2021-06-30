#pragma warning disable SA1636 // File header copyright text should match

// <copyright file="LabelSetSdk.cs" company="OpenTelemetry Authors">
// Copyright 2018, OpenTelemetry Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
#pragma warning restore SA1636 // File header copyright text should match

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenTelemetry.Metrics
{
    /// <summary>
    /// LabelSet implementation.
    /// </summary>
    [Obsolete("OpenTelemetry Metrics API is not considered stable yet, see https://github.com/SteeltoeOSS/Steeltoe/issues/711 more information")]
    internal class LabelSetSdk : LabelSet
    {
        // encoded value is used internally by the SDK as a key in Dictionary.
        // This could potentially be made public, and combined with an
        // option to override Encoder, can avoid reencoding of labels
        // at Exporter level.
        private string _labelSetEncoded;

        /// <summary>
        /// Initializes a new instance of the <see cref="LabelSetSdk"/> class.
        /// </summary>
        /// <param name="labels">labels from which labelset should be constructed.</param>
        internal LabelSetSdk(IEnumerable<KeyValuePair<string, string>> labels)
        {
            Labels = SortAndDedup(labels);
            _labelSetEncoded = GetLabelSetEncoded(Labels);
        }

        /// <summary>
        /// Gets or sets the sorted and de-duped labels for this LabelSet.
        /// For duplicated keys, the last value wins.
        /// </summary>
        public override IEnumerable<KeyValuePair<string, string>> Labels { get; set; }

        /// <inheritdoc/>
#pragma warning disable S2328 // "GetHashCode" should not reference mutable fields
        public override int GetHashCode() => _labelSetEncoded.GetHashCode();
#pragma warning restore S2328 // "GetHashCode" should not reference mutable fields

        /// <inheritdoc/>
        public override bool Equals(object obj) => _labelSetEncoded.Equals(((LabelSetSdk)obj)._labelSetEncoded);

        private static IEnumerable<KeyValuePair<string, string>> SortAndDedup(IEnumerable<KeyValuePair<string, string>> labels)
        {
            var orderedList = labels.OrderBy(x => x.Key).ToList();
            if (orderedList.Count == 1)
            {
                return orderedList;
            }

            var dedupedList = new List<KeyValuePair<string, string>>();

            var dedupedListIndex = 0;
            dedupedList.Add(orderedList[dedupedListIndex]);
            for (var i = 1; i < orderedList.Count; i++)
            {
                if (orderedList[i].Key.Equals(orderedList[i - 1].Key))
                {
                    dedupedList[dedupedListIndex] = orderedList[i];
                }
                else
                {
                    dedupedList.Add(orderedList[i]);
                    dedupedListIndex++;
                }
            }

            return dedupedList;
        }

        private static string GetLabelSetEncoded(IEnumerable<KeyValuePair<string, string>> labels)
        {
            var encoder = new StringBuilder();
            var isFirstLabel = true;

            // simple encoding.
            foreach (var label in labels)
            {
                if (!isFirstLabel)
                {
                    encoder.Append(",");
                }

                encoder.Append(label.Key);
                encoder.Append("=");
                encoder.Append(label.Value);
                isFirstLabel = false;
            }

            return encoder.ToString();
        }
    }
}
