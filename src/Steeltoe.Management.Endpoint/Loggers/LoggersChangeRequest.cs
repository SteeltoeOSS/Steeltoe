using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Steeltoe.Management.Endpoint.Loggers
{
    public class LoggersChangeRequest
    {
        public LoggersChangeRequest(string name, string level)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException(nameof(name));
            }

            if (string.IsNullOrEmpty(level))
            {
                throw new ArgumentException(nameof(level));
            }

            Name = name;
            Level = level;
        }
        public string Name { get; }
        public string Level { get; }
        public override string ToString()
        {
            return "[" + Name + "," + Level + "]";
        }
    }
}
