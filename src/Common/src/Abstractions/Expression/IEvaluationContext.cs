// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Steeltoe.Common.Expression.Internal
{
    /// <summary>
    /// Expressions are executed in an evaluation context. It is in this context that
    /// references are resolved when encountered during expression evaluation.
    /// TODO:  This interface is not complete
    /// </summary>
    public interface IEvaluationContext
    {
        ITypedValue RootObject { get; }

        List<IPropertyAccessor> PropertyAccessors { get; }

        List<IConstructorResolver> ConstructorResolvers { get; }

        List<IMethodResolver> MethodResolvers { get; }

        IServiceResolver ServiceResolver { get; }

        ITypeLocator TypeLocator { get; }

        ITypeConverter TypeConverter { get; }

        ITypeComparator TypeComparator { get; }

        IOperatorOverloader OperatorOverloader { get; }

        void SetVariable(string name, object value);

        object LookupVariable(string name);

        T LookupVariable<T>(string name);
    }
}
