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
using System;
using System.Threading;

namespace Steeltoe.Management.Census.Trace.Export
{
    [Obsolete("Use OpenCensus project packages")]
    public sealed class SpanExporter : SpanExporterBase
    {
        private SpanExporterWorker Worker { get; }

        private readonly Thread _workerThread;

        internal SpanExporter(SpanExporterWorker worker)
        {
            Worker = worker;
            _workerThread = new Thread(worker.Run)
            {
                IsBackground = true,
                Name = "SpanExporter"
            };
            _workerThread.Start();
        }

        internal static ISpanExporter Create(int bufferSize, IDuration scheduleDelay)
        {
            SpanExporterWorker worker = new SpanExporterWorker(bufferSize, scheduleDelay);
            return new SpanExporter(worker);
        }

        public override void AddSpan(ISpan span)
        {
            Worker.AddSpan(span);
        }

        public override void RegisterHandler(string name, IHandler handler)
        {
            Worker.RegisterHandler(name, handler);
        }

        public override void UnregisterHandler(string name)
        {
            Worker.UnregisterHandler(name);
        }

        public override void Dispose()
        {
            Worker.Dispose();
        }

        internal Thread ServiceExporterThread
        {
            get
            {
                return _workerThread;
            }
        }
    }
}
