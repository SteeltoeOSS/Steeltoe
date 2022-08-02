// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Steeltoe.Common.Expression.Internal.Spring.Standard;
using Steeltoe.Common.Expression.Internal.Spring.Support;
using Steeltoe.Common.Util;
using Xunit;

namespace Steeltoe.Common.Expression.Internal.Spring;

public class ScenariosForSpringSecurityExpressionTests : AbstractExpressionTests
{
    [Fact]
    public void TestScenario01_Roles()
    {
        var parser = new SpelExpressionParser();
        var ctx = new StandardEvaluationContext();
        IExpression expr = parser.ParseRaw("HasAnyRole('MANAGER','TELLER')");

        ctx.SetRootObject(new Person("Ben"));
        object value = expr.GetValue(ctx, typeof(bool));
        Assert.False((bool)value);

        ctx.SetRootObject(new Manager("Luke"));
        value = expr.GetValue(ctx, typeof(bool));
        Assert.True((bool)value);
    }

    [Fact]
    public void TestScenario02_ComparingNames()
    {
        var parser = new SpelExpressionParser();
        var ctx = new StandardEvaluationContext();

        ctx.AddPropertyAccessor(new SecurityPrincipalAccessor());

        // Multiple options for supporting this expression: "p.name == principal.name"
        // (1) If the right person is the root context object then "name==principal.name" is good enough
        IExpression expr = parser.ParseRaw("Name == Principal.Name");

        ctx.SetRootObject(new Person("Andy"));
        object value = expr.GetValue(ctx, typeof(bool));
        Assert.True((bool)value);

        ctx.SetRootObject(new Person("Christian"));
        value = expr.GetValue(ctx, typeof(bool));
        Assert.False((bool)value);

        // (2) Or register an accessor that can understand 'p' and return the right person
        expr = parser.ParseRaw("P.Name == Principal.Name");

        var pAccessor = new PersonAccessor();
        ctx.AddPropertyAccessor(pAccessor);
        ctx.SetRootObject(null);

        pAccessor.ActivePerson = new Person("Andy");
        value = expr.GetValue(ctx, typeof(bool));
        Assert.True((bool)value);

        pAccessor.ActivePerson = new Person("Christian");
        value = expr.GetValue(ctx, typeof(bool));
        Assert.False((bool)value);
    }

    [Fact]
    public void TestScenario03_Arithmetic()
    {
        var parser = new SpelExpressionParser();
        var ctx = new StandardEvaluationContext();

        // Might be better with a as a variable although it would work as a property too...
        // Variable references using a '#'
        IExpression expr = parser.ParseRaw("(HasRole('SUPERVISOR') or (#a <  1.042)) and HasIpAddress('10.10.0.0/16')");

        bool value;

        ctx.SetVariable("a", 1.0d); // referenced as #a in the expression
        ctx.SetRootObject(new Supervisor("Ben")); // so non-qualified references 'hasRole()' 'hasIpAddress()' are invoked against it
        value = expr.GetValue<bool>(ctx);
        Assert.True(value);

        ctx.SetRootObject(new Manager("Luke"));
        ctx.SetVariable("a", 1.043d);
        value = expr.GetValue<bool>(ctx);
        Assert.False(value);
    }

    // Here i'm going to change which hasRole() executes and make it one of my own Java methods
    [Fact]
    public void TestScenario04_ControllingWhichMethodsRun()
    {
        var parser = new SpelExpressionParser();
        var ctx = new StandardEvaluationContext();

        ctx.SetRootObject(new Supervisor("Ben")); // so non-qualified references 'hasRole()' 'hasIpAddress()' are invoked against it);

        // NEEDS TO OVERRIDE THE REFLECTION ONE - SHOW REORDERING MECHANISM
        // Might be better with a as a variable although it would work as a property too...
        // Variable references using a '#'
        // SpelExpression expr = parser.parseExpression("(hasRole('SUPERVISOR') or (#a <  1.042)) and hasIpAddress('10.10.0.0/16')");
        ctx.AddMethodResolver(new MyMethodResolver());

        IExpression expr = parser.ParseRaw("(HasRole(3) or (#a <  1.042)) and HasIpAddress('10.10.0.0/16')");

        bool value;

        ctx.SetVariable("a", 1.0d); // referenced as #a in the expression
        value = expr.GetValue<bool>(ctx);
        Assert.True(value);

        // ctx.setRootObject(new Manager("Luke"));
        // ctx.setVariable("a",1.043d);
        // value = (bool)expr.GetValue(ctx,typeof(bool));
        // assertFalse(value);
    }

    public class Person
    {
        public virtual string[] Roles =>
            new[]
            {
                "NONE"
            };

        public virtual string Name { get; }

        public Person(string n)
        {
            Name = n;
        }

        public virtual bool HasAnyRole(params string[] roles)
        {
            if (roles == null)
            {
                return true;
            }

            string[] myRoles = Roles;

            foreach (string myRole in myRoles)
            {
                if (roles.Any(role => myRole.Equals(role)))
                {
                    return true;
                }
            }

            return false;
        }

        public virtual bool HasRole(string role)
        {
            return HasAnyRole(role);
        }

        public virtual bool HasIpAddress(string ipAddress)
        {
            return true;
        }
    }

    public class Manager : Person
    {
        public override string[] Roles =>
            new[]
            {
                "MANAGER"
            };

        public Manager(string n)
            : base(n)
        {
        }
    }

    public class Teller : Person
    {
        public override string[] Roles =>
            new[]
            {
                "TELLER"
            };

        public Teller(string n)
            : base(n)
        {
        }
    }

    public class Supervisor : Person
    {
        public override string[] Roles =>
            new[]
            {
                "SUPERVISOR"
            };

        public Supervisor(string n)
            : base(n)
        {
        }
    }

    public class SecurityPrincipalAccessor : IPropertyAccessor
    {
        public bool CanRead(IEvaluationContext context, object target, string name)
        {
            return name.Equals("Principal");
        }

        public ITypedValue Read(IEvaluationContext context, object target, string name)
        {
            return new TypedValue(new Principal());
        }

        public bool CanWrite(IEvaluationContext context, object target, string name)
        {
            return false;
        }

        public void Write(IEvaluationContext context, object target, string name, object newValue)
        {
        }

        public IList<Type> GetSpecificTargetClasses()
        {
            return null;
        }

        public class Principal
        {
            public string Name = "Andy";
        }
    }

    public class PersonAccessor : IPropertyAccessor
    {
        public Person ActivePerson { get; set; }

        public bool CanRead(IEvaluationContext context, object target, string name)
        {
            return name.Equals("P");
        }

        public ITypedValue Read(IEvaluationContext context, object target, string name)
        {
            return new TypedValue(ActivePerson);
        }

        public bool CanWrite(IEvaluationContext context, object target, string name)
        {
            return false;
        }

        public void Write(IEvaluationContext context, object target, string name, object newValue)
        {
        }

        public IList<Type> GetSpecificTargetClasses()
        {
            return null;
        }
    }

    public class MyMethodResolver : IMethodResolver
    {
        public IMethodExecutor Resolve(IEvaluationContext context, object targetObject, string name, List<Type> argumentTypes)
        {
            if (name.Equals("HasRole"))
            {
                return new HasRoleExecutor(context.TypeConverter);
            }

            return null;
        }

        public class HasRoleExecutor : IMethodExecutor
        {
            private readonly ITypeConverter _tc;

            public HasRoleExecutor(ITypeConverter typeConverter)
            {
                _tc = typeConverter;
            }

            public static bool HasRole(params string[] strings)
            {
                return true;
            }

            public ITypedValue Execute(IEvaluationContext context, object target, params object[] arguments)
            {
                try
                {
                    MethodInfo m = typeof(HasRoleExecutor).GetMethod(nameof(HasRole), new[]
                    {
                        typeof(string[])
                    });

                    object[] args = arguments;

                    if (args != null)
                    {
                        ReflectionHelper.ConvertAllArguments(_tc, args, m);
                    }

                    if (m.IsVarArgs())
                    {
                        args = ReflectionHelper.SetupArgumentsForVarargsInvocation(ClassUtils.GetParameterTypes(m), args);
                    }

                    return new TypedValue(m.Invoke(null, args), m.ReturnType);
                }
                catch (Exception ex)
                {
                    throw new AccessException("Problem invoking hasRole", ex);
                }
            }
        }
    }
}
