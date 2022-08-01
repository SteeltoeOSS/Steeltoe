// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Messaging.RabbitMQ.Config;

public abstract class AbstractDeclarable : IDeclarable
{
    protected List<object> declaringAdmins = new ();

    protected AbstractDeclarable(Dictionary<string, object> arguments)
    {
        ShouldDeclare = true;
        Arguments = arguments != null ? new Dictionary<string, object>(arguments) : new Dictionary<string, object>();
    }

    public bool ShouldDeclare { get; set; }

    public Dictionary<string, object> Arguments { get; set; }

    public List<object> DeclaringAdmins
    {
        get
        {
            return declaringAdmins;
        }

        set
        {
            declaringAdmins = value;
        }
    }

    public bool IgnoreDeclarationExceptions { get; set; }

    public virtual void SetAdminsThatShouldDeclare(params object[] adminArgs)
    {
        var admins = new List<object>();
        if (adminArgs != null)
        {
            if (adminArgs.Length > 1)
            {
                foreach (var a in adminArgs)
                {
                    if (a == null)
                    {
                        throw new InvalidOperationException("'admins' cannot contain null elements");
                    }
                }
            }

            if (adminArgs.Length > 0 && !(adminArgs.Length == 1 && adminArgs[0] == null))
            {
                admins.AddRange(adminArgs);
            }
        }

        declaringAdmins = admins;
    }

    public void AddArgument(string name, object value)
    {
        Arguments[name] = value;
    }

    public object RemoveArgument(string name)
    {
        Arguments.Remove(name, out var result);
        return result;
    }
}
