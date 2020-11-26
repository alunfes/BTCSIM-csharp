using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;


namespace BTCSIM
{
    class CombinedAC
    {
        public static (List<double>, int, double, double) calcCombinedAC(List<SimAccount> ac_list) //totla_pl_log, num_trade, win_rate, sharp ratio
        {
            var combined_total_pl = new List<double>();
            double combined_num_trade = 0;
            double combined_num_win = 0;
            double combined_win_rate = 0;
            double combined_sharp_ratio = 0;
            double current_pl = 0;
            for (int i = 0; i < ac_list.Count; i++)
            {
                for (int j = 0; j < ac_list[i].total_pl_list.Count; j++)
                {
                    combined_total_pl.Add(ac_list[i].total_pl_list[j] + current_pl);
                }
                current_pl = combined_total_pl.Last();
                combined_num_trade += ac_list[i].performance_data.num_trade;
                combined_num_win = ac_list[i].performance_data.num_win;
            }
            combined_win_rate = Math.Round(combined_num_win / combined_num_trade, 4);

            List<double> change = new List<double>();
            for (int i = 1; i < combined_total_pl.Count; i++)
            {
                if (combined_total_pl[i - 1] != 0)
                    change.Add((combined_total_pl[i] - combined_total_pl[i - 1]) / combined_total_pl[i - 1]);
                else
                    change.Add(0);
            }
            var doubleList = change.Select(a => Convert.ToDouble(a)).ToArray();

            //平均値算出
            double mean = doubleList.Average();
            //自乗和算出
            double sum2 = doubleList.Select(a => a * a).Sum();
            //分散 = 自乗和 / 要素数 - 平均値^2
            double variance = sum2 / Convert.ToDouble(doubleList.Length) - mean * mean;
            //標準偏差 = 分散の平方根
            var stdv = Math.Sqrt(variance);
            if (stdv != 0)
                combined_sharp_ratio = Math.Round(combined_total_pl.Last() / stdv, 4);
            else
                combined_sharp_ratio = 0;

            return (combined_total_pl, Convert.ToInt32(combined_num_trade), combined_win_rate, combined_sharp_ratio);
        }
    }


    class MainClass
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("# of CPU cores="+System.Environment.ProcessorCount.ToString());

            var key = "";
            while (true)
            {
                Console.WriteLine("\"ga\" : island GA");
                Console.WriteLine("\"sim\" : read sim");
                Console.WriteLine("\"conti_ga\" : conti island GA and SIM");

                key = Console.ReadLine();
                if (key == "ga" || key == "sim" || key == "conti_ga")
                    break;
            }

            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            Console.WriteLine("started program.");
            List<int> terms = new List<int>();
            for(int i=100; i<1000; i = i + 100) { terms.Add(i); }

            MarketData.initializer(terms);

            
            //Read Weight Sim
            if (key == "sim")
            {
                Console.WriteLine("Started Read Weight SIM");
                var ga = new GA(0);
                var chromo = ga.readWeights(0);
                //var from = 1000 + Convert.ToInt32(Math.Round(MarketData.Close.Count * 0.8));
                var from = 130000;
                var to = from + 10000;
                int max_amount = 10;
                //var to = MarketData.Close.Count -1;
                var ac = ga.sim_ga_limit(from, to, max_amount, chromo, from.ToString() + " - " + to.ToString());
                //var ac = ga.sim_ga_limit(Convert.ToInt32(MarketData.Close.Count * 0.05), MarketData.Close.Count - 1, chromo);
                //var ac = ga.sim_ga(Convert.ToInt32(MarketData.Close.Count * 0.05), MarketData.Close.Count-1, chromo);
                //var ac = ga.sim_ga(1000, Convert.ToInt32(MarketData.Close.Count * 0.05), chromo);
            }


            //Island GA
            if (key == "ga")
            {
                Console.WriteLine("Started Island GA SIM");
                RandomSeed.initialize();
                int from = 100000;
                int num_island = 10;
                int num_chromos = 8;
                int num_generations = 8;
                int banned_move_period = 3;
                int max_amount = 10;
                var units = new int[] { 11, 30, 4 };
                var mutation_rate = 0.9;
                var move_ratio = 0.2;
                //int to = Convert.ToInt32(Math.Round(MarketData.Close.Count * 0.8)) + from;
                int to = 30000 + from;
                var ga_island = new GAIsland();
                ga_island.start_ga_island(from, to, max_amount, num_island, banned_move_period, move_ratio, num_chromos, num_generations, units, mutation_rate);

                var ga = new GA(0);
                var chromo = ga.readWeights(ga_island.best_island);
                var ac = ga.sim_ga_limit(from, to, max_amount, chromo, from.ToString() + " - " + to.ToString());

            }
            if (key == "conti_ga")
            {
                Console.WriteLine("Started Island GA SIM");
                RandomSeed.initialize();
                int sim_window = 2000;
                int ga_window = 30000;
                int start_ind = 10000;
                int num_island = 5;
                int num_chromos = 8;
                int num_generations = 5;
                int banned_move_period = 3;
                int max_amount = 10;
                var units = new int[] { 11, 20, 4 };
                var mutation_rate = 0.9;
                var move_ratio = 0.2;
                var ac_list = new List<SimAccount>();
                for (int i = 0; i < 3; i++)
                {
                    int ga_from = i * ga_window + start_ind;
                    int ga_to = ga_from + ga_window;
                    int sim_from = ga_to;
                    int sim_to = sim_from + sim_window;
                    Console.WriteLine("Conti GA SIM i=" + i.ToString() + ", ga period=" + ga_from.ToString() + " - " + ga_to.ToString() + ", sim period=" + sim_from.ToString() + " - " + sim_to.ToString());
                    var ga_island = new GAIsland();
                    ga_island.start_ga_island(ga_from, ga_to, max_amount, num_island, banned_move_period, move_ratio, num_chromos, num_generations, units, mutation_rate);
                    
                    var ga = new GA(0);
                    var chromo = ga.readWeights(ga_island.best_island);
                    var ac = new SimAccount();
                    ac = ga.sim_ga_limit_conti(sim_from, sim_to, max_amount, chromo, sim_from.ToString() + " - " + sim_to.ToString(), ac);
                    ac_list.Add(ac);
                }
                (List<double> combined_total_pl, int comined_num_trade, double combined_win_rate, double combined_sharp_ratio) res = CombinedAC.calcCombinedAC(ac_list);
                Console.WriteLine("*************************************************************************");
                Console.WriteLine("term total pl=" + ac_list.Last().performance_data.total_pl.ToString() + ", term num trade=" + ac_list.Last().performance_data.num_trade.ToString() + ", term win rate="+ ac_list.Last().performance_data.win_rate.ToString() + ", term sharp ratio="+ ac_list.Last().performance_data.sharp_ratio.ToString());
                Console.WriteLine("combined total pl=" + res.combined_total_pl.Last().ToString() + ", combined num trade=" + res.comined_num_trade.ToString() + ", combined win rate=" + res.combined_win_rate.ToString() + ", combined sharp ratio=" + res.combined_sharp_ratio.ToString());
                Console.WriteLine("*************************************************************************");
                System.Threading.Thread.Sleep(3000);
                LineChart.DisplayLineChart(res.combined_total_pl, "Conti sim: "+ ga_window + start_ind +", combined num trade=" + res.comined_num_trade.ToString() + ", combined win rate=" + res.combined_win_rate.ToString());
            }
            stopWatch.Stop();
            Console.WriteLine("Completed all processes.");
            Console.WriteLine("Time Elapsed (min)=" + stopWatch.Elapsed.Minutes.ToString());
        }


        

    }
}
