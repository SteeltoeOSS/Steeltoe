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

using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace Steeltoe.Management.Endpoint.Trace
{
    public class TraceEndpoint : AbstractEndpoint<List<TraceResult>>
    {
        private ILogger<TraceEndpoint> _logger;
        private ITraceRepository _traceRepo;

        public TraceEndpoint(ITraceOptions options, ITraceRepository traceRepository, ILogger<TraceEndpoint> logger = null)
            : base(options)
        {
            _traceRepo = traceRepository ?? throw new ArgumentNullException(nameof(traceRepository));
            _logger = logger;
        }

        public new ITraceOptions Options
        {
            get
            {
                return options as ITraceOptions;
            }
        }

        public override List<TraceResult> Invoke()
        {
            return DoInvoke(_traceRepo);
        }

        public List<TraceResult> DoInvoke(ITraceRepository repo)
        {
            return repo.GetTraces();
        }
    }
}
