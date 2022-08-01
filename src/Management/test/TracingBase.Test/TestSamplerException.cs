// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace Steeltoe.Management.Tracing.Test;

[Serializable]
public class TestSamplerException : Exception
{
    public TestSamplerException()
    {
    }

    public TestSamplerException(string message)
        : base(message)
    {
    }

    public TestSamplerException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    protected TestSamplerException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }
}
