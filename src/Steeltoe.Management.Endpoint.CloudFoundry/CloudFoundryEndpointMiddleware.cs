
using Steeltoe.Management.Endpoint.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System.Threading.Tasks;

namespace Steeltoe.Management.Endpoint.CloudFoundry
{
    public class CloudFoundryEndpointMiddleware : EndpointMiddleware<Links, string>
    {
   
        private ICloudFoundryOptions _options;
        private RequestDelegate _next;

        public CloudFoundryEndpointMiddleware(RequestDelegate next, CloudFoundryEndpoint endpoint, ILogger<CloudFoundryEndpointMiddleware> logger)
            : base(endpoint, logger)
        {
            _next = next;
            _options = endpoint.Options as ICloudFoundryOptions;
        }

        public async Task Invoke(HttpContext context)
        {
            if (IsCloudFoundryRequest(context))
            {
                await HandleCloudFoundryRequestAsync(context);
            } else
            {
                await  _next(context);
            }
        }

        private async Task HandleCloudFoundryRequestAsync(HttpContext context)
        {

            var serialInfo = base.HandleRequest(GetRequestUri(context.Request));
            logger.LogDebug("Returning: {0}", serialInfo);
            context.Response.Headers.Add("Content-Type", "application/json;charset=UTF-8");
            context.Response.Headers.Add("X-Application-Context", "actuator:cloud:0");
            await context.Response.WriteAsync(serialInfo);

        }

        private string GetRequestUri(HttpRequest request)
        {
            string scheme = request.Scheme;

            StringValues headerScheme;
            if (request.Headers.TryGetValue("X-Forwarded-Proto", out headerScheme))
            {
                scheme = headerScheme.ToString();
            }

            return scheme + "://" + request.Host.ToString() + request.Path.ToString();
        }

        public bool IsCloudFoundryRequest(HttpContext context)
        {
            if (!context.Request.Method.Equals("GET")) { return false; }
            PathString path = new PathString(endpoint.Path);
            return context.Request.Path.Equals(path);
        }


    }
}

