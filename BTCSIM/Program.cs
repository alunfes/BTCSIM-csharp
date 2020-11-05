﻿using System;
using System.Collections.Generic;
using System.Diagnostics;


namespace BTCSIM
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            Console.WriteLine("started program.");
            List<int> terms = new List<int>();
            for(int i=100; i<1000; i = i + 100) { terms.Add(i); }

            MarketData.initializer(terms);
            
            //Read Weight Sim
            /*
            Console.WriteLine("Started Read Weight SIM");
            var ga = new GA();
            var chromo = ga.readWeights();
            //var ac = ga.sim_ga(Convert.ToInt32(MarketData.Close.Count * 0.8), MarketData.Close.Count-1, chromo);
            var ac = ga.sim_ga(1000, Convert.ToInt32(MarketData.Close.Count * 0.8), chromo);
            */

            //GA
            
            Console.WriteLine("Started GA SIM");
            RandomSeed.initialize();
            //var chromo = new Gene(new int[] { 14, 50, 3 });
            var sim = new Sim();
            var ac = new SimAccount();
            int from = 1000;
            int num_chromos = 100;
            int num_generations = 10;
            var units = new int[] {14, 50, 3};
            var mutation_rate = 0.8;
            int to = Convert.ToInt32(Math.Round(MarketData.Close.Count * 0.8));
            var ga = new GA();
            ga.start_ga(from, to, num_chromos, num_generations, units, mutation_rate);
            

            stopWatch.Stop();
            /*Console.WriteLine(stopWatch.Elapsed.Seconds.ToString() + " seconds.");
            Console.WriteLine("pl=" + ac.performance_data.total_pl);
            Console.WriteLine("num trade=" + ac.performance_data.num_trade);
            Console.WriteLine("win rate=" + ac.performance_data.win_rate);
            Console.WriteLine("sharp_ratio=" + ac.performance_data.sharp_ratio);
            */

            /*
            RandomGenerator.initialize();
            var units = new int[3] { 10, 50, 3 };
            var gene = new Gene(units);
            var nn = new NN();
            var inputs = RandomGenerator.getRandomArray(10);
            var res = nn.calcNN(inputs, units, gene.weight_gene1, gene.weight_gene2, gene.bias_gene1, gene.bias_gene2, 1);
            foreach (var r in res)
                Console.WriteLine(r);
            Console.WriteLine(nn.getActivatedUnit(res));
            */




            /*   
            var simopt = new SimOptimizer();
            int opt_win_size = 50;
            int kijun_val = 1;
            int num_conti = 0;
            Console.WriteLine("opt_win_size=" + opt_win_size.ToString() + ", kijun_val = " + kijun_val.ToString());
            var ac = simopt.start_optimization(opt_win_size, kijun_val, num_conti, MarketData.Close.Count - opt_win_size - 1, 0.0, num_conti);
            */
            /*
            int k = 0;
            using (var sw = new System.IO.StreamWriter(@"result_conti_random.csv", false))
            {
                sw.WriteLine("opt_win_size,kijun_val,num_conti,total_pl,num_trade,win_rate,fee to pl ratio,sharp ratio");
                for (int i = 0; i < 100; i++)
                {
                    for (int j = 0; j < 10; j++)
                    {
                        Console.WriteLine("No." + k.ToString());
                        k++;
                        var simopt = new SimOptimizer();
                        int opt_win_size = (i + 1) * 5;
                        int kijun_val = 1;
                        int num_conti = j + 1;
                        //Console.WriteLine("opt_win_size=" + opt_win_size.ToString() + ", kijun_val = " + kijun_val.ToString());
                        var ac = simopt.start_optimization(opt_win_size, kijun_val, 10, MarketData.Close.Count - opt_win_size - 1, 0.3, num_conti);
                        Console.WriteLine("opt_win_size=" + opt_win_size.ToString() + ", " + "kijun_val=" + kijun_val.ToString()+", "+"num_conti="+num_conti.ToString()+", "+ac.performance_data.total_pl.ToString()+
                            ", "+ac.performance_data.num_trade.ToString()+", "+ac.performance_data.win_rate.ToString()+", "+(ac.performance_data.total_fee / ac.performance_data.total_pl).ToString());
                        sw.WriteLine("{0},{1},{2},{3},{4},{5},{6},{7}", opt_win_size, kijun_val, num_conti, ac.performance_data.total_pl, ac.performance_data.num_trade, ac.performance_data.win_rate, ac.performance_data.total_fee / ac.performance_data.total_pl, ac.performance_data.sharp_ratio.ToString());
                    }
                }
            }*/

            Console.WriteLine("Completed");



            /*
            var ga = new GA();
            ga.start_ga(30, 300, 0.1, 10000);
            */

            /*
            for (int i = 0; i < 10; i++)
            {
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();
                var ac = new SimAccount();
                var sim = new Sim();
                ac = sim.sim_trendfollow_limit(10000, MarketData.Close.Count-1, (i + 1) * 1000, 0.01, -0.01, 0.99, 0.99, ac);
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
            */
        }
    }
}
