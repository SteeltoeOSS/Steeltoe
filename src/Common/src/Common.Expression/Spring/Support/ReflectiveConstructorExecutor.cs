// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;
using System;
using System.Reflection;

namespace Steeltoe.Common.Expression.Internal.Spring.Support
{
    public class ReflectiveConstructorExecutor : IConstructorExecutor
    {
        private readonly int? _varargsPosition;

        public ConstructorInfo Constructor { get; }

        public ReflectiveConstructorExecutor(ConstructorInfo ctor)
        {
            Constructor = ctor;
            if (ctor.IsVarArgs())
            {
                _varargsPosition = ctor.GetParameters().Length - 1;
            }
            else
            {
                _varargsPosition = null;
            }
        }

        public ITypedValue Execute(IEvaluationContext context, params object[] arguments)
        {
            try
            {
                ReflectionHelper.ConvertArguments(context.TypeConverter, arguments, Constructor, _varargsPosition);
                if (Constructor.IsVarArgs())
                {
                    arguments = ReflectionHelper.SetupArgumentsForVarargsInvocation(ClassUtils.GetParameterTypes(Constructor), arguments);
                }

                return new TypedValue(Constructor.Invoke(arguments));
            }
            catch (Exception ex)
            {
                throw new AccessException("Problem invoking constructor: " + Constructor, ex);
            }
        }
    }
}
