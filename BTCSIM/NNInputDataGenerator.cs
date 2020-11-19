using System;
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
            foreach (var d in MarketData.Divergence_minmax_scale[i])
                input_data.Add(d);
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
                input_data.Add((ac.performance_data.unrealized_pl / ac.holding_data.holding_size) / (ac.holding_data.holding_price) );
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


        public double[] generateNNInputDataLimit(SimAccount ac, int i)
        {
            var input_data = new List<double>();

            //Divergence_minmax_scale
            //foreach (var d in MarketData.Divergence_minmax_scale[i])
             //   input_data.Add(d);

            if (ac.order_data.order_side.Count > 0)
            {
                if (ac.order_data.getLastOrderSide()=="buy")
                {
                    input_data.Add(0);
                    input_data.Add(1);
                    input_data.Add(0);
                }
                else if (ac.order_data.getLastOrderSide() == "sell")
                {
                    input_data.Add(0);
                    input_data.Add(0);
                    input_data.Add(1);
                }
                else
                {
                    Console.WriteLine("Unknown order side! " + ac.order_data.order_side[ac.order_data.order_serial_list[0]]);
                    input_data.Add(0);
                    input_data.Add(0);
                    input_data.Add(0);
                }
            }
            else
            {
                input_data.Add(0);
                input_data.Add(0);
                input_data.Add(0);
            }



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
                input_data.Add(-1);
            else
                input_data.Add(1.0 / ac.holding_data.holding_period);

            //unrealized pl / holding period
            if (ac.holding_data.holding_period == 0)
                input_data.Add(0);
            else
                input_data.Add(ac.performance_data.unrealized_pl / ac.holding_data.holding_period);

            //holding size
            if (ac.holding_data.holding_size == 0)
                input_data.Add(0);
            else
                input_data.Add(ac.holding_data.holding_size / 10.0);

            //unrealize pl change
            

            return input_data.ToArray();
        }
    }
}
