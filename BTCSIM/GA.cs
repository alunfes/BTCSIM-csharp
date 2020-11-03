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
        //3 layer NN
        public double[] weight_gene1 { get; set; } //double[num input data * second layer units]
        public double[] bias_gene1 { get; set; }
        public double[] weight_gene2 { get; set; } //double[second layer units * third layer units]
        public double[] bias_gene2 { get; set; }
        public int[] num_units { get; set; }
        
        public Gene(int[] units)
        {
            this.num_units = units;
            weight_gene1 = new double[units[0] * units[1]];
            bias_gene1 = new double[units[1]];
            weight_gene2 = new double[units[1] * units[2]];
            bias_gene2 = new double[units[2]];
            weight_gene1 = RandomGenerator.getRandomArray(units[0] * units[1]);
            bias_gene1 = RandomGenerator.getRandomArray(units[1]);
            weight_gene2 = RandomGenerator.getRandomArray(units[1] * units[2]);
            bias_gene2 = RandomGenerator.getRandomArray(units[2]);
        }
    }


    public class GA
    {
        public Gene[] chromos { get; set; }
        public List<double> best_eva_log { get; set; }
        public double best_eva { get; set; }
        public int best_chromo { get; set; }
        public List<int> best_chromo_log { get; set; }
        public SimAccount best_ac { get; set; }
        public List<SimAccount> best_ac_log { get; set; }

        public ConcurrentDictionary<int, long> eva_time { get; set; }

        public List<int> generation_time_log { get; set; }
        public double estimated_time_to_completion { get; set; }

        public List<int> best_chromo_gene { get; set; }


        public GA()
        {
            generation_time_log = new List<int>();
            estimated_time_to_completion = -1;
            best_chromo_log = new List<int>();
            best_eva_log = new List<double>();
            best_ac_log = new List<SimAccount>();
        }

        public void start_ga(int from, int to, int num_chromos, int num_generations, int[] units, double mutation_rate)
        {
            //initialize chromos
            
            Console.WriteLine("started GA");
            generate_chromos(num_chromos, units);
            for (int i = 0; i < num_generations; i++)
            {
                Stopwatch generationWatch = new Stopwatch();
                generationWatch.Start();
                //evaluation chromos
                var eva_dic = new ConcurrentDictionary<int, double>();
                var ac_dic = new ConcurrentDictionary<int, SimAccount>();
                Parallel.For(0, chromos.Length, j =>
                {
                    (double total_pl, SimAccount ac) res = evaluation(from, to, j, chromos[j]);
                    eva_dic.GetOrAdd(j, res.total_pl);
                    ac_dic.GetOrAdd(j, res.ac);
                });
                //check best eva
                check_best_eva(eva_dic, ac_dic);
                //roulette selection
                var selected_chro_ind_list = roulette_selection(eva_dic);
                //mutation
                mutation(mutation_rate);
                //cross over
                crossover(selected_chro_ind_list, 0.3);                
                generationWatch.Stop();
                generation_time_log.Add(generationWatch.Elapsed.Seconds);
                calc_time_to_complete_from_generation_time(i, num_generations);
                display_generation(i);
            }
            //write_best_chromo();
            
            Console.WriteLine("Completed GA.");
        }

        private void generate_chromos(int num_chrom, int[] num_units_layer)
        {
            chromos = new Gene[num_chrom];
            for(int i=0; i<num_chrom; i++)
                chromos[i] = new Gene(num_units_layer);
        }

        private (double, SimAccount) evaluation(int from, int to, int chro_id, Gene chro)
        {
            var ac = new SimAccount();
            var sim = new Sim();
            ac = sim.sim_ga(from, to, chro, ac);
            return (ac.performance_data.total_pl, ac);
        }

        private void check_best_eva(ConcurrentDictionary<int, double> eva, ConcurrentDictionary<int, SimAccount> ac)
        {
            var max_eva = -99999999.0;
            var eva_key = eva.Keys.ToArray();
            int best_eva_key = -1;
            foreach(var k in eva_key)
            {
                if (eva[k] > max_eva)
                {
                    max_eva = eva[k];
                    best_eva_key = k;
                }
            }
            best_ac_log.Add(ac[best_eva_key]);
            best_ac = ac[best_eva_key];
            best_chromo = best_eva_key;
            best_chromo_log.Add(best_eva_key);
            best_eva = max_eva;
            best_eva_log.Add(max_eva);
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
                vals.Add(eva[i] - min);//evaのkeyが0-count-1までの連続値になっていることが前提
            
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

        private void mutation(double mutation_ratio)
        {
            Random rnd = new Random();
            for (int i=0; i<chromos.Count(); i++)
            {
                if (i != best_chromo)
                {
                    for (int j = 0; j < chromos[i].bias_gene1.Length; j++)
                        chromos[i].bias_gene1[j] = rnd.NextDouble() > (1-mutation_ratio) ?  RandomGenerator.getRandomArrayRange(-10,10) : chromos[i].bias_gene1[j];
                    for (int j = 0; j < chromos[i].weight_gene1.Length; j++)
                        chromos[i].weight_gene1[j] = rnd.NextDouble() > (1 - mutation_ratio) ? RandomGenerator.getRandomArrayRange(-10, 10) : chromos[i].weight_gene1[j];
                    for (int j = 0; j < chromos[i].bias_gene2.Length; j++)
                        chromos[i].bias_gene2[j] = rnd.NextDouble() > (1 - mutation_ratio) ? RandomGenerator.getRandomArrayRange(-10, 10) : chromos[i].bias_gene2[j];
                    for (int j = 0; j < chromos[i].weight_gene2.Length; j++)
                        chromos[i].weight_gene2[j] = rnd.NextDouble() > (1 - mutation_ratio) ? RandomGenerator.getRandomArrayRange(-10, 10) : chromos[i].weight_gene2[j];
                }
            }
        }

        private void crossover(List<int> selected, double cross_over_ratio)
        {
            var rnd = new Random();
            var new_chromos = new Gene[chromos.Count()];
            chromos.CopyTo(new_chromos, 0);
            for (int i=0; i<chromos.Count(); i++)
            {
                if (i != best_chromo)
                {
                    //bias1/2, weight1/2からそれぞれからランダムにratio %のweightを選択して交配
                    var ratio = cross_over_ratio;
                    for (int j = 0; j < chromos[i].weight_gene1.Length; j++)
                        new_chromos[i].weight_gene1[j] = rnd.NextDouble() > (1-ratio) ? chromos[selected[i]].weight_gene1[j] : chromos[i].weight_gene1[j];
                    for (int j = 0; j < chromos[i].bias_gene1.Length; j++)
                        new_chromos[i].bias_gene1[j] = rnd.NextDouble() > (1 - ratio) ? chromos[selected[i]].bias_gene1[j] : chromos[i].bias_gene1[j];
                    for (int j = 0; j < chromos[i].weight_gene2.Length; j++)
                        new_chromos[i].weight_gene2[j] = rnd.NextDouble() > (1 - ratio) ? chromos[selected[i]].weight_gene2[j] : chromos[i].weight_gene2[j];
                    for (int j = 0; j < chromos[i].bias_gene2.Length; j++)
                        new_chromos[i].bias_gene2[j] = rnd.NextDouble() > (1 - ratio) ? chromos[selected[i]].bias_gene2[j] : chromos[i].bias_gene2[j];
                }
            }
            chromos = new Gene[chromos.Count()];
            new_chromos.CopyTo(chromos, 0);
        }

        

        private void display_generation(int generation)
        {
            Console.WriteLine("Generation No." + generation.ToString() + " : " + " Best Chromo ID=" + best_chromo.ToString() + ", Estimated completion hour="+estimated_time_to_completion.ToString() + ", Best eva=" + best_eva.ToString());
            Console.WriteLine("Best num trade=" + best_ac.performance_data.num_trade.ToString() + " : " + "Best win rate=" + best_ac.performance_data.win_rate.ToString() + " : " + "Best total pl=" + best_ac.performance_data.total_pl.ToString());
            Console.WriteLine("---------------------------------------------------------------------------");
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
            StreamWriter sw = new StreamWriter(@"./best_weight.csv", false, Encoding.UTF8);
            sw.WriteLine("weight1,bias1,weight2,bias2");  // ヘッダ部出力
            /*
            for (int i = 0; i < chromos[best_chromo].position_gene.Count; i++)
            {
                sw.WriteLine(string.Format("{0},{1},{2},{3},{4},{5},{6},{7}", MarketData.Dt[i], MarketData.Open[i], MarketData.High[i], MarketData.Low[i], MarketData.Close[i], MarketData.Size[i],
                    MarketData.UnixTime[i], chromos[best_chromo].position_gene[i]));
            }
            */
            sw.Close();
            Console.WriteLine("Completed write best chromo.");
        }


    }
}
