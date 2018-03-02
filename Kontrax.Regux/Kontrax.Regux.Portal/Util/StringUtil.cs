using System;

namespace Kontrax.Regux.Portal
{
    public static class StringUtil
    {
        public static string FirstCapitalLetter(this string s)
        {
            if (s == null)
            {
                return null;
            }
            return s.Substring(0, 1).ToUpper() + s.Substring(1);
        }

        public static string NewLineToBr(this string s)
        {
            if (s == null)
            {
                return null;
            }
            return s.Replace(Environment.NewLine, "<br />");
        }
    }
}
