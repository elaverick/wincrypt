using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Numerics;

namespace Crypt
{
    class blumblumshub
    {
        private readonly int _p;       // p
        private readonly int _q;      // q
                                     // Modulus = p * q
        private readonly int _seed; // Seed or Start Value
 
        private int _currentValue;
 
        public blumblumshub(int p, int q, int seed)
        {
            _seed = seed;
            _p = p;
            _q = q;
            _currentValue = _seed;
        }
 
        public int Generate()
        {
            _currentValue = (int) ((((BigInteger)_currentValue) * ((BigInteger)_currentValue)) % (_p * _q));
            return _currentValue;
        }
 
 
        public static List<int> GenerateFullCycle(int p, int q, int seed)
        {
            var randomNumbers = new List<int>();
            var bbs = new blumblumshub(p, q, seed);
 
            int rvalue = bbs.Generate();
            while (randomNumbers.Contains(rvalue) == false)
            {
                randomNumbers.Add(rvalue);
                rvalue = bbs.Generate();
            }
 
            return randomNumbers;
        }
    }
}
