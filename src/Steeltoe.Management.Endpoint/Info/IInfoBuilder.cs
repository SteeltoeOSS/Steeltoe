using System.Collections.Generic;

namespace Steeltoe.Management.Endpoint.Info
{
    public interface IInfoBuilder
    {
        IInfoBuilder WithInfo(string key, object value);
        IInfoBuilder WithInfo(Dictionary<string, object> details);
        Dictionary<string, object> Build();

    }
}
