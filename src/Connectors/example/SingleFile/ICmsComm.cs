using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Steeltoe.Connector.Example
{
    public interface ICMSComm
    {
        Task<bool> DbOnline();
    }
}
