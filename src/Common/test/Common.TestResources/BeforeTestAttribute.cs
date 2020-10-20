using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Xunit.Sdk;

namespace Steeltoe.Common.TestResources
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class BeforeTestAttribute: BeforeAfterTestAttribute
    {
        //public Action Action { get; }

        //public BeforeTestAttribute(Action action)
        //{
        //    Action = action;
        //}

        public override void Before(MethodInfo methodUnderTest)
        {

         //   methodUnderTest.Invoke(null, null);
        }

        public override void After(MethodInfo methodUnderTest) 
        {   
        }
    }
}
