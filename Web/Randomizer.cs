using System;
using System.Linq;

namespace CreativeColon.ChatterClub.Web
{
    class Randomizer
    {
        const string Characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789abcdefghijklmnopqrstuvwxyz";
        static Random Random = new Random();

        public static string GenerateString(byte length)
        {
            return new string(Enumerable.Repeat(Characters, length).Select(s => s[Random.Next(s.Length)]).ToArray());
        }
    }
}
