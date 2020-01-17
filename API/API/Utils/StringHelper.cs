using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace API.Utils
{
    public static class StringHelper
    {
        public static string StripHTML(this string HTMLText)
        {
            var reg = new Regex("<[^>]+>", RegexOptions.IgnoreCase);
            return reg.Replace(HTMLText, "");
        }
    }
}


