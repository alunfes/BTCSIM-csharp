using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace BTCSIM
{
    public class WinSim
    {
        public WinSim()
        {
        }


        /*
         * Window内の右端からbuy or sellの有無を確認して、buy / sellがあれば記録する。
         * Buy / selが記録された状態で反対のbuy / sellがあれば値幅として記録。最後に残ったbuy / sellは無視。（最後のプライスで値幅を考慮すると結局全てbuy出すようになる気がする）
         */
        public SimAccount sim_win_market(int from, int to, List<int[]> sim_windows, Gene2 chromo, SimAccount ac, double nn_threshold)
        {
            var nn = new NN();
            var nn_input_data_generator = new NNInputDataGenerator();
            var pred_list = new List<int>();
            for(int i=from; i<to+1; i++)
            {
                var nn_inputs = nn_input_data_generator.generateNNWinGA(i, chromo.num_index);
                var nn_outputs = nn.calcNN(nn_inputs, chromo, 0);
                pred_list.Add(nn.getActivatedUnitOnlyBuySell(nn_outputs, nn_threshold));    
            }

            double maker_fee = -0.00025;
            int num_trade = 0;
            double total_nehaba = 0;
            for(int i=0; i<sim_windows.Count; i++)
            {
                var buy_price = new List<double>();
                var sell_price = new List<double>();
                for (int j=sim_windows[i][0]; j<= sim_windows[i][1]; j++)
                {
                    if (pred_list[j-from] == 1 && sell_price.Count == 0)
                        buy_price.Add(MarketData.Bid[j] * (1 + maker_fee));
                    else if (pred_list[j - from] == 2 && buy_price.Count == 0)
                        sell_price.Add(MarketData.Ask[j] * (1 - maker_fee));
                    else if (pred_list[j - from] == 1 && sell_price.Count > 0) //exit sell position
                    {
                        var pl = (sell_price[0] - MarketData.Bid[j] * (1 + maker_fee));
                        ac.performance_data.total_pl += pl;
                        ac.performance_data.sell_pl_list.Add(pl);
                        ac.performance_data.realized_pl_list.Add(pl);
                        ac.performance_data.num_trade ++;
                        total_nehaba += pl;
                        num_trade ++;
                        sell_price.RemoveAt(0);
                    }
                    else if (pred_list[j - from] == 2 && buy_price.Count > 0) //exit buy position
                    {
                        var pl = (MarketData.Ask[j] * (1 - maker_fee) - buy_price[0]);
                        ac.performance_data.total_pl += pl;
                        ac.performance_data.buy_pl_list.Add(pl);
                        ac.performance_data.realized_pl_list.Add(pl);
                        ac.performance_data.num_trade++;
                        total_nehaba += pl;
                        num_trade++;
                        buy_price.RemoveAt(0);
                    }
                }
            }
            return ac;
        }
    }
}
