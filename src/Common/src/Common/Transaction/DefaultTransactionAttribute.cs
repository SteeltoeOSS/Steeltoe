// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text;

namespace Steeltoe.Common.Transaction;

public class DefaultTransactionAttribute : DefaultTransactionDefinition, ITransactionAttribute
{
    public string Qualifier { get; }

    public string Descriptor { get; }

    public DefaultTransactionAttribute()
    {
    }

    public DefaultTransactionAttribute(ITransactionAttribute other)
        : base(other)
    {
    }

    public DefaultTransactionAttribute(int propagationBehavior)
        : base(propagationBehavior)
    {
    }

    public bool RollbackOn(Exception exception)
    {
        return exception switch
        {
            OutOfMemoryException => true,
            InvalidProgramException => true,
            AccessViolationException => true,
            BadImageFormatException => true,
            _ => false
        };
    }

    protected StringBuilder GetAttributeDescription()
    {
        StringBuilder result = GetDefinitionDescription();

        if (!string.IsNullOrEmpty(Qualifier))
        {
            result.Append("; '").Append(Qualifier).Append('\'');
        }

        return result;
    }
}
