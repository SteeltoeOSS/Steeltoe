
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;


namespace Steeltoe.Management.Endpoint.Trace
{
    public class TraceEndpoint : AbstractEndpoint<List<Trace>>
    {

        private ILogger<TraceEndpoint> _logger;
        private ITraceRepository _traceRepo;

        public new ITraceOptions Options
        {
            get
            {
                return options as ITraceOptions;
            }
        }

        public TraceEndpoint(ITraceOptions options, ITraceRepository repository, ILogger<TraceEndpoint> logger) :
            base(options)
        {
            _logger = logger;
            _traceRepo = repository;
        }

        public override List<Trace> Invoke()
        {
            return _traceRepo.GetTraces();
        }

    }

}
