// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text;

namespace Steeltoe.Common.Transaction;

public class DefaultTransactionAttribute : DefaultTransactionDefinition, ITransactionAttribute
{
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

    public string Qualifier { get; }

    public string Descriptor { get; }

    public bool RollbackOn(Exception exception)
    {
        if (exception is OutOfMemoryException ||
            exception is InvalidProgramException ||
            exception is AccessViolationException ||
            exception is StackOverflowException ||
            exception is BadImageFormatException)
        {
            return true;
        }

        return false;
    }

    protected StringBuilder GetAttributeDescription()
    {
        var result = GetDefinitionDescription();
        if (!string.IsNullOrEmpty(Qualifier))
        {
            result.Append("; '").Append(Qualifier).Append('\'');
        }

        return result;
    }
}
