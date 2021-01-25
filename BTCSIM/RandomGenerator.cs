using System;
using System.Collections.Generic;
using System.Linq;

namespace BTCSIM
{
    public static class RandomSeed
    {
        public static Random rnd { get; set; }

        public static void initialize()
        {
            rnd = new Random();
        }
    }
    
    
    class RandomGenerator
    {
        public double[] getRandomArray(int num)
        {
            double[] res = new double[num];
            for (int i = 0; i < num; i++)
                res[i] = (RandomSeed.rnd.Next(-10000,10000)) /10000.0;
                //res[i] = (RandomSeed.rnd.NextDouble() * 2.0) - 1.0;
            return res;
        }

        public double getRandomArrayRange(int minv, int maxv)
        {
            double res = (RandomSeed.rnd.Next(minv * 1000, maxv * 1000)) / 1000.0;
            return res;
        }
    }
}