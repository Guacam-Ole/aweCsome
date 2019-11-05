using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AweCsome
{
    public static class FileHelper
    {
        private static string NextSuffix(ref long value, string suffix, out bool changed)
        {
            changed = false;
            if (value > 1024)
            {
                value = value / 1024;
                changed = true;
                return suffix;
            }
            return null;
        }

        public static string PrettyLong(long value)
        {
            string suffix = NextSuffix(ref value, "KB", out bool changed) ?? "Bytes";
            if (changed) suffix = NextSuffix(ref value, "MB", out changed) ?? "KB";
            if (changed) suffix = NextSuffix(ref value, "GB", out changed) ?? "MB";
            if (changed) suffix = NextSuffix(ref value, "TB", out changed) ?? "GB";
            return $"{value} {suffix}";
        }
    }
}
