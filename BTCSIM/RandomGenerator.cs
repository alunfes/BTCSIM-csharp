using System;
using System.Collections.Generic;
using System.Linq;

namespace BTCSIM
{
    static public class RandomGenerator
    {
        static Random rnd;
        public static void initialize()
        {
            rnd = new System.Random();
        }

        public static double[] getRandomArray(int num)
        {
            double[] res = new double[num];
            for (int i = 0; i < num; i++)
                res[i] = (rnd.NextDouble() * 2.0) - 1.0;
            return res;
        }

        public static double getRandomArrayRange(int minv, int maxv)
        {
            double res = (rnd.Next(minv * 1000, maxv * 1000)) / 1000.0;
            return res;
        }
    }
}