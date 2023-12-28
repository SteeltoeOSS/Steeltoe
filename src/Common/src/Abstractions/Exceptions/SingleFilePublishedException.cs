// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace Steeltoe.Common.Exceptions;

public class SingleFilePublishedException : Exception
{
    public SingleFilePublishedException()
    {

    }

    public SingleFilePublishedException(string message) : base(message)
    {

    }

    public string BaseDirectory { get; set; }
}
