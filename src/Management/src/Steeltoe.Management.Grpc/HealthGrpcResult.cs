using Microsoft.AspNetCore.Mvc.RazorPages;
using ProtoBuf;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Management.Endpoint.Mappings;

namespace Steeltoe.Management.Grpc;

[ProtoContract]
public class HealthGrpcResult
{
    public HealthGrpcResult()
    {

    }
    public HealthGrpcResult(HealthCheckResult baseResult)
    {
        Status = baseResult.Status;
        Description = baseResult.Description;
        Details = new List<HealthGrpcDetail>();
        foreach(var key in baseResult.Details.Keys)
        {
            if (baseResult.Details[key] is HealthCheckResult healthCheckResult)
            {
                var innerResult = new HealthGrpcDetail(key, healthCheckResult);
                Details.Add(innerResult);
            }
        }
    }

    [ProtoMember(1)]
    public HealthStatus Status { get; set; }

    [ProtoMember(2)]
    public string Description { get; set; }

    [ProtoMember(3)]
    public List<HealthGrpcDetail> Details { get; set; }


}

[ProtoContract]
public class HealthGrpcDetail 
{
    public HealthGrpcDetail()
    {

    }
    public HealthGrpcDetail(string name, HealthCheckResult baseResult)
    {
      //  Status = baseResult.Status;
        Name = name;
        Description = baseResult.Description;
        if (baseResult.Details.Count() > 0)
        {
            Details = new Dictionary<string, string>();
            foreach (var key in baseResult.Details.Keys)
            {

                Details.Add(key, baseResult.Details[key].ToString());
            }
        }
    }

    [ProtoMember(3)]
    public string Name { get; set; }

    [ProtoMember(2)]
    public string Description { get; set; }

    [ProtoMember(1)]
    public Dictionary<string, string> Details { get; set; } 
}


