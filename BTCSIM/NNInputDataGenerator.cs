using System;
using System.Linq;
using System.Collections.Generic;


namespace BTCSIM
{
    /*market indicies, holding side, holding period, unrealized pl, */
    public class NNInputDataGenerator
    {
        public double[] generateNNInputData(SimAccount ac, int i)
        {
            var input_data = new List<double>();

            //Divergence_minmax_scale
            //foreach (var d in MarketData.Divergence_minmax_scale[i])
            //    input_data.Add(d);
            //vola_kyori

            //ac holding side
            if (ac.holding_data.holding_side == "buy")
            {
                input_data.Add(0);
                input_data.Add(1);
                input_data.Add(0);
            }
            else if (ac.holding_data.holding_side == "sell")
            {
                input_data.Add(0);
                input_data.Add(0);
                input_data.Add(1);
            }
            else
            {
                input_data.Add(1);
                input_data.Add(0);
                input_data.Add(0);
            }


            //ac pl, 損益率を2unitにわけて表現する
            if (ac.performance_data.unrealized_pl == 0)
            {
                input_data.Add(0);
                input_data.Add(0);
            }
            else if (ac.performance_data.unrealized_pl > 0)
            {
                //unrealized_pl = amount * (price - holding_price)
                //(price - holding_price) / holding_price  <-目的式
                //(unrealized_pl / amount) / holding_price
                input_data.Add((ac.performance_data.unrealized_pl / ac.holding_data.holding_size) / (ac.holding_data.holding_price));
                input_data.Add(0);
            }
            else
            {
                input_data.Add(0);
                input_data.Add(-1.0 * (ac.performance_data.unrealized_pl / ac.holding_data.holding_size) / (ac.holding_data.holding_price));
            }

            //holding period
            if (ac.holding_data.holding_period == 0)
                input_data.Add(0);
            else
                input_data.Add(1.0 / ac.holding_data.holding_period);

            //unrealized pl / holding period
            if (ac.holding_data.holding_period == 0)
                input_data.Add(0);
            else
                input_data.Add(ac.performance_data.unrealized_pl / ac.holding_data.holding_period);

            //unrealize pl change


            return input_data.ToArray();
        }




        public double[] generateNNInputDataLimit(SimAccount ac, int i, int[] index)
        {
            var input_data = new List<double>();

            //Divergence_minmax_scale
            if (index[0] == 1)
            {
                foreach (var d in MarketData.Divergence_minmax_scale[i])
                    input_data.Add(d);
            }
            //vola_kyori_minmax_scale
            if (index[1] == 1)
            {
                foreach (var d in MarketData.Volakyori_minmax_scale[i])
                    input_data.Add(d);
            }
            //vol ma divergence minmax scale
            if (index[2] == 1)
            {
                foreach (var d in MarketData.Vol_ma_divergence_minmax_scale[i])
                    input_data.Add(d);
            }
            //buy sell vol ratio
            if (index[3] == 1)
            {
                foreach (var d in MarketData.Buysell_vol_ratio_minmax_scale[i])
                    input_data.Add(d);
            }
            //rsi
            if (index[4] == 1)
            {
                foreach (var d in MarketData.Rsi_scale[i])
                    input_data.Add(d);
            }
            //uwahige
            if (index[5] == 1)
            {
                foreach (var d in MarketData.Uwahige_scale[i])
                    input_data.Add(d);
            }
            //shitahige
            if (index[6] == 1)
            {
                foreach (var d in MarketData.Shitahige_scale[i])
                    input_data.Add(d);
            }

            if (input_data.Contains(Double.NaN))
                Console.WriteLine("NNInputDataGenerator: Nan is included !");

            //order side
            if (ac.order_data.order_side.Count > 0)
            {
                if (ac.order_data.getLastOrderSide() == "buy")
                {
                    input_data.Add(1);
                    input_data.Add(0);
                }
                else if (ac.order_data.getLastOrderSide() == "sell")
                {
                    input_data.Add(0);
                    input_data.Add(1);
                }
                else
                {
                    Console.WriteLine("Unknown order side! " + ac.order_data.order_side[ac.order_data.order_serial_list[0]]);
                    input_data.Add(0);
                    input_data.Add(0);
                }
            }
            else
            {
                input_data.Add(0);
                input_data.Add(0);
            }

            //holding side
            if (ac.holding_data.holding_side == "buy")
            {
                input_data.Add(1);
                input_data.Add(0);
            }
            else if (ac.holding_data.holding_side == "sell")
            {
                input_data.Add(0);
                input_data.Add(1);
            }
            else
            {
                input_data.Add(0);
                input_data.Add(0);
            }

            //holding size
            //assumed max amount = 5
            var max_amount = 3;
            for (int j = 0; j < max_amount; j++)
            {
                if (ac.holding_data.holding_size > j)
                    input_data.Add(1);
                else
                    input_data.Add(0);
            }


            //unrealized_pl = amount * (price - holding_price)
            //(price - holding_price) / holding_price  <-目的式
            //(unrealized_pl / amount) / holding_price
            //-20 - 20%の損益率を20unitで表現する。
            var pl_ratio = ac.holding_data.holding_size > 0 ? 100.0 * (ac.performance_data.unrealized_pl / ac.holding_data.holding_size) / (ac.holding_data.holding_price) : 0;
            for (int j = 1; j < 21; j++)
            {
                if (pl_ratio >= -20 + (j * 2.0))
                    input_data.Add(1);
                else
                    input_data.Add(0);
            }

            //holding period
            for (int j = 1; j < 21; j++)
            {
                if (ac.holding_data.holding_period >= j * 10)
                    input_data.Add(1);
                else
                    input_data.Add(0);
            }

            return input_data.ToArray();
        }


        /*
         * 直近一定期間のraw dataをminmax scaleしたデータを入力に使う。
         */
        public double[] generateNNInputRawData(SimAccount ac, int i, int[] index)
        {
            var input_data = new List<double>();
            return input_data.ToArray(); ;
        }
    }
}
