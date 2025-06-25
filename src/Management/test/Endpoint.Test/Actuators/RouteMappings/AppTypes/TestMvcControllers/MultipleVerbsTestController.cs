// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Mvc;

namespace Steeltoe.Management.Endpoint.Test.Actuators.RouteMappings.AppTypes.TestMvcControllers;

internal sealed class MultipleVerbsTestController : Controller
{
    [HttpGet]
    [HttpHead]
    public IActionResult GetAll()
    {
        return new EmptyResult();
    }

    [HttpGet("{id:long}")]
    [HttpHead("{id:long}")]
    public IActionResult GetById(long id)
    {
        _ = id;
        return new NotFoundResult();
    }
}
