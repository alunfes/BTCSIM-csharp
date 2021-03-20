using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;

namespace BTCSIM
{
    public class GAIsland
    {
        public List<GA> gas { get; set; }
        public int best_island { get; set; }
        public List<int> best_island_log { get; set; }
        public double best_eva { get; set; }
        public List<double> best_eva_log { get; set; }
        public int last_move_banned_generation { get; set; }
        public int move_ban_period { get; set; }
        public bool move_ban_flg { get; set; }

        public GAIsland()
        {
            gas = new List<GA>();
            best_island = -1;
            best_island_log = new List<int>();
            best_eva = -1;
            best_eva_log = new List<double>();
            last_move_banned_generation = 0;
            move_ban_period = 100;
            move_ban_flg = true;
        }


        public void start_win_ga_island(int from, int to, int num_random_windows, int num_island, int move_ban_period, double move_ratio, int num_chromos, int num_generations, int[] units, double mutation_rate, double nn_threshold, int[] index)
        {
            //generate sim_windows
            var mid = Convert.ToInt32(Math.Round((to - from) / 2.0));
            var quarter = Convert.ToInt32(Math.Round((to - from) / 4.0));
            var sim_window = new List<int[]>() { new int[] { from, to }, new int[] { from, from + mid }, new int[] { from + mid, to }, new int[] { from, from + quarter }, new int[] { from + quarter, from + quarter * 2 }, new int[] { from + quarter * 2, from + quarter * 3 }, new int[] { from + quarter * 3, to } };
            for (int i = 0; i < num_random_windows; i++)
            {
                var start = RandomSeed.rnd.Next(from, to - 500);
                sim_window.Add(new int[] { start, RandomSeed.rnd.Next(start, to + 1) });
            }

            var sww = new Stopwatch();
            this.move_ban_period = move_ban_period;
            //initialize GS in each island
            for (int i = 0; i < num_island; i++)
                gas.Add(new GA(i));
            //do GA calc for move_ban_period
            for (int i = 0; i < num_generations; i++)
            {
                sww.Start();
                for (int j = 0; j < num_island; j++)
                {
                    gas[j].start_island_win_ga(from, to, sim_window, num_chromos, i, units, mutation_rate, nn_threshold, index);
                }
                checkBestIsland();
                sww.Stop();
                display_info(i, sww);
                resetIslandsMoveBanControl(i);
                if (move_ban_flg == false)
                    moveBetweenIsland(move_ratio);
                sww.Reset();
            }
            Console.WriteLine("Completed Win GA");
        }


        /*それぞれのislandでchromosを初期化する
         *0番目のislandからGA計算を開始して、全ての染色体の評価と次世代生成までを行う
         *全てのislandの計算が終わったら同じように次世代の計算を0番目のislandから行う。
         *island間の移動禁止期間が終わったら、各世代の最後にランダムに選択したisland間においてランダムに選択した染色体の交換を行う
         *
         *->各GA instanceにおいて、1世代ごとの計算で止めて染色体を保存した上で、次の世代の計算をするという仕組みが必要。
         */
        public void start_ga_island(int from, int to, int max_amount, int num_island, int move_ban_period, double move_ratio, int num_chromos, int num_generations, int[] units, double mutation_rate, int sim_type, double nn_threshold, int[] index)
        {
            var sww = new Stopwatch();
            this.move_ban_period = move_ban_period;
            //initialize GS in each island
            for (int i = 0; i < num_island; i++)
                gas.Add(new GA(i));
            //do GA calc for move_ban_period
            for (int i = 0; i < num_generations; i++)
            {
                sww.Start();
                for (int j = 0; j < num_island; j++)
                {
                    gas[j].start_island_ga(from, to, max_amount, num_chromos, i, units, mutation_rate, sim_type, nn_threshold, index);
                }
                checkBestIsland();
                sww.Stop();
                display_info(i, sww);
                resetIslandsMoveBanControl(i);
                if (move_ban_flg==false)
                    moveBetweenIsland(move_ratio);
                sww.Reset();
            }
            Console.WriteLine("Completed GA");
        }


        /*各islandにおいて、ランダムに選択したislandからランダムに選択した染色体を交換する
         ->best chromo以外を選択するようにする*/
        private void moveBetweenIsland(double move_ratio)
        {
            if (gas.Count > 1)
            {
                for (int i = 0; i < gas.Count; i++)
                {
                    var num_move = Convert.ToInt32(gas[i].chromos.Length * move_ratio);
                    for (int j = 0; j < num_move; j++)
                    {
                        var island_list = Enumerable.Range(0, gas.Count).ToList();
                        island_list.RemoveAt(island_list.IndexOf(i));
                        var selected_island = island_list[RandomSeed.rnd.Next(0, island_list.Count)];
                        var target_chrom_list = Enumerable.Range(0, gas[selected_island].chromos.Length).ToList();
                        target_chrom_list.RemoveAt(target_chrom_list.IndexOf(gas[selected_island].best_chromo));
                        var selected_target_chromo = target_chrom_list[RandomSeed.rnd.Next(0, target_chrom_list.Count)];
                        var selected_id = RandomSeed.rnd.Next(0, gas[i].chromos.Length);
                        while (selected_id == gas[i].best_chromo)
                            selected_id = RandomSeed.rnd.Next(0, gas[i].chromos.Length);

                        //exchange chromo
                        //copy targe chromo to tmp chromo
                        var tmp_chrom = new Gene2(gas[selected_island].chromos[selected_target_chromo].num_units, gas[selected_island].chromos[selected_target_chromo].num_index);
                        for (int k = 0; k < gas[selected_island].chromos[selected_target_chromo].bias_gene.Count; k++) //for layers
                        {
                            for (int l = 0; l < gas[selected_island].chromos[selected_target_chromo].bias_gene[k].Length; l++) //for weights
                                tmp_chrom.bias_gene[k][l] = gas[selected_island].chromos[selected_target_chromo].bias_gene[k][l];
                        }
                        for (int k = 0; k < gas[selected_island].chromos[selected_target_chromo].weight_gene.Count; k++) //for layers
                        {
                            for (int l = 0; l < gas[selected_island].chromos[selected_target_chromo].weight_gene[k].Count; l++) //for units
                            {
                                for (int m = 0; m < gas[selected_island].chromos[selected_target_chromo].weight_gene[k][l].Length; m++) //for weights
                                    tmp_chrom.weight_gene[k][l][m] = gas[selected_island].chromos[selected_target_chromo].weight_gene[k][l][m];
                            }
                        }

                        //copy from selected chromo to target chromo
                        for (int k = 0; k < gas[selected_island].chromos[selected_target_chromo].bias_gene.Count; k++) //for layers
                        {
                            for (int l = 0; l < gas[selected_island].chromos[selected_target_chromo].bias_gene[k].Length; l++) //for weights
                                gas[selected_island].chromos[selected_target_chromo].bias_gene[k] = gas[i].chromos[selected_id].bias_gene[k];
                        }
                        for (int k = 0; k < gas[selected_island].chromos[selected_target_chromo].weight_gene.Count; k++) //for layers
                        {
                            for (int l = 0; l < gas[selected_island].chromos[selected_target_chromo].weight_gene[k].Count; l++) //for units
                            {
                                for (int m = 0; m < gas[selected_island].chromos[selected_target_chromo].weight_gene[k][l].Length; m++) //for weights
                                    gas[selected_island].chromos[selected_target_chromo].weight_gene[k][l][m] = gas[i].chromos[selected_id].weight_gene[k][l][m];
                            }
                        }

                        //copy from target chromo to selected chromo
                        for (int k = 0; k < gas[selected_island].chromos[selected_target_chromo].bias_gene.Count; k++) //for layers
                        {
                            for (int l = 0; l < gas[selected_island].chromos[selected_target_chromo].bias_gene[k].Length; l++) //for weights
                                gas[i].chromos[selected_id].bias_gene[k][l] = tmp_chrom.bias_gene[k][l];
                        }
                        for (int k = 0; k < gas[selected_island].chromos[selected_target_chromo].weight_gene.Count; k++) //for layers
                        {
                            for (int l = 0; l < gas[selected_island].chromos[selected_target_chromo].weight_gene[k].Count; l++) //for units
                            {
                                for (int m = 0; m < gas[selected_island].chromos[selected_target_chromo].weight_gene[k][l].Length; m++) //for weights
                                    gas[i].chromos[selected_id].weight_gene[k][l][m] = tmp_chrom.weight_gene[k][l][m];
                            }
                        }
                    }
                }
            }
        }


        private void checkBestIsland()
        {
            for (int i = 0; i < gas.Count; i++)
            {
                if (best_eva < gas[i].best_eva)
                {
                    best_eva = gas[i].best_eva;
                    best_island = i;
                }
            }
            best_eva_log.Add(best_eva);
            best_island_log.Add(best_island);
        }


        //move ban period後、一定世代が経過したらbest island以外の染色体を全てresetして再度move banにする。
        private void resetIslandsMoveBanControl(int current_generation)
        {
            if (current_generation - last_move_banned_generation >= move_ban_period && move_ban_flg)
            {
                move_ban_flg = false;
                Console.WriteLine("Allowed Move.");
            }
            else if (current_generation - last_move_banned_generation > move_ban_period + move_ban_period && move_ban_flg == false)
            {
                Console.WriteLine("Banned Move.");
                move_ban_flg = true;
                last_move_banned_generation = current_generation;
                for (int i = 0; i < gas.Count; i++)
                    if (i != best_island) { gas[i].resetChromos(); }
            }
        }



        private void display_info(int generation_ind, Stopwatch sw)
        {
            Console.WriteLine("---------------------------------------------------");
            Console.WriteLine("Generation No." + generation_ind.ToString() + ", Best Island No." + best_island.ToString() + ", Best eva=" + best_eva.ToString()
                + ", Best chromo No." + gas[best_island].best_chromo.ToString() + ", Best pl=" + gas[best_island].best_ac.performance_data.total_pl.ToString()
                + ", Best num trade=" + gas[best_island].best_ac.performance_data.num_trade.ToString()
                + ", Best win rate=" + gas[best_island].best_ac.performance_data.win_rate.ToString()
                + ", Best ave pl=" + (gas[best_island].best_ac.performance_data.total_pl / gas[best_island].best_ac.performance_data.num_trade).ToString()
                + ", Best sharp ratio = " + gas[best_island].best_ac.performance_data.sharp_ratio.ToString()
                + ", Best buy pl = " + gas[best_island].best_ac.performance_data.buy_pl_list.Sum().ToString()
                + ", Best sell pl = " + gas[best_island].best_ac.performance_data.sell_pl_list.Sum().ToString()
                + ", Best num market order = " + gas[best_island].best_ac.performance_data.num_maker_order.ToString()) ;
            Console.WriteLine("Time Elapsed (sec)=" + sw.Elapsed.TotalSeconds.ToString());
            Console.WriteLine("---------------------------------------------------");
        }



    }
}
