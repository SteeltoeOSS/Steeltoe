//
// Copyright 2015 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//

namespace Steeltoe.Discovery.Client
{
    public abstract class AbstractOptions
    {

        protected virtual bool GetBoolean(string strValue, bool def)
        {

            bool result = def;
            if (!string.IsNullOrEmpty(strValue))
            {
                bool.TryParse(strValue, out result);
            }
            return result;
        }
        protected virtual int GetInt(string strValue, int def)
        {

            int result = def;
            if (!string.IsNullOrEmpty(strValue))
            {
                int.TryParse(strValue, out result);
            }
            return result;
        }
        protected virtual string GetString(string strValue, string def)
        {
            if (strValue == null)
                return def;
            return strValue;
        }
    }
}
