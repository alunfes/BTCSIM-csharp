using System;
using System.Linq;
using System.Collections.Generic;

namespace BTCSIM
{
    /*market indicies, holding side, holding period, unrealized pl, */
    public static class NNInputDataGenerator
    {
        public static double[] generateNNInputData(SimAccount ac, int i)
        {
            var input_data = new List<double>();

            //Divergence_minmax_scale
            input_data = MarketData.Divergence_minmax_scale[i];
            
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

            //ac holding period


            //ac pl, 損益率を2unitにわけて表現する
            if (ac.performance_data.unrealized_pl == 0)
            {
                input_data.Add(0);
                input_data.Add(0);
            }
            else if (ac.performance_data.unrealized_pl > 0)
            {
                //unrealized_pl = amount * (price - holding_price) / holding_price
                //(price - holding_price) / holding_price
                //(unrealized_pl / amount) / holding_price
                input_data.Add( 100.0 * (ac.performance_data.unrealized_pl / ac.holding_data.holding_size) / (ac.holding_data.holding_price) );
                input_data.Add(0);
            }
            else
            {
                input_data.Add(0);
                input_data.Add(-100.0 * (ac.performance_data.unrealized_pl / ac.holding_data.holding_size) / (ac.holding_data.holding_price));
            }

            
            return input_data.ToArray();
        }
    }
}
