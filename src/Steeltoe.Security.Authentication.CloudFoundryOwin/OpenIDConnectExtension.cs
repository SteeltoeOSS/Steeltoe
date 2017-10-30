// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//

using Owin;
using System.Net;

namespace Steeltoe.Security.Authentication.CloudFoundry.Owin
{
    public static class OpenIDConnectExtension
    {
        public static IAppBuilder UseOpenIDConnect(this IAppBuilder app, OpenIDConnectOptions options)
        {
            // In order to talk to Pivotal SSO tile, we must limit the TLS defaults used by the service
            // point manager, otherwise the SSO tile will reject the TLS level on the connection and remotely
            // close the socket.
            System.Net.ServicePointManager.SecurityProtocol =
                SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

            return app.Use(typeof(OpenIDConnectAuthenticationMiddleware), app, options);
        }
    }
}
