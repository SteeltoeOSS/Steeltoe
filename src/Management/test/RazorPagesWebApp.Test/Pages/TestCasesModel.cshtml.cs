// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Steeltoe.Management.Endpoint.RazorPagesWebApp.Test.Pages;

public class TestCasesModel : PageModel
{
    public void OnGet(int? languageId = 99, string? filter = null, int pageNumber = 1, int? pageSize = 10)
    {
        _ = languageId;
        _ = filter;
        _ = pageNumber;
        _ = pageSize;
    }

    public Task OnPostAsync()
    {
        return Task.CompletedTask;
    }

    public IActionResult OnPatchAsync([FromRoute] Guid id, [FromHeader(Name = "X-Media-Version")] string mediaVersion, [FromBody] JsonElement? requestBody)
    {
        _ = id;
        _ = mediaVersion;
        _ = requestBody;
        return new EmptyResult();
    }

    public void OnDeleteAllAsync()
    {
    }
}
