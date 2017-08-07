using Microsoft.AspNetCore.Http;
using Steeltoe.Management.Endpoint.Middleware;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Steeltoe.Management.Endpoint.Trace
{

    public class TraceEndpointMiddleware : EndpointMiddleware<List<Trace>>
    {
        private RequestDelegate _next;

        public TraceEndpointMiddleware(RequestDelegate next, TraceEndpoint endpoint, ILogger<TraceEndpointMiddleware> logger)
            : base(endpoint, logger)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            if (IsTraceRequest(context))
            {
                await HandleTraceRequestAsync(context);
            }
            else
            {
                await _next(context);
            }
        }

        private async Task HandleTraceRequestAsync(HttpContext context)
        {
            var serialInfo = base.HandleRequest();
            logger.LogDebug("Returning: {0}", serialInfo);
            context.Response.Headers.Add("Content-Type", "application/vnd.spring-boot.actuator.v1+json");
            await context.Response.WriteAsync(serialInfo);
        }

        private bool IsTraceRequest(HttpContext context)
        {
            if (!context.Request.Method.Equals("GET")) { return false; }
            PathString path = new PathString(endpoint.Path);
            return context.Request.Path.Equals(path);
        }

    }
}
