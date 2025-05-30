// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Endpoint.Actuators.RouteMappings;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
{
    // This project intentionally does NOT include appsettings*.json files, because they get copied to test projects
    // that reference this project, and that affects test outcomes. For example, setting the minimum log level
    // to Trace on WebApplicationBuilder wouldn't work, because these files overrule log levels.

    ["DetailedErrors"] = builder.Environment.IsDevelopment() ? "true" : "false",
    ["Logging:LogLevel:Default"] = "Information",
    ["Logging:LogLevel:Microsoft.AspNetCore"] = "Warning",
    ["AllowedHosts"] = "*",
    ["Management:Endpoints:Actuator:Exposure:Include:0"] = "*",
    ["Management:Endpoints:Mappings:IncludeActuators"] = "false",
    ["Management:Endpoints:SerializerOptions:WriteIndented"] = "true"
});

builder.Services.AddRazorPages();
builder.Services.AddRouteMappingsActuator();

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

await app.RunAsync();
