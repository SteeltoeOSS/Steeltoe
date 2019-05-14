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

using Steeltoe.Management.Census.Trace.Export;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Steeltoe.Management.Census.Testing.Export
{
    [Obsolete]
    public class TestHandler : IHandler
    {
        private readonly object monitor = new object();
        private readonly List<ISpanData> spanDataList = new List<ISpanData>();

        public void Export(IList<ISpanData> data)
        {
            lock (monitor)
            {
                this.spanDataList.AddRange(data);
                Monitor.PulseAll(monitor);
            }
        }

        public IList<ISpanData> WaitForExport(int numberOfSpans)
        {
            IList<ISpanData> ret;
            lock (monitor)
            {
                while (spanDataList.Count < numberOfSpans)
                {
                    try
                    {
                        if (!Monitor.Wait(monitor, 5000))
                        {
                            return new List<ISpanData>();
                        }
                    }
                    catch (Exception)
                    {
                        // Preserve the interruption status as per guidance.
                        // Thread.currentThread().interrupt();
                        return new List<ISpanData>();
                    }
                }

                ret = new List<ISpanData>(spanDataList);
                spanDataList.Clear();
            }

            return ret;
        }
    }
}
