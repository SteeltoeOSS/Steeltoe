// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

#pragma warning disable format

namespace Steeltoe.Management.Endpoint.Test.Actuators.RouteMappings.AppTypes.TestMvcControllers;

[Route("annotated-test")]
internal sealed class AnnotatedTestController : Controller
{
    [HttpGet("get-all")]
    [ProducesResponseType<IEnumerable<string>>(StatusCodes.Status200OK, "application/xhtml+xml")]
    [ProducesResponseType<IEnumerable<string>>(StatusCodes.Status400BadRequest, "text/html")]
    public Task<IActionResult> GetAllAsync([FromServices] IConfiguration configuration, CancellationToken cancellationToken)
    {
        _ = configuration;
        _ = cancellationToken;
        return Task.FromResult<IActionResult>(new EmptyResult());
    }

    [HttpPatch("update/{id:guid}")]
    [Consumes(typeof(Product), "application/json")]
    public async Task UpdateAsync(Guid id, int? languageId, [FromHeader(Name = "X-Media-Version")] string mediaVersion,
        [FromHeader(Name = "X-Include-Details")] [FromQuery] string? includeDetails, [FromBody] Product product, bool failOnError = true)
    {
        _ = id;
        _ = languageId;
        _ = mediaVersion;
        _ = includeDetails;
        _ = product;
        _ = failOnError;
        await Task.Yield();
    }

    [HttpDelete]
    [Produces("text/plain", "text/javascript")]
    public string Delete(
#nullable disable
        // Unable to determine whether the first parameter is required.
        string id, [Required] string alternateId
#nullable restore
    )
    {
        _ = id;
        _ = alternateId;
        return "alert('deleted successfully');";
    }

    internal sealed class Product
    {
        public string? Name { get; set; }
        public decimal Price { get; set; }
    }
}
