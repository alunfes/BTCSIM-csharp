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
    /*
    public static class LINQExtension
    {
        public static double Median(this IEnumerable<double>? source)
        {
            if (!(source?.Any() ?? false))
            {
                throw new InvalidOperationException("Cannot compute median for a null or empty set.");
            }
            var sortedList = (from number in source
                              orderby number
                              select number).ToList();
            int itemIndex = sortedList.Count / 2;
            if (sortedList.Count % 2 == 0)
            {
                // Even number of items.
                return (sortedList[itemIndex] + sortedList[itemIndex - 1]) / 2;
            }
            else
            {
                // Odd number of items.
                return sortedList[itemIndex];
            }
        }
    }*/

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
            var random_generator = new RandomGenerator();
            this.num_units = units;
            weight_gene1 = new double[units[0] * units[1]];
            bias_gene1 = new double[units[1]];
            weight_gene2 = new double[units[1] * units[2]];
            bias_gene2 = new double[units[2]];
            weight_gene1 = random_generator.getRandomArray(units[0] * units[1]);
            bias_gene1 = random_generator.getRandomArray(units[1]);
            weight_gene2 = random_generator.getRandomArray(units[1] * units[2]);
            bias_gene2 = random_generator.getRandomArray(units[2]);
        }
    }

    //for multile middle layer and dic type weights
    public class Gene2
    {
        public List<Dictionary<int, double[]>> weight_gene { get; set; } //weight_gene[layer][output unit][input unit] -> [<inputs units id as key, double[num middle units-1]>, <middle units id as key, double[num middle units-2>, ..]
        public List<double[]> bias_gene { get; set; } //bias_gene[layer][output unit]  ->  [num_unit[1], num_unit[2], .. num_unit[num_layers - 1]]
        public int[] num_units { get; set; } //[num_inputs, num_middle, num_middle2... , num_output]

        public Gene2(int[] units)
        {
            var random_generator = new RandomGenerator();
            this.num_units = units;
            weight_gene = new List<Dictionary<int, double[]>>();
            bias_gene = new List<double[]>();
            //initialize weight / bias
            for (int i = 1; i < units.Length; i++) //for layers
            {
                var weight = new Dictionary<int, double[]>();
                for (int j = 0; j < units[i]; j++) //for units in a layer
                {
                    weight[j] = random_generator.getRandomArray(units[i - 1]);
                    
                }
                weight_gene.Add(weight);
                var gene = random_generator.getRandomArray(units[i]);
                bias_gene.Add(gene);
            }
        }

    }


    public class GA
    {
        public Gene2[] chromos { get; set; }
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
        public int island_id { get; set; }

        private RandomGenerator random_generator { get; set; }



        public GA(int island_id)
        {
            RandomSeed.initialize();
            generation_time_log = new List<int>();
            estimated_time_to_completion = -1;
            best_chromo_log = new List<int>();
            best_eva_log = new List<double>();
            best_ac_log = new List<SimAccount>();
            random_generator = new RandomGenerator();
            this.island_id = island_id;
        }


        public Gene2 readWeights(int island_id)
        {
            using (StreamReader sr = new StreamReader(@"./best_weight_ID-" + island_id.ToString() + ".csv", Encoding.UTF8, false))
            {
                var data = new List<string>();
                var units = new List<int>();
                var bias = new List<double[]>();
                var　weights = new List<Dictionary<int, double[]>>();
                var layer_id_list = new List<int>();
                
                while (true)
                {
                    var line = sr.ReadLine();
                    data.Add(line);
                    if (line == null)
                        break;
                    else
                    {
                        if (line.Contains("units"))
                        {
                            var ele = line.Split(',').ToList();
                            units = ele.GetRange(1, ele.Count - 1).Select(int.Parse).ToList();
                        }
                        else if (line.Contains("bias"))
                        {
                            var ele = line.Split(',').ToList();
                            var ele_range = ele.GetRange(1, ele.Count - 1).Select(double.Parse).ToList();
                            bias.Add(ele_range.ToArray());
                        }
                        else if (line.Contains("weight")) //weight:0:0,-0.369,0.9373   -> weight:layer:unit
                        {
                            var ele = line.Split(',').ToList();
                            var ele_range = ele.GetRange(1, ele.Count - 1).Select(double.Parse).ToArray();
                            var layer_id = Convert.ToInt32(ele[0].Split(':')[1]);
                            var unit_id = Convert.ToInt32(ele[0].Split(':')[2]);
                            var dic = new Dictionary<int, double[]>();
                            dic[unit_id] = ele_range;
                            if (layer_id_list.Contains(layer_id))
                                weights[layer_id][unit_id] = ele_range;
                            else
                                weights.Add(dic);
                            layer_id_list.Add(layer_id);
                        }
                    }
                }
                data.RemoveAt(data.Count - 1); //remove null
                var chrom = new Gene2(units.ToArray());
                chrom.bias_gene = bias;
                chrom.weight_gene = weights;
                return chrom;
            }
        }

        public SimAccount sim_ga(int from, int to, Gene2 chromo, string title)
        {
            var sim = new Sim();
            var ac = new SimAccount();
            ac = sim.sim_ga(from, to, chromo, ac);
            Console.WriteLine("pl=" + ac.performance_data.total_pl);
            Console.WriteLine("num trade=" + ac.performance_data.num_trade);
            Console.WriteLine("win rate=" + ac.performance_data.win_rate);
            Console.WriteLine("sharp_ratio=" + ac.performance_data.sharp_ratio);
            LineChart.DisplayLineChart(ac.total_pl_list, title);
            return ac;
        }

        public SimAccount sim_ga_limit(int from, int to, int max_amount, Gene2 chromo, string title, bool chart)
        {
            var sim = new Sim();
            var ac = new SimAccount();
            ac = sim.sim_ga_limit(from, to, max_amount, chromo, ac);
            Console.WriteLine("pl=" + ac.performance_data.total_pl);
            Console.WriteLine("num trade=" + ac.performance_data.num_trade);
            Console.WriteLine("win rate=" + ac.performance_data.win_rate);
            Console.WriteLine("sharp_ratio=" + ac.performance_data.sharp_ratio);
            if (chart)
                LineChart.DisplayLineChart(ac.total_pl_list, title);
            return ac;
        }

        public SimAccount sim_ga_market_limit(int from, int to, int max_amount, Gene2 chromo, string title, bool chart)
        {
            var sim = new Sim();
            var ac = new SimAccount();
            ac = sim.sim_ga_market_limit(from, to, max_amount, chromo, ac);
            Console.WriteLine("pl=" + ac.performance_data.total_pl);
            Console.WriteLine("num trade=" + ac.performance_data.num_trade);
            Console.WriteLine("win rate=" + ac.performance_data.win_rate);
            Console.WriteLine("sharp_ratio=" + ac.performance_data.sharp_ratio);
            if (chart)
                LineChart.DisplayLineChart(ac.total_pl_list, title);
            return ac;
        }

        public SimAccount sim_ga_limit_conti(int from, int to, int max_amount, Gene2 chromo, string title, SimAccount ac, bool chart)
        {
            var sim = new Sim();
            ac = sim.sim_ga_limit(from, to, max_amount, chromo, ac);
            Console.WriteLine("pl=" + ac.performance_data.total_pl);
            Console.WriteLine("num trade=" + ac.performance_data.num_trade);
            Console.WriteLine("win rate=" + ac.performance_data.win_rate);
            Console.WriteLine("sharp_ratio=" + ac.performance_data.sharp_ratio);
            if (chart)
                LineChart.DisplayLineChart(ac.total_pl_list, title);
            return ac;
        }


        public void start_island_ga(int from, int to, int max_amount, int num_chromos, int generation_ind, int[] units, double mutation_rate, int sim_type)
        {
            if (generation_ind == 0)
                generate_chromos(num_chromos, units);
            var eva_dic = new ConcurrentDictionary<int, double>();
            var ac_dic = new ConcurrentDictionary<int, SimAccount>();
            var option = new ParallelOptions();
            option.MaxDegreeOfParallelism = System.Environment.ProcessorCount;
            /*
            Parallel.For(0, chromos.Length, option, j =>
            {
                (double total_pl, SimAccount ac) res = evaluation(from, to, max_amount, j, chromos[j], sim_type);
                eva_dic.GetOrAdd(j, res.total_pl);
                ac_dic.GetOrAdd(j, res.ac);
            });
            */
            //Console.WriteLine("island No."+island_id.ToString() + ", eva time="+sw.Elapsed.Seconds.ToString());
            for (int k = 0; k < chromos.Length; k++)
            {
                (double total_pl, SimAccount ac) res = evaluation(from, to, max_amount, k, chromos[k], sim_type);
                eva_dic.GetOrAdd(k, res.total_pl);
                ac_dic.GetOrAdd(k, res.ac);
            }
            //check best eva
            check_best_eva(eva_dic, ac_dic);
            //roulette selection
            var selected_chro_ind_list = roulette_selection(eva_dic);
            //cross over
            crossover(selected_chro_ind_list, 0.3);
            //mutation
            mutation(mutation_rate, -1, 1);
            write_best_chromo();
            eva_dic = null;
            ac_dic = null;
        }


        public void start_ga(int from, int to, int max_amount, int num_chromos, int num_generations, int[] units, double mutation_rate, bool display_info, int sim_type)
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

                var option = new ParallelOptions();
                option.MaxDegreeOfParallelism = System.Environment.ProcessorCount;
                Parallel.For(0, chromos.Length, option, j =>
                {
                    (double total_pl, SimAccount ac) res = evaluation(from, to, max_amount, j, chromos[j], sim_type);
                    eva_dic.GetOrAdd(j, res.total_pl);
                    ac_dic.GetOrAdd(j, res.ac);
                });
                /*
                for (int k =0; k<chromos.Length; k++)
                {
                    (double total_pl, SimAccount ac) res = evaluation(from, to, k, chromos[k]);
                    eva_dic.GetOrAdd(k, res.total_pl);
                    ac_dic.GetOrAdd(k, res.ac);
                }*/


                //check best eva
                check_best_eva(eva_dic, ac_dic);
                //roulette selection
                var selected_chro_ind_list = roulette_selection(eva_dic);
                //cross over
                crossover(selected_chro_ind_list, 0.3);
                //mutation
                mutation(mutation_rate, -1, 1);
                generationWatch.Stop();
                generation_time_log.Add(generationWatch.Elapsed.Seconds);
                calc_time_to_complete_from_generation_time(i, num_generations);
                if (display_info)
                    display_generation(i, generationWatch);
                write_best_chromo();
            }
            Console.WriteLine("Completed GA.");


        }

        private void generate_chromos(int num_chrom, int[] num_units_layer)
        {
            chromos = new Gene2[num_chrom];
            for (int i = 0; i < num_chrom; i++)
                chromos[i] = new Gene2(num_units_layer);
        }

        private (double, SimAccount) evaluation(int from, int to, int max_amount, int chro_id, Gene2 chro, int sim_type)
        {
            var ac = new SimAccount();
            var sim = new Sim();
            //ac = sim.sim_ga(from, to, chro, ac);
            if (sim_type == 0)
                ac = sim.sim_ga_limit(from, to, max_amount, chro, ac);
            else if (sim_type == 1)
                ac = sim.sim_ga_market_limit(from, to, max_amount, chro, ac);
            else
                Console.WriteLine("GA-evaluation: Invalid Sim Type!");
            return (ac.performance_data.total_pl * Math.Sqrt(ac.performance_data.num_trade), ac);
        }

        private void check_best_eva(ConcurrentDictionary<int, double> eva, ConcurrentDictionary<int, SimAccount> ac)
        {
            var max_eva = -99999999.0;
            var eva_key = eva.Keys.ToArray();
            int best_eva_key = -1;
            foreach (var k in eva_key)
            {
                if (eva[k] > max_eva)
                {
                    max_eva = eva[k];
                    best_eva_key = k;
                }
            }
            //best_ac_log.Add(ac[best_eva_key]);  may cause memory leake
            best_ac = ac[best_eva_key];
            best_chromo = best_eva_key;
            best_chromo_log.Add(best_eva_key);
            best_eva = max_eva;
            best_eva_log.Add(max_eva);
        }

        /*eva.valueにminを加算して、合計値を10000に置き換えてそれぞれの値を計算。
         */
        private List<int> roulette_selection(ConcurrentDictionary<int, double> eva)
        {
            var selected_chro_ind = new List<int>();
            List<int> roulette_board = new List<int>();

            //全部の値が同じときは同じ割合でroulette boardを作る
            var flg_same = true;
            for (int i = 1; i < eva.Count; i++)
            {
                if (eva[0] != eva[i])
                {
                    flg_same = false;
                    break;
                }
            }
            if (flg_same)
            {
                var ave_val = Convert.ToInt32(Math.Round(10000.0 / eva.Count));
                for (int i = 0; i < eva.Count; i++)
                    roulette_board.Add((i + 1) * ave_val);
            }
            else
            {
                List<double> vals = new List<double>();
                var min = eva.Values.Min();
                for (int i = 0; i < eva.Count; i++)
                    vals.Add(eva[i] - min);//evaのkeyが0-count-1までの連続値になっていることが前提

                List<double> con_vals = new List<double>();
                var sumv = vals.Sum();
                var tmp_val = 0;
                foreach (var v in vals)
                {
                    tmp_val += Convert.ToInt32(Math.Round(10000 * v / sumv));
                    roulette_board.Add(tmp_val);
                }
            }

            Random rnd = new Random(DateTime.Now.Millisecond);
            for (int i = 0; i < chromos.Count(); i++)
            {

                if (i == best_chromo)
                {
                    selected_chro_ind.Add(-1); //best chromoはroulette selectしなくて良い
                }
                else
                {
                    var selected = rnd.Next(0, roulette_board.Last() + 1);
                    if (selected <= roulette_board[0])
                        selected_chro_ind.Add(0);
                    else
                    {
                        for (int j = 1; j < roulette_board.Count; j++)
                        {
                            if (selected > roulette_board[j - 1] && selected <= roulette_board[j])
                                selected_chro_ind.Add(j);
                        }
                    }
                    if (selected_chro_ind.Last() == i) //選択したidが自身のidと同じときはやり直し
                    {
                        i--;
                        selected_chro_ind.RemoveAt(selected_chro_ind.Count - 1);
                    }
                }
            }
            if (selected_chro_ind.Count != chromos.Count())
                Console.WriteLine("selected ind is not matched with num chromo in roulette selection!");
            return selected_chro_ind;
        }


        private void mutation(double mutation_ratio, int random_weight_min, int random_weight_max)
        {
            Random rnd = new Random(DateTime.Now.Millisecond);
            for (int i = 0; i < chromos.Count(); i++)
            {
                if (i != best_chromo)
                {
                    for (int j = 0; j < chromos[i].bias_gene.Count; j++)
                    {
                        for (int k = 0; k < chromos[i].bias_gene[j].Length; k++)
                            chromos[i].bias_gene[j][k] = rnd.NextDouble() > (1 - mutation_ratio) ? random_generator.getRandomArrayRange(random_weight_min, random_weight_max) : chromos[i].bias_gene[j][k];
                    }
                    for (int j = 0; j < chromos[i].weight_gene.Count; j++)
                    {
                        for (int k = 0; k < chromos[i].weight_gene[j].Count; k++)
                        {
                            for (int l = 0; l < chromos[i].weight_gene[j][k].Length; l++)
                                chromos[i].weight_gene[j][k][l] = rnd.NextDouble() > (1 - mutation_ratio) ? random_generator.getRandomArrayRange(random_weight_min, random_weight_max) : chromos[i].weight_gene[j][k][l];
                        }
                    }
                }
            }
        }


        //reset chromos with random weigths except
        public void resetChromos()
        {
            for (int i = 0; i < chromos.Length; i++)
                chromos[i] = new Gene2(chromos[i].num_units);
        }


        private void crossover(List<int> selected, double cross_over_ratio)
        {
            var rnd = new Random(DateTime.Now.Millisecond);
            var new_chromos = new Gene2[chromos.Count()];

            //deep copy chromos
            for (int i = 0; i < new_chromos.Length; i++)
                new_chromos[i] = new Gene2(chromos[0].num_units);

            for (int i = 0; i < new_chromos.Length; i++)
            {
                for (int j = 0; j < chromos[i].bias_gene.Count; j++)
                {
                    for (int k = 0; k < chromos[i].bias_gene[j].Length; k++)
                        new_chromos[i].bias_gene[j][k] = chromos[i].bias_gene[j][k];
                }
                for (int j = 0; j < chromos[i].weight_gene.Count; j++)
                {
                    for (int k = 0; k < chromos[i].weight_gene[j].Count; k++)
                    {
                        for (int l = 0; l < chromos[i].weight_gene[j][k].Length; l++)
                            new_chromos[i].weight_gene[j][k][l] = chromos[i].weight_gene[j][k][l];
                    }
                }
            }

            for (int i = 0; i < chromos.Count(); i++)
            {
                if (i != best_chromo)
                {
                    //bias1/2, weight1/2からそれぞれからランダムにratio %のweightを選択して交配
                    for (int j = 0; j < chromos[i].weight_gene.Count; j++)
                    {
                        for (int k = 0; k < chromos[i].weight_gene[j].Count; k++)
                        {
                            if (rnd.NextDouble() > (1 - cross_over_ratio))
                            {
                                new_chromos[i].weight_gene[j][k] = chromos[selected[i]].weight_gene[j][k];
                                new_chromos[i].bias_gene[j] = chromos[selected[i]].bias_gene[j];
                            }
                            else
                            {
                                new_chromos[i].weight_gene[j][k] = chromos[i].weight_gene[j][k];
                                new_chromos[i].bias_gene[j] = chromos[i].bias_gene[j];
                            }
                        }
                    }
                }
            }
            chromos = new Gene2[chromos.Count()];
            new_chromos.CopyTo(chromos, 0);
        }



        private void display_generation(int generation, Stopwatch watch)
        {
            Console.WriteLine("Generation No." + generation.ToString() + " : " + " Best Chromo ID=" + best_chromo.ToString() + ", Estimated completion hour=" + estimated_time_to_completion.ToString() + ", Best eva=" + best_eva.ToString() + ", time elapsed:" + watch.Elapsed.Minutes.ToString());
            Console.WriteLine("Best num trade=" + best_ac.performance_data.num_trade.ToString() + " : " + "Best win rate=" + best_ac.performance_data.win_rate.ToString() + " : " + "Best total pl=" + best_ac.performance_data.total_pl.ToString() + " : " + "Best sharp ratio=" + best_ac.performance_data.sharp_ratio.ToString());
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
            //Console.WriteLine("Writing Best Chromo...");
            using (StreamWriter sw = new StreamWriter(@"./best_weight_ID-" + island_id.ToString() + ".csv", false, Encoding.UTF8))
            {
                //units
                var units = "units," + string.Join(",", chromos[best_chromo].num_units);
                sw.WriteLine(units);
                //bias
                for (int i = 0; i < chromos[best_chromo].bias_gene.Count; i++)
                {
                    var bias = "bias" + i.ToString() + "," + string.Join(",", chromos[best_chromo].bias_gene[i]);
                    sw.WriteLine(bias);
                }
                //weight
                for (int i=0; i<chromos[best_chromo].weight_gene.Count; i++)
                {
                    foreach (var key in chromos[best_chromo].weight_gene[i].Keys)
                    {
                        var weights = "weight:" + i.ToString() + ":" + key.ToString() +"," + string.Join(",", chromos[best_chromo].weight_gene[i][key]);
                        sw.WriteLine(weights);
                    }
                }
            }
            //Console.WriteLine("Completed write best chromo.");
        }
    }
}
