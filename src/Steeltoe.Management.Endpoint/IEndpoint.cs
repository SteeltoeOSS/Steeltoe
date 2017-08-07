using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Endpoint
{
    public interface IEndpoint
    {
        string Id { get; }
        bool Enabled { get; }
        bool Sensitive { get; }
        IEndpointOptions Options { get; }
        string Path { get; }
    }

    public interface IEndpoint<TResult> : IEndpoint
    {
        TResult Invoke();
    }

    public interface IEndpoint<TResult, TRequest> : IEndpoint
    {
        TResult Invoke(TRequest arg);
    }
}
