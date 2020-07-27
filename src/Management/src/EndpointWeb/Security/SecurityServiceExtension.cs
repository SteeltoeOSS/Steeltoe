// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace Steeltoe.Management.Endpoint.Security
{
    public static class SecurityServiceExtension
    {
        public static async Task<bool> IsAccessAllowed(this IEnumerable<ISecurityService> securityChecks, HttpContextBase context, IEndpointOptions options)
        {
            var allTasks = securityChecks.Select(service => service.IsAccessAllowed(context, options));
            var returnValues = await Task.WhenAll(allTasks).ConfigureAwait(false);
            return returnValues.All(isAccessAllowed => isAccessAllowed == true);
        }
    }
}
