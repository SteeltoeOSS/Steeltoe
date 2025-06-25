// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Steeltoe.Management.Endpoint.Test.Actuators.RouteMappings.AppTypes.TestApiControllers;

[ApiController]
[Route("annotated-test")]
internal sealed class AnnotatedTestController : ControllerBase
{
    [HttpGet("get-all")]
    [ProducesResponseType<IEnumerable<string>>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<IEnumerable<string>>(StatusCodes.Status400BadRequest, "application/xml")]
    public IEnumerable<string> GetAll()
    {
        return [];
    }

    [HttpPatch("update/{id:guid}")]
    [Consumes(typeof(Product), "application/json")]
    public async Task UpdateAsync(Guid id, [FromHeader(Name = "X-Media-Version")] string mediaVersion,
        [FromHeader(Name = "X-Include-Details")] string? includeDetails, [FromBody] Product product, bool failOnError = true)
    {
        _ = id;
        _ = mediaVersion;
        _ = includeDetails;
        _ = product;
        _ = failOnError;
        await Task.Yield();
    }

    internal sealed class Product
    {
        public string? Name { get; set; }
        public decimal Price { get; set; }
    }
}
