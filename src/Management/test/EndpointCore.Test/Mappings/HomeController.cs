// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Mvc;

namespace Steeltoe.Management.Endpoint.Mappings.Test
{
    public class HomeController : Controller
    {
        [HttpGet]
        [Produces("text/plain", new string[] { "application/json", "text/json" })]
        public Person Index()
        {
            return new Person();
        }
    }
}
