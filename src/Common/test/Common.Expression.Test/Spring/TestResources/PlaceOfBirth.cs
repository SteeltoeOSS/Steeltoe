// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.Expression.Internal.Spring.TestResources
{
    public class PlaceOfBirth
    {
        public string Country;

        private string city;

        public override string ToString() => city;

        public string City
        {
            get => city;
            set => city = value;
        }

        public PlaceOfBirth(string str) => city = str;

        public int DoubleIt(int i) => i * 2;

        public override bool Equals(object o)
        {
            if (o is not PlaceOfBirth oPob)
            {
                return false;
            }

            return city.Equals(oPob.city);
        }

        public override int GetHashCode() => city.GetHashCode();
    }
}
