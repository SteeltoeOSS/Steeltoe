using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;
using Microsoft.AspNet.Razor.TagHelpers;
using Spring.Extensions.Configuration.CloudFoundry;
using Microsoft.AspNet.Mvc.Rendering;

namespace SimpleCloudFoundry.TagHelpers
{
   
    /// <summary>
    /// Tag helper for generating the HTML for the service credentials in a VCAP_SERVICES entry
    /// </summary>
    [HtmlTargetElement("service-credentials", Attributes ="credentials")]
    public class ServiceCredentialsTagHelper : TagHelper
    {
        [HtmlAttributeName("credentials")]
        public Dictionary<string, Credential> Credentials { set; get; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.Content.AppendHtml("<ul>");
            if (Credentials != null)
            {
                foreach(KeyValuePair<string, Credential> pair in Credentials)
                {
                    GenerateCredentialHtml(pair.Key, pair.Value, output);
                }
            }
            output.Content.AppendHtml("</ul>");
          
            base.Process(context, output);
        }

        private void GenerateCredentialHtml(string key, Credential credential, TagHelperOutput output)
        {
            if (!string.IsNullOrEmpty(credential.Value))
            {
                output.Content.AppendHtml("<li>");
                output.Content.Append(key + "=" + credential.Value);
                output.Content.AppendHtml("</li>");
            } else
            {

                output.Content.AppendHtml("<li>");
                output.Content.Append(key);
                output.Content.AppendHtml("</li>");

                output.Content.AppendHtml("<ul>");
                foreach (KeyValuePair<string, Credential> pair in credential)
                {
                    GenerateCredentialHtml(pair.Key, pair.Value, output);
                }
                output.Content.AppendHtml("</ul>");
            }
        }
    }
}
