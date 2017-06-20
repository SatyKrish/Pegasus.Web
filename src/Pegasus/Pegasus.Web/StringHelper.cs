using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Pegasus.Web
{
    public static class StringHelper
    {
        private const string RandomCharacters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        private static readonly Random RandomStringGenerator = new Random();

        public static string RandomString(int length)
        {
            return new string(Enumerable.Repeat(RandomCharacters, length).Select(s => s[RandomStringGenerator.Next(s.Length)]).ToArray());
        }

        public static T ToEnum<T>(this string value)
        {
            return (T)Enum.Parse(typeof(T), value, true);
        }
    }
}
