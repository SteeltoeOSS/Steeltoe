using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Steeltoe.Management.Endpoint.Trace
{
    public class Trace
    {
        public Trace(long timestamp, Dictionary<string, object> info)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            TimeStamp = timestamp;
            Info = info;
        }

        [JsonProperty("timestamp")]
        public long TimeStamp { get; }

        [JsonProperty("info")]
        public Dictionary<string, object> Info { get; }

    }
}
