// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using OpenCensus.Trace.Propagation;

namespace Steeltoe.Management.Census.Trace.Propagation
{
    public sealed class B3PropagationComponent : PropagationComponentBase
    {
        private readonly ThrowsBinaryFormat _binaryFormat = new ThrowsBinaryFormat();
        private readonly B3Format _textFormat = new B3Format();

        /// <inheritdoc/>
        public override IBinaryFormat BinaryFormat
        {
            get
            {
                return this._binaryFormat;
            }
        }

        /// <inheritdoc/>
        public override ITextFormat TextFormat
        {
            get
            {
                return this._textFormat;
            }
        }
    }
}
