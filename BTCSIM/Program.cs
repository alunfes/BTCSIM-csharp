using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace BTCSIM
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            List<int> terms = new List<int>();
            for(int i=100; i<10000; i = i + 100) { terms.Add(i); }
            MarketData.initializer(terms);

            for (int i = 0; i < 1; i++)
            {
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();
                var ac = new SimAccount();
                var sim = new Sim();
                ac = sim.sim_trendfollow_limit(10000, MarketData.Close.Count-1, (i + 1) * 100, 0.01, -0.01, 0.99, 0.99, ac);
                ac.calc_sharp_ratio();
                stopWatch.Stop();
                Console.WriteLine("Completed sim. " + i.ToString());
                Console.WriteLine(stopWatch.Elapsed.Seconds.ToString() + " seconds.");
                Console.WriteLine("pl=" + ac.performance_data.total_pl);
                Console.WriteLine("num trade=" + ac.performance_data.num_trade);
                Console.WriteLine("win rate=" + ac.performance_data.win_rate);
                Console.WriteLine("sharp_ratio=" + ac.performance_data.sharp_ratio);
                TableToCSV.LogDataToCSV(ac.log_data.log_data_table, "./ac.csv");
            }
        }
    }
}
