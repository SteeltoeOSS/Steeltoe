using Steeltoe.Management.Census.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Stats
{
    public sealed class ViewName : IViewName
    {
        internal const int NAME_MAX_LENGTH = 255;
        public String AsString { get; }

        internal ViewName(String asString)
        {
            if (asString == null)
            {
                throw new ArgumentNullException(nameof(asString));
            }
            this.AsString = asString;
        }

        public static IViewName Create(String name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (!(StringUtil.IsPrintableString(name) && name.Length <= NAME_MAX_LENGTH))
            {
                throw new ArgumentOutOfRangeException(
                    "Name should be a ASCII string with a length no greater than "
                    + NAME_MAX_LENGTH
                    + " characters.");
            }

            return new ViewName(name);
        }

        public override String ToString()
        {
            return "Name{"
                + "asString=" + AsString
                + "}";
        }

        public override bool Equals(Object o)
        {
            if (o == this)
            {
                return true;
            }
            if (o is ViewName)
            {
                ViewName that = (ViewName)o;
                return (this.AsString.Equals(that.AsString));
            }
            return false;
        }

        public override int GetHashCode()
        {
            int h = 1;
            h *= 1000003;
            h ^= this.AsString.GetHashCode();
            return h;
        }
    }
}
