using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace BTCSIM
{
    /*短い期間（３分とか）のデータに対して現時点から期間終了時点の価格差から持つべきポジションを計算する。
     * その後、opt_position(3m)をベースに少し長い期間（30分とか）のデータに対して同じように
     */
    
    public class SimOptimizer
    {
        /*
        public List<int> opt_positions { get; set; }
        public List<int> opt_timing { get; set; }
        private List<int> index_log { get; set; }

        public SimOptimizer()
        {
            opt_positions = new List<int>();
            opt_timing = new List<int>();
            index_log = new List<int>();
        }

        public SimAccount start_optimization(int opt_window_size, double kijun_val, int from, int to, double random_ratio, int conti_num)
        {
            //check for first period
            if (MarketData.Close[opt_window_size] - MarketData.Close[0] > 0)
            {
                for (int i = 0; i < opt_window_size; i++)
                {
                    opt_positions.Add(1);
                    index_log.Add(i);
                }
            }
            else
            {
                for (int i = 0; i < opt_window_size; i++)
                {
                    opt_positions.Add(2);
                    index_log.Add(i);
                }
            }
            //optimize for all periods
            int current_ind = opt_window_size;
            //while (current_ind <= to - opt_window_size)
            while(opt_positions.Count() < MarketData.Close.Count() - opt_window_size)
            {
                if (MarketData.Close[current_ind + opt_window_size] - MarketData.Close[current_ind] >= kijun_val)
                {
                    for (int i = 0; i < opt_window_size; i++)
                    {
                        opt_positions.Add(1);
                        index_log.Add(i + current_ind);
                    }
                }
                else if (MarketData.Close[current_ind + opt_window_size] - MarketData.Close[current_ind] <= -kijun_val)
                {
                    for (int i = 0; i < opt_window_size; i++)
                    {
                        opt_positions.Add(2);
                        index_log.Add(i + current_ind);
                    }
                }
                else
                {
                    for (int i = 0; i < opt_window_size; i++)
                    {
                        opt_positions.Add(opt_positions.Last());
                        index_log.Add(i + current_ind);
                    }
                }
                current_ind += opt_window_size;
            }
            if (opt_positions.Count() != MarketData.Close.Count() - opt_window_size)
            {
                Console.WriteLine("##########################################");
                Console.WriteLine("num_opt_positon:"+opt_positions.Count().ToString());
                Console.WriteLine("index_log:" + index_log.Last());
                Console.WriteLine("to:"+to.ToString());
                Console.WriteLine("len(MarketData.close:" + MarketData.Close.Count().ToString());
                Console.WriteLine("##########################################");
            }
            generate_opt_timing();
            WriteOptData(opt_window_size, from, to);
            WriteOptTimingData(opt_window_size, from, to);
            var ac = new SimAccount();
            var sim = new Sim();
            var chro = new Gene(opt_positions.Count);
            chro.position_gene = new List<int>(opt_positions);
            if (random_ratio > 0)
                chro = random_opt_position(random_ratio, chro);
            //ac = sim.sim_ga(from, to, 1, chro, ac);
            ac = sim.sim_ga_limit(from, to, 1, chro, ac);
            //ac = sim.sim_ga_limit_conti_entry(from, to, 1, chro, ac, conti_num);
            Console.WriteLine("pl=" + ac.performance_data.total_pl);
            Console.WriteLine("num trade=" + ac.performance_data.num_trade);
            Console.WriteLine("win rate=" + ac.performance_data.win_rate);
            Console.WriteLine("sharp_ratio=" + ac.performance_data.sharp_ratio);
            Console.WriteLine("fee/total_pl ratio=" + ac.performance_data.total_fee / ac.performance_data.total_pl);
            LineChart.DisplayLineChart(ac.log_data.total_pl_log, opt_window_size, kijun_val);
            return ac;
        }

        private void generate_opt_timing()
        {
            opt_positions.Add(0);
            for (int i=1; i<opt_positions.Count(); i++)
            {
                if (opt_positions[i - 1] != opt_positions[i])
                    opt_timing.Add(opt_positions[i] == 1 ? 1 : 2);
                else
                    opt_timing.Add(0);
            }
        }

        private Gene random_opt_position(double random_ratio, Gene gene)
        {
            Random rnd = new Random();
            for (int i=0; i<gene.position_gene.Count(); i++)
            {
                if (rnd.NextDouble() < random_ratio)
                    gene.position_gene[i] = gene.position_gene[i] == 1 ? 2 : 1;
            }
            return gene;
        }

        private void WriteOptData(int opt_window_size, int from, int to)
        {
            Console.WriteLine("writing opt data...");
            StreamWriter writer = new StreamWriter(@"./Data/onemin_bybit_opt.csv", false, Encoding.GetEncoding("UTF-8"));
            writer.WriteLine("dt,open,high,low,close,size,opt_position");
            for (int i=0; i<opt_positions.Count; i++)
            {
                writer.WriteLine(MarketData.Dt[i].ToString() +","+ MarketData.Open[i] + "," + MarketData.High[i] + "," + MarketData.Low[i] + "," + MarketData.Close[i] + "," +
                    MarketData.Size[i] + "," + /*MarketData.UnixTime[i + from] + "," + opt_positions[i]);
            }
        }


        private void WriteOptTimingData(int opt_window_size, int from, int to)
        {
            Console.WriteLine("writing opt timing data...");
            StreamWriter writer = new StreamWriter(@"./Data/onemin_bybit_opt_timing.csv", false, Encoding.GetEncoding("UTF-8"));
            writer.WriteLine("dt,open,high,low,close,size,opt_timing,opt_position");
            for (int i = 0; i < opt_timing.Count; i++)
            {
                writer.WriteLine(MarketData.Dt[i].ToString() + "," + MarketData.Open[i] + "," + MarketData.High[i] + "," + MarketData.Low[i] + "," + MarketData.Close[i] + "," +
                    MarketData.Size[i] + "," + /*MarketData.UnixTime[i + from] + "," + opt_timing[i] + ","+opt_positions[i]);
            }
        }
        */
    }
}
