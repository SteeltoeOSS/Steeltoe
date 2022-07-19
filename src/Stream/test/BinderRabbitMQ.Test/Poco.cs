// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Stream.Binder.Rabbit;

public class Poco
{
#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter
    // ReSharper disable once InconsistentNaming
    public string field;
#pragma warning restore SA1307 // Accessible fields should begin with upper-case letter

    public Poco()
    {
    }

    public Poco(string field)
    {
        this.field = field;
    }
}
