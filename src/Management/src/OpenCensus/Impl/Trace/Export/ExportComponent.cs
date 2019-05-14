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

using Steeltoe.Management.Census.Common;
using Steeltoe.Management.Census.Internal;
using System;

namespace Steeltoe.Management.Census.Trace.Export
{
    [Obsolete("Use OpenCensus project packages")]
    public sealed class ExportComponent : ExportComponentBase
    {
        private const int EXPORTER_BUFFER_SIZE = 32;

        // Enforces that trace export exports data at least once every 5 seconds.
        private static readonly IDuration EXPORTER_SCHEDULE_DELAY = Duration.Create(5, 0);

        public static IExportComponent CreateWithoutInProcessStores(IEventQueue eventQueue)
        {
            return new ExportComponent(false, eventQueue);
        }

        public static IExportComponent CreateWithInProcessStores(IEventQueue eventQueue)
        {
            return new ExportComponent(true, eventQueue);
        }

        private ExportComponent(bool supportInProcessStores, IEventQueue eventQueue)
        {
            SpanExporter = Export.SpanExporter.Create(EXPORTER_BUFFER_SIZE, EXPORTER_SCHEDULE_DELAY);
            this.RunningSpanStore =
                supportInProcessStores
                    ? new InProcessRunningSpanStore()
                    : Export.RunningSpanStoreBase.NoopRunningSpanStore;
            this.SampledSpanStore =
                supportInProcessStores
                    ? new InProcessSampledSpanStore(eventQueue)
                    : Export.SampledSpanStoreBase.NoopSampledSpanStore;
        }

        public override ISpanExporter SpanExporter { get; }

        public override IRunningSpanStore RunningSpanStore { get; }

        public override ISampledSpanStore SampledSpanStore { get; }
    }
}
