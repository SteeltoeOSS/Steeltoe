using System.Collections.Generic;
using System.Linq;
using Steeltoe.Management.Endpoint;

namespace Steeltoe.Management.EndpointBase
{
    public class ActuatorMediaTypes
    {
        public static readonly string V1_JSON = "application/vnd.spring-boot.actuator.v1+json";

        public static readonly string V2_JSON = "application/vnd.spring-boot.actuator.v2+json";

        public static readonly string APP_JSON = "application/json";

        public static string GetContentHeaders(List<string> acceptHeaders, MediaTypeVersion version = MediaTypeVersion.V2)
        {
            var contentHeader = string.Empty;
            var allowedMediaTypes = new List<string> {version == MediaTypeVersion.V2 ? V2_JSON : V1_JSON, APP_JSON};
            
            foreach (var acceptHeader in acceptHeaders)
            {
                contentHeader = allowedMediaTypes.First(header => acceptHeader == header);
            }

            return contentHeader + ";charset=UTF-8";
        }
    }
}