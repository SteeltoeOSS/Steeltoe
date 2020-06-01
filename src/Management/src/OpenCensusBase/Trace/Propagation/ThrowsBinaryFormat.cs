// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using OpenCensus.Trace;
using OpenCensus.Trace.Propagation;

namespace Steeltoe.Management.Census.Trace.Propagation
{
    public class ThrowsBinaryFormat : BinaryFormatBase
    {
        public override ISpanContext FromByteArray(byte[] bytes)
        {
            throw new System.NotImplementedException();
        }

        public override byte[] ToByteArray(ISpanContext spanContext)
        {
            throw new System.NotImplementedException();
        }
    }
}
