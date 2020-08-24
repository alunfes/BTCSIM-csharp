using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Text;

namespace BTCSIM
{
    public class Gene
    {
        public List<int> position_gene { get; set; } //1:buy, 2:sell
        
        public Gene(int num_gene)
        {
            Random rnd = new Random();
            position_gene = new List<int>();
            for (int i = 0; i < num_gene; i++)
                position_gene.Add(rnd.Next(1, 3));
        }
    }


    public class GA
    {
        public Gene[] chromos { get; set; }
        public List<double> best_eva_log { get; set; }
        public double best_eva { get; set; }
        public int best_chromo { get; set; }
        public List<int> best_chromo_log { get; set; }

        public ConcurrentDictionary<int, long> eva_time { get; set; }

        public List<int> generation_time_log { get; set; }
        public double estimated_time_to_completion { get; set; }


        public GA()
        {
            generation_time_log = new List<int>();
            eva_time = new ConcurrentDictionary<int, long>();
            estimated_time_to_completion = -1;
            best_chromo_log = new List<int>();
            best_eva_log = new List<double>();
        }

        public void start_ga(int num_chromos, int num_generations, double mutation_rate, int num_gene)
        {
            //initialize chromos
            Console.WriteLine("started GA");
            generate_chromos(num_chromos, num_gene);
            Console.WriteLine("initialized chromos");
            for (int i = 0; i < num_generations; i++)
            {
                Stopwatch generationWatch = new Stopwatch();
                generationWatch.Start();
                //evaluation chromos
                var eva_dic = new ConcurrentDictionary<int, double>();
                eva_time = new ConcurrentDictionary<int, long>();
                Parallel.For(0, chromos.Length, j =>
                {
                    var eva = evaluation(j, chromos[j]);
                    eva_dic.GetOrAdd(j, eva);
                });
                //check best eva
                check_best_eva(eva_dic);
                //roulette selection
                var selected_chro_ind_list = roulette_selection(eva_dic);
                //cross over
                crossover(selected_chro_ind_list);
                //mutation
                mutation(0.1);
                generationWatch.Stop();
                generation_time_log.Add(generationWatch.Elapsed.Seconds);
                calc_time_to_complete_from_generation_time(i, num_generations);
                display_generation(i, generationWatch.Elapsed.Seconds);
            }
            write_best_chromo();
            Console.WriteLine("Completed GA.");
        }

        private void generate_chromos(int num_chrom, int num_gene)
        {
            chromos = new Gene[num_chrom];
            for(int i=0; i<chromos.Length; i++)
                chromos[i] = new Gene(num_gene);
        }

        private double evaluation(int chro_id, Gene chro)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            var ac = new SimAccount();
            var sim = new Sim();
            ac = sim.sim_ga(0, MarketData.Close.Count-1, 1, chro, ac);
            stopWatch.Stop();
            eva_time.GetOrAdd(chro_id, stopWatch.ElapsedMilliseconds);
            return ac.performance_data.total_pl;
        }

        private void check_best_eva(ConcurrentDictionary<int, double> eva)
        {
            var max = -99999999.0;
            var eva_key = eva.Keys.ToArray();
            int best_eva_key = -1;
            foreach(var k in eva_key)
            {
                if (eva[k] > max)
                {
                    max = eva[k];
                    best_eva_key = k;
                }
            }
            best_chromo = best_eva_key;
            best_chromo_log.Add(best_eva_key);
            best_eva = max;
            best_eva_log.Add(max);
        }

        /*eva.valueにminを加算して、合計値を10000に置き換えてそれぞれの値を計算。
         * roulette boardを作成。
         */
        private List<int> roulette_selection(ConcurrentDictionary<int, double> eva)
        {
            var selected_chro_ind = new List<int>();
            List<int> roulette_board = new List<int>();
            List<double> vals = new List<double>();
            var min = eva.Values.Min();
            for (int i=0; i<eva.Count; i++)
                vals.Add(eva[i] + min);//evaのkeyが0-count-1までの連続値になっていることが前提
            
            List<double> con_vals = new List<double>();
            var sumv = vals.Sum();
            var tmp_val = 0;
            foreach (var v in vals)
            {
                tmp_val += Convert.ToInt32(Math.Round(10000 * v / sumv));
                roulette_board.Add(tmp_val);
            }

            Random rnd = new Random();
            for(int i=0; i<chromos.Count(); i++)
            {
                var selected = rnd.Next(0, roulette_board.Last() + 1);
                if (selected <= roulette_board[0])
                    selected_chro_ind.Add(0);
                else
                {
                    for (int j=1; j<roulette_board.Count; j++)
                    {
                        if (selected > roulette_board[j - 1] && selected <= roulette_board[j])
                            selected_chro_ind.Add(j);
                    }
                }
            }
            if (selected_chro_ind.Count != chromos.Count())
                Console.WriteLine("selected ind is not matched with num chromo in roulette selection!");
            return selected_chro_ind;
        }


        private void crossover(List<int> selected)
        {
            Random rnd = new Random();
            var new_chromos = new Gene[chromos.Count()];
            chromos.CopyTo(new_chromos, 0);
            for (int i=0; i<chromos.Count(); i++)
            {
                if (i != best_chromo)
                {
                    //copy 3% gene start from random selected index
                    var selected_ind = rnd.Next(0, Convert.ToInt32(chromos[selected[i]].position_gene.Count() * 0.97) - 1);
                    for (int j = 0; j < Convert.ToInt32(chromos[selected[i]].position_gene.Count() * 0.03); j++)
                        new_chromos[i].position_gene[j + selected_ind] = chromos[selected[i]].position_gene[j + selected_ind];
                }
            }
            chromos = new Gene[chromos.Count()];
            new_chromos.CopyTo(chromos, 0);
        }

        private void mutation(double mutation_ratio)
        {
            Random rnd = new Random();
            for (int i=0; i<chromos.Count(); i++)
            {
                if (i != best_chromo)
                {
                    for (int j = 0; j < chromos[i].position_gene.Count(); j++)
                    {
                        if (rnd.NextDouble() < mutation_ratio)
                            chromos[i].position_gene[j] = chromos[i].position_gene[j] == 1 ? 2 : 1;
                    }
                }
            }
        }

        private void display_generation(int generation, int time_elapsed)
        {
            Console.WriteLine("Generation No." + generation.ToString() + " : " + " Best Chromo ID=" + best_chromo.ToString() + ", Estimated completion hour="+estimated_time_to_completion.ToString() + ", Best eva=" + best_eva.ToString());
        }

        private void calc_time_to_complete_from_generation_time(int generation, int num_generations)
        {
            if (generation_time_log.Count() > 0)
            {
                estimated_time_to_completion = Math.Round((generation_time_log.Average() * (num_generations - generation)) / 3600, 2);
            }
        }

        private void write_best_chromo()
        {
            Console.WriteLine("Writing Best Chromo...");
            StreamWriter file = new StreamWriter(@"C:\test\test.csv", false, Encoding.UTF8);
            file.WriteLine("dt,open,high,low,close,size,unix_time,opt_position");  // ヘッダ部出力
            for (int i = 0; i < )
            {
                file.WriteLine(string.Format(""{ 0},{ 1}
                "", words[i], words[i + 1])); // データ部出力
            }
            file.Close();
            Console.WriteLine("Completed write best chromo.");
        }


    }
}
