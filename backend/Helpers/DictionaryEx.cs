using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace fdocheck
{
    static class DictionaryEx
    {
        public static string GetValueDefault(this Dictionary<string, string> obj, string key, string defaultValue = null)
        {
            return obj.ContainsKey(key) ? obj[key] : defaultValue;
        }
    }
}
