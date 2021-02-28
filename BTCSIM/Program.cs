using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading;


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
                combined_num_win += ac_list[i].performance_data.num_win;
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
        private static SimAccount doSim(int from, int to, int max_amount, int sim_type, int best_island_id, int[] index, bool display_chart, double nn_threshold)
        {
            Console.WriteLine("Started Read Weight SIM");
            var ga = new GA(0);
            var chromo = ga.readWeights(best_island_id, false);
            if (sim_type == 0)
                return ga.sim_ga_limit(from, to, max_amount, chromo, from.ToString() + " - " + to.ToString() + ", dt:" + MarketData.Dt[from].ToString() + " - " + MarketData.Dt[to - 1] + ", Best Island=" + best_island_id.ToString(), display_chart);
            else
                return ga.sim_ga_market_limit(from, to, max_amount, chromo, from.ToString() + " - " + to.ToString() + ", dt:" + MarketData.Dt[from].ToString() + " - " + MarketData.Dt[to - 1] + ", Best Island=" + best_island_id.ToString(), display_chart, nn_threshold);
            Thread.Sleep(3);
        }

        private static int doGA(int from, int to, int max_amount,  int num_island, int num_chromo, int num_generation, int banned_move_period, int[] units, double mutation_rate, double move_ratio,  int[] index, bool display_chart, double nn_threshold)
        {
            Console.WriteLine("Started Island GA SIM");
            RandomSeed.initialize();
            var ga_island = new GAIsland();
            ga_island.start_ga_island(from, to, max_amount, num_island, banned_move_period, move_ratio, num_chromo, num_generation, units, mutation_rate, 1, nn_threshold, index);
            return ga_island.best_island;
        }

        private static SimAccount doMultiSim(int from, int to, int max_amount, List<int> best_chrom_log_id, bool display_chart, List<double> nn_threshold)
        {
            Console.WriteLine("Started Multi SIM");
            var chromos = new Gene2[best_chrom_log_id.Count];
            var ga = new GA(0);
            for (int i = 0; i < best_chrom_log_id.Count; i++)
            {
                chromos[i] = ga.readWeights(best_chrom_log_id[i], true);
            }
            var title = "Combined PL Ratio - " + from.ToString() + " - " + to.ToString() + ", dt:" + MarketData.Dt[from].ToString() + " - " + MarketData.Dt[to - 1];
            return ga.sim_ga_multi_chromo(from, to, max_amount, chromos.ToList(), title, display_chart, nn_threshold);
        }

        public static void Main(string[] args)
        {
            Console.WriteLine("# of CPU cores=" + System.Environment.ProcessorCount.ToString());

            var key = "";
            while (true)
            {
                Console.WriteLine("\"ga\" : island GA");
                Console.WriteLine("\"sim\" : read sim");
                Console.WriteLine("\"mul ga\" : multi strategy ga");
                Console.WriteLine("\"mul sim\" : multi strategy sim");
                key = Console.ReadLine();
                if (key == "ga" || key == "sim" || key == "mul ga" || key == "mul sim")
                    break;
            }

            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            Console.WriteLine("started program.");
            List<int> terms = new List<int>();
            for (int i = 10; i < 1000; i = i + 100) { terms.Add(i); }
            MarketData.initializer(terms);

            var from = 1000;
            var to = 501000;
            int max_amount = 5;
            var index = new int[] { 0, 0, 0, 1 ,1, 0, 0};
            double nn_threshold = 0.5;
            int best_island_id = 1;
            bool display_chart = true;
            var sim_type = 1; //0:limit, 1:market/limit

            //read weight sim
            if (key == "sim")
            {
                var ac = doSim(from, to, max_amount, sim_type, best_island_id, index, display_chart, nn_threshold);
            }
            //island ga
            else if (key == "ga")
            {
                int num_island = 2;
                int num_chromos = 4;
                int num_generations = 20;
                int banned_move_period = 2;
                var units = new int[] { 69, 5, 5, 5, 5 };
                var mutation_rate = 0.5;
                var move_ratio = 0.2;
                best_island_id = doGA(from, to, max_amount, num_island, num_chromos, num_generations, banned_move_period, units, mutation_rate, move_ratio, index, display_chart, nn_threshold);
                doSim(from, to, max_amount, sim_type, best_island_id, index, display_chart, nn_threshold);
                doSim(to, MarketData.Close.Count-1, max_amount, sim_type, best_island_id, index, display_chart, nn_threshold);
            }
            //multi strategy combination sim
            else if (key == "mul ga")
            {
                var index_list = new List<int[]> { new int[] { 0, 0, 0, 1, 1, 0, 0 }, new int[] { 0, 0, 0, 1, 1, 0, 0 } };
                var units_list = new List<int[]> { new int[] { 37, 5, 5, 5, 5 }, new int[] { 17, 5, 5, 5 }};
                var best_pl_list = new List<List<double>>();
                var best_ac_list = new List<SimAccount>();
                int num_island = 2;
                int num_chromos = 4;
                int num_generations = 60;
                int banned_move_period = 2;
                var mutation_rate = 0.5;
                var move_ratio = 0.2;
                var id_list = new List<int>();
                var nn_threshold_list = new List<double>();
                for (int i = 0; i < index_list.Count; i++)
                {
                    best_island_id = doGA(from, to, max_amount, num_island, num_chromos, num_generations, banned_move_period, units_list[i], mutation_rate, move_ratio, index_list[i], display_chart, nn_threshold);
                    var ac = doSim(from, to, max_amount, sim_type, best_island_id, index_list[i], display_chart, nn_threshold);
                    best_pl_list.Add(ac.total_pl_ratio_list);
                    best_ac_list.Add(ac);
                    if (File.Exists(@"./log_best_weight_ID-" + i.ToString() + ".csv"))
                        File.Delete(@"./log_best_weight_ID-" + i.ToString() + ".csv");
                    File.Copy(@"./best_weight_ID-" + best_island_id.ToString() + ".csv", @"./log_best_weight_ID-" + i.ToString() + ".csv");
                    id_list.Add(i);
                    nn_threshold_list.Add(nn_threshold);
                }
                doMultiSim(from, to, max_amount, id_list, true, nn_threshold_list);
                doMultiSim(to, MarketData.Close.Count-1, max_amount, id_list, true, nn_threshold_list);

            }
            else if (key == "mul sim")
            {
                var num_best_chromo = 4;
                var id_list = new List<int>();
                var nn_threshold_list = new List<double>();
                for (int i = 0; i < num_best_chromo; i++)
                {
                    id_list.Add(i);
                    nn_threshold_list.Add(nn_threshold);
                }
                doMultiSim(from, to, max_amount, id_list, true, nn_threshold_list);
            }
            stopWatch.Stop();
            Console.WriteLine("Completed all processes.");
            Console.WriteLine("Time Elapsed (sec)=" + stopWatch.Elapsed.TotalSeconds.ToString());
        }
    }
}
