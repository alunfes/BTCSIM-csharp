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
        public List<int> opt_positions { get; set; }

        public SimOptimizer()
        {
            opt_positions = new List<int>();
        }

        public void start_optimization(int opt_window_size, double kijun_val, int from, int to)
        {
            //check for first period
            if (MarketData.Close[from + opt_window_size-1] - MarketData.Open[from] > 0)
            {
                for (int i = 0; i< opt_window_size; i++)
                    opt_positions.Add(1);
            }
            else
            {
                for (int i = 0; i < opt_window_size; i++)
                    opt_positions.Add(2);
            }
            //optimize for all periods
            int current_ind = from + opt_window_size - 1;
            while (current_ind <= to - opt_window_size)
            {
                if (MarketData.Close[current_ind + opt_window_size] - MarketData.Close[current_ind] >= kijun_val)
                {
                    for (int i = 0; i < opt_window_size; i++)
                        opt_positions.Add(1);
                }
                else if (MarketData.Close[current_ind + opt_window_size] - MarketData.Close[current_ind] <= -kijun_val)
                {
                    for (int i = 0; i < opt_window_size; i++)
                        opt_positions.Add(2);
                }
                else
                {
                    for (int i = 0; i < opt_window_size; i++)
                        opt_positions.Add(opt_positions.Last());
                }
                current_ind += opt_window_size;
            }
            //
            var ac = new SimAccount();
            var sim = new Sim();
            var chro = new Gene(opt_positions.Count);
            chro.position_gene = new List<int>(opt_positions);
            ac = sim.sim_ga(from, to, 1, chro, ac);
            Console.WriteLine("pl=" + ac.performance_data.total_pl);
            Console.WriteLine("num trade=" + ac.performance_data.num_trade);
            Console.WriteLine("win rate=" + ac.performance_data.win_rate);
            Console.WriteLine("sharp_ratio=" + ac.performance_data.sharp_ratio);
            LineChart.DisplayLineChart(ac.log_data.total_pl_log);
        }


    }
}
