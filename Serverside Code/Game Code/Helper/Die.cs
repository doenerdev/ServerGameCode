using System;
using System.Collections.Generic;
using System.Text;
using ServerGameCode.Helper;

namespace ServerGameCode.Helper
{
    public class Die
    {
        private readonly RandomGenerator _rndGenerator;

        public Die(RandomGenerator rndGenerator)
        {
            _rndGenerator = rndGenerator;
        }

        public int RollW12()
        {
            return RollW6() + RollW6();
        }

        public int RollW6()
        {
            return _rndGenerator.RandomRange(1, 7);
        }
    }

}
