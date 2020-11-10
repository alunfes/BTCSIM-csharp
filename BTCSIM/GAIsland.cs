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

        public GAIsland()
        {
            gas = new List<GA>();
            best_island = -1;
            best_island_log = new List<int>();
            best_eva = -1;
            best_eva_log = new List<double>();
        }

        /*それぞれのislandでchromosを初期化する
         *0番目のislandからGA計算を開始して、全ての染色体の評価と次世代生成までを行う
         *全てのislandの計算が終わったら同じように次世代の計算を0番目のislandから行う。
         *island間の移動禁止期間が終わったら、各世代の最後にランダムに選択したisland間においてランダムに選択した染色体の交換を行う
         *
         *->各GA instanceにおいて、1世代ごとの計算で止めて染色体を保存した上で、次の世代の計算をするという仕組みが必要。
         */
        public void start_ga_island(int from, int to, int num_island, int move_ban_period, double move_ratio, int num_chromos, int num_generations, int[] units, double mutation_rate)
        {
            var sw = new Stopwatch();
            //initialize GS in each island
            for (int i = 0; i < num_island; i++)
                gas.Add(new GA(i));
            //do GA calc for move_ban_period
            for(int i=0; i<move_ban_period; i++)
            {
                sw.Start();
                for(int j=0; j<num_island; j++)
                {
                    gas[j].start_island_ga(from, to, num_chromos, i, units, mutation_rate);
                }
                checkBestIsland();
                sw.Stop();
                display_info(i, sw);
                sw.Reset();
            }
            Console.WriteLine("Move banned period has been finished.");
            //do GA calc for remaining generations
            for (int i = move_ban_period; i < num_generations; i++)
            {
                sw.Start();
                moveBetweenIsland(move_ratio);
                for (int j = 0; j < num_island; j++)
                {
                    gas[j].start_island_ga(from, to, num_chromos, i, units, mutation_rate);
                    //gas[j].resetChromos();
                }
                checkBestIsland();
                sw.Stop();
                display_info(i, sw);
                sw.Reset();
            }
            Console.WriteLine("Completed GA");
        }


        /*各islandにおいて、ランダムに選択したislandからランダムに選択した染色体を交換する
         ->best chromo以外を選択するようにする*/
        private void moveBetweenIsland(double move_ratio)
        {
            for(int i=0; i<gas.Count; i++)
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
                    var tmp_chrom = new Gene(gas[selected_island].chromos[selected_target_chromo].num_units);
                    for (int k = 0; k < gas[selected_island].chromos[selected_target_chromo].bias_gene1.Length; k++)
                        tmp_chrom.bias_gene1[k] = gas[selected_island].chromos[selected_target_chromo].bias_gene1[k];
                    for (int k = 0; k < gas[selected_island].chromos[selected_target_chromo].bias_gene2.Length; k++)
                        tmp_chrom.bias_gene2[k] = gas[selected_island].chromos[selected_target_chromo].bias_gene2[k];
                    for (int k = 0; k < gas[selected_island].chromos[selected_target_chromo].weight_gene1.Length; k++)
                        tmp_chrom.weight_gene1[k] = gas[selected_island].chromos[selected_target_chromo].weight_gene1[k];
                    for (int k = 0; k < gas[selected_island].chromos[selected_target_chromo].weight_gene2.Length; k++)
                        tmp_chrom.weight_gene2[k] = gas[selected_island].chromos[selected_target_chromo].weight_gene2[k];
                    //copy from selected chromo to target chromo
                    for (int k = 0; k < gas[selected_island].chromos[selected_target_chromo].bias_gene1.Length; k++)
                        gas[selected_island].chromos[selected_target_chromo].bias_gene1[k] = gas[i].chromos[selected_id].bias_gene1[k];
                    for (int k = 0; k < gas[selected_island].chromos[selected_target_chromo].bias_gene2.Length; k++)
                        gas[selected_island].chromos[selected_target_chromo].bias_gene2[k] = gas[i].chromos[selected_id].bias_gene2[k];
                    for (int k = 0; k < gas[selected_island].chromos[selected_target_chromo].weight_gene1.Length; k++)
                        gas[selected_island].chromos[selected_target_chromo].weight_gene1[k] = gas[i].chromos[selected_id].weight_gene1[k];
                    for (int k = 0; k < gas[selected_island].chromos[selected_target_chromo].weight_gene2.Length; k++)
                        gas[selected_island].chromos[selected_target_chromo].weight_gene2[k] = gas[i].chromos[selected_id].weight_gene2[k];
                    //copy from target chromo to selected chromo
                    for (int k = 0; k < gas[selected_island].chromos[selected_target_chromo].bias_gene1.Length; k++)
                        gas[i].chromos[selected_id].bias_gene1[k] = tmp_chrom.bias_gene1[k];
                    for (int k = 0; k < gas[selected_island].chromos[selected_target_chromo].bias_gene2.Length; k++)
                        gas[i].chromos[selected_id].bias_gene2[k] = tmp_chrom.bias_gene2[k];
                    for (int k = 0; k < gas[selected_island].chromos[selected_target_chromo].weight_gene1.Length; k++)
                        gas[i].chromos[selected_id].weight_gene1[k] = tmp_chrom.weight_gene1[k];
                    for (int k = 0; k < gas[selected_island].chromos[selected_target_chromo].weight_gene2.Length; k++)
                        gas[i].chromos[selected_id].weight_gene2[k] = tmp_chrom.weight_gene2[k];
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



        private void display_info(int generation_ind, Stopwatch sw)
        {
            Console.WriteLine("---------------------------------------------------");
            Console.WriteLine("Generation No." + generation_ind.ToString() + ", Best Island No." + best_island.ToString() + ", Best eva=" + best_eva.ToString()
                + ", Best chromo No." + gas[best_island].best_chromo.ToString() + ", Best pl=" + gas[best_island].best_ac.performance_data.total_pl.ToString()
                + ", Best num trade=" + gas[best_island].best_ac.performance_data.num_trade.ToString()
                + ", Best win rate=" + gas[best_island].best_ac.performance_data.win_rate.ToString()
                + ", Best sharp ratio = " + gas[best_island].best_ac.performance_data.sharp_ratio.ToString());
            Console.WriteLine("Time Elapsed (sec)="+sw.Elapsed.Seconds.ToString());
            Console.WriteLine("---------------------------------------------------");
        }


        
    }
}
