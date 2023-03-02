using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Steeltoe.Management.Endpoint.Options;
public class ConfigureEndpointOptions<T>:IConfigureOptions<T>
    where T : EndpointOptionsBase
{
    protected readonly IConfiguration configuration;
    private readonly string prefix;
    private readonly string id;

    public ConfigureEndpointOptions(IConfiguration configuration, string prefix, string id)
    {
        this.configuration = configuration;
        this.prefix = prefix;
        this.id = id;
    }

    public virtual void Configure(T options)
    {
        configuration.GetSection(prefix).Bind(options);
        if (options.Id == null)
        {
            options.Id = id;
        }
        if (options.RequiredPermissions == Permissions.Undefined)
        {
            options.RequiredPermissions = Permissions.Restricted;
        }
    }
}
