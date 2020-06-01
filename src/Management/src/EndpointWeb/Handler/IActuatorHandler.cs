// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using System.Web;

namespace Steeltoe.Management.Endpoint.Handler
{
    public interface IActuatorHandler
    {
        void HandleRequest(HttpContextBase context);

        bool RequestVerbAndPathMatch(string httpMethod, string requestPath);

        Task<bool> IsAccessAllowed(HttpContextBase context);
    }
}
