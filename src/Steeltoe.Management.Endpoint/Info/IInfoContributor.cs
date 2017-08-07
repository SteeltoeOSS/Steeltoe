using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Endpoint.Info
{
    public interface IInfoContributor
    {
        void Contribute(IInfoBuilder builder);
    }
}
