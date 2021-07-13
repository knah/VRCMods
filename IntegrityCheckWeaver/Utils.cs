using System;
using System.Linq;

namespace IntegrityCheckWeaver
{
    public static class Utils
    {
        private static readonly Random ourRandom = new(DateTime.Now.GetHashCode());
        private static readonly (char, int)[] ourFrequencyTable = {
            ('E', 21912), ('T', 16587), ('A', 14810), ('O', 14003), ('I', 13318), ('N', 12666), ('S', 11450),
            ('R', 10977), ('H', 10795), ('D', 7874), ('L', 7253), ('U', 5246), ('C', 4943), ('M', 4761), ('F', 4200),
            ('Y', 3853), ('W', 3819), ('G', 3693), ('P', 3316), ('B', 2715), ('V', 2019), ('K', 1257), ('X', 315), 
            ('Q', 205), ('J', 188), ('Z', 128),
        };

        private static readonly int ourFrequencyTableSum = ourFrequencyTable.Sum(it => it.Item2);

        private static char RandomLetter()
        {
            var v = ourRandom.Next(0, ourFrequencyTableSum);
            var i = 0;
            while (v > ourFrequencyTable[i].Item2)
            {
                v -= ourFrequencyTable[i].Item2;
                i++;
            }

            return char.ToLowerInvariant(ourFrequencyTable[i].Item1);
        }
        
        internal static string CompletelyRandomString()
        {
            var length = ourRandom.Next(5, 21);
            var chars = new char[length];

            for (var i = 0; i < length; i++)
            {
                chars[i] = RandomLetter();
                if (ourRandom.Next(0, i + 1) == 0)
                    chars[i] = char.ToUpperInvariant(chars[i]);
            }

            return new string(chars);
        }

        internal static int RandomInt(int from, int to)
        {
            return ourRandom.Next(from, to);
        }
    }
}