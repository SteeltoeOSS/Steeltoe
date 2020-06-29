using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Steeltoe.Management.Endpoint
{
    public enum EndpointNames
    {
        Cloudfoundry,
        Actuator,
        Info,
        Metrics,
        Loggers,
        Health,
        HeapDump,
        Trace,
        DbMigrations,
        Env,
        Mappings,
        Refresh,
        ThreadDump,
        Prometheus
    }

    public class EndpointNamesCollection : IEndpointNames
    {
        private readonly List<EndpointNames> _endpointNames;

        public EndpointNamesCollection(EndpointNames[] endpointNames)
        {
            _endpointNames = endpointNames.ToList();
        }

        public IEnumerator<EndpointNames> GetEnumerator()
        {
            return _endpointNames.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _endpointNames.GetEnumerator();
        }
    }

    public interface IEndpointNames : IEnumerable<EndpointNames>
    {
    }
}
