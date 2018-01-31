using System;
using System.Collections.Generic;
using System.Security.Permissions;
using System.Text;

namespace ServerGameCode.Helper
{
    //single ranodm istance: https://stackoverflow.com/questions/5359540/random-numbers-for-dice-Match
    public class RandomGenerator
    {
        private readonly Random _rnd;

        public RandomGenerator()
        {
            _rnd = new Random();;
        }

        public int RandomRange(int start, int end) //end is exlusive
        {
            return _rnd.Next(start, end);
        }

        public double RandomRange(double start, double end)
        {
            return _rnd.NextDouble() * (end - start) + start;
        }
    }

}
