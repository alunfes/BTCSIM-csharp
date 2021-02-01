using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Text;


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
            var chromo = ga.readWeights(best_island_id);
            if (sim_type == 0)
                return ga.sim_ga_limit(from, to, max_amount, chromo, from.ToString() + " - " + to.ToString() + ", dt:" + MarketData.Dt[from].ToString() + " - " + MarketData.Dt[to - 1] + ", Best Island=" + best_island_id.ToString(), display_chart, index);
            else
                return ga.sim_ga_market_limit(from, to, max_amount, chromo, from.ToString() + " - " + to.ToString() + ", dt:" + MarketData.Dt[from].ToString() + " - " + MarketData.Dt[to - 1] + ", Best Island=" + best_island_id.ToString(), display_chart, nn_threshold, index);
        }

        private static int doGA(int from, int to, int max_amount,  int num_island, int num_chromo, int num_generation, int banned_move_period, int[] units, double mutation_rate, double move_ratio,  int[] index, bool display_chart, double nn_threshold)
        {
            Console.WriteLine("Started Island GA SIM");
            RandomSeed.initialize();
            var ga_island = new GAIsland();
            ga_island.start_ga_island(from, to, max_amount, num_island, banned_move_period, move_ratio, num_chromo, num_generation, units, mutation_rate, 1, nn_threshold, index);
            return ga_island.best_island;
        }

        public static void Main(string[] args)
        {
            Console.WriteLine("# of CPU cores=" + System.Environment.ProcessorCount.ToString());

            var key = "";
            while (true)
            {
                Console.WriteLine("\"ga\" : island GA");
                Console.WriteLine("\"sim\" : read sim");
                Console.WriteLine("\"multi\" : multi strategy sim");

                key = Console.ReadLine();
                if (key == "ga" || key == "sim" || key == "multi")
                    break;
            }

            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            Console.WriteLine("started program.");
            List<int> terms = new List<int>();
            for (int i = 10; i < 1000; i = i + 100) { terms.Add(i); }
            MarketData.initializer(terms);

            var from = 1000;
            var to = 500000;
            int max_amount = 1;
            var index = new int[] { 1, 1, 1, 0 };
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
                var units = new int[] { 37, 10, 10, 5 };
                var mutation_rate = 0.5;
                var move_ratio = 0.2;
                best_island_id = doGA(from, to, max_amount, num_island, num_chromos, num_generations, banned_move_period, units, mutation_rate, move_ratio, index, display_chart, nn_threshold);
                doSim(from, to, max_amount, sim_type, best_island_id, index, display_chart, nn_threshold);
            }
            //multi strategy combination sim
            else if (key == "multi")
            {
                var index_list = new List<int[]> { new int[] { 1, 0, 0, 0 }, new int[] { 0, 1, 0, 0 }, new int[] { 0, 0, 1, 0 }, new int[] { 1, 1, 1, 0 }, new int[] { 1, 0, 1, 0 } };
                var units_list = new List<int[]> { new int[] { 17, 5, 5, 5 }, new int[] { 17, 5, 5, 5 }, new int[] { 17, 5, 5, 5 }, new int[] { 37, 10, 10, 5 }, new int[] { 27, 7, 7, 5 } };
                var best_pl_list = new List<List<double>>();
                var best_ac_list = new List<SimAccount>();
                int num_island = 2;
                int num_chromos = 4;
                int num_generations = 6;
                int banned_move_period = 2;
                var mutation_rate = 0.5;
                var move_ratio = 0.2;
                var conbined_pl = new List<double>();
                var sim_from = to;
                var sim_to = to + 200000;
                for (int i = 0; i < 2; i++)
                {
                    best_island_id = doGA(from, to, max_amount, num_island, num_chromos, num_generations, banned_move_period, units_list[i], mutation_rate, move_ratio, index_list[i], display_chart, nn_threshold);
                    var ac = doSim(sim_from, sim_to, max_amount, sim_type, best_island_id, index_list[i], display_chart, nn_threshold);
                    best_pl_list.Add(ac.total_pl_ratio_list);
                    best_ac_list.Add(ac);
                    File.Copy(@"./best_weight_ID-" + best_island_id.ToString() + ".csv", @"./log_best_weight_ID-" + i.ToString() + ".csv");
                }
                using (StreamWriter sw = new StreamWriter(@"./multi_strategy_results.csv", false, Encoding.UTF8))
                {
                    for (int i = 0; i < best_pl_list[0].Count; i++)
                    {
                        var line = "";
                        var total_pl_tmp = 0.0;
                        for (int j = 0; j < best_pl_list.Count; j++)
                        {
                            line += best_pl_list[j][i].ToString() + ",";
                            total_pl_tmp += best_pl_list[j][i];
                        }
                        var ave_total_pl = total_pl_tmp / Convert.ToDouble(best_pl_list.Count);
                        conbined_pl.Add(ave_total_pl);
                        sw.WriteLine(line+","+ave_total_pl.ToString());
                    }
                    LineChart.DisplayLineChart(conbined_pl, "Combined PL Ratio - " + sim_from.ToString() + " - " + sim_to.ToString() + ", dt:" + MarketData.Dt[sim_from].ToString() + " - " + MarketData.Dt[sim_to - 1]);
                }
            }

                /*
                if (key == "sim")
                {
                    Console.WriteLine("Started Read Weight SIM");
                    var ga = new GA(0);
                    var chromo = ga.readWeights(1);
                    //var from = 1000 + Convert.ToInt32(Math.Round(MarketData.Close.Count * 0.8));
                    var from = 501000;
                    var to = MarketData.Close.Count - 1;
                    int max_amount = 1;
                    double nn_threshold = 0.1;
                    var index = new int[] { 1, 1, 1, 0 };
                    var ac = ga.sim_ga_market_limit(from, to, max_amount, chromo, from.ToString() + " - " + to.ToString() + ", dt:" + MarketData.Dt[from].ToString() + " - " + MarketData.Dt[to - 1], true, nn_threshold, index);
                }


                //Island GA
                if (key == "ga")
                {
                    Console.WriteLine("Started Island GA SIM");
                    RandomSeed.initialize();
                    int from = 1000;
                    int num_island = 2;
                    int num_chromos = 4;
                    int num_generations = 20;
                    int banned_move_period = 2;
                    int max_amount = 1;
                    var units = new int[] { 37, 10, 10, 5 };
                    var index = new int[] { 1,1,1,0};
                    var mutation_rate = 0.5;
                    var move_ratio = 0.2;
                    var sim_type = 1; //0:limit, 1:market/limit
                    double nn_threshold = 0.5;
                    //int to = Convert.ToInt32(Math.Round(MarketData.Close.Count * 0.8)) + from;
                    int to = 600000 + from;
                    var ga_island = new GAIsland();
                    ga_island.start_ga_island(from, to, max_amount, num_island, banned_move_period, move_ratio, num_chromos, num_generations, units, mutation_rate, sim_type, nn_threshold, index);

                    var ga = new GA(0);
                    var chromo = ga.readWeights(ga_island.best_island);
                    var ac = new SimAccount();
                    if (sim_type == 0)
                        ac = ga.sim_ga_limit(from, to, max_amount, chromo, from.ToString() + " - " + to.ToString() + ", dt:" + MarketData.Dt[from].ToString() + " - " + MarketData.Dt[to - 1] + ", Best Island=" + ga_island.best_island.ToString(), true, index);
                    else
                        ac = ga.sim_ga_market_limit(from, to, max_amount, chromo, from.ToString() + " - " + to.ToString() + ", dt:" + MarketData.Dt[from].ToString() + " - " + MarketData.Dt[to - 1] + ", Best Island=" + ga_island.best_island.ToString(), true, nn_threshold, index);
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
                    var units = new int[] { 20, 20, 4 };
                    var index = new int[] { 1, 1, 1, 0 };
                    var mutation_rate = 0.9;
                    var move_ratio = 0.2;
                    var sim_type = 1;
                    double nn_threshold = 0.7;
                    var ac_list = new List<SimAccount>();
                    for (int i = 0; i < 3; i++)
                    {
                        int ga_from = i * sim_window + start_ind;
                        int ga_to = ga_from + ga_window;
                        int sim_from = ga_to;
                        int sim_to = sim_from + sim_window;
                        Console.WriteLine("Conti GA SIM i=" + i.ToString() + ", ga period=" + ga_from.ToString() + " - " + ga_to.ToString() + ", sim period=" + sim_from.ToString() + " - " + sim_to.ToString());
                        var ga_island = new GAIsland();
                        ga_island.start_ga_island(ga_from, ga_to, max_amount, num_island, banned_move_period, move_ratio, num_chromos, num_generations, units, mutation_rate, sim_type, nn_threshold, index);

                        var ga = new GA(0);
                        var chromo = ga.readWeights(ga_island.best_island);
                        var ac = new SimAccount();
                        ac = ga.sim_ga_limit_conti(sim_from, sim_to, max_amount, chromo, sim_from.ToString() + " - " + sim_to.ToString(), ac, false, index);
                        ac_list.Add(ac);
                    }
                    (List<double> combined_total_pl, int comined_num_trade, double combined_win_rate, double combined_sharp_ratio) res = CombinedAC.calcCombinedAC(ac_list);
                    Console.WriteLine("*************************************************************************");
                    Console.WriteLine("term total pl=" + ac_list.Last().performance_data.total_pl.ToString() + ", term num trade=" + ac_list.Last().performance_data.num_trade.ToString() + ", term win rate=" + ac_list.Last().performance_data.win_rate.ToString() + ", term sharp ratio=" + ac_list.Last().performance_data.sharp_ratio.ToString());
                    Console.WriteLine("combined total pl=" + res.combined_total_pl.Last().ToString() + ", combined num trade=" + res.comined_num_trade.ToString() + ", combined win rate=" + res.combined_win_rate.ToString() + ", combined sharp ratio=" + res.combined_sharp_ratio.ToString());
                    Console.WriteLine("*************************************************************************");
                    System.Threading.Thread.Sleep(3000);
                    LineChart.DisplayLineChart(res.combined_total_pl, "Conti sim: " + ac_list[0].start_ind.ToString() + " - " + ac_list.Last().end_ind.ToString() + ", combined num trade=" + res.comined_num_trade.ToString() + ", combined win rate=" + res.combined_win_rate.ToString());
                }
                */
                stopWatch.Stop();
            Console.WriteLine("Completed all processes.");
            Console.WriteLine("Time Elapsed (sec)=" + stopWatch.Elapsed.TotalSeconds.ToString());
        }
    }
}
