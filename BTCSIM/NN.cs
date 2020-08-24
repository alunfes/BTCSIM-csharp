using System;
namespace BTCSIM
{
    static public class NN
    {
        /*outputs: entry buy, entry sell, exit all-limit, exit all-market, cancel order, update order
         */
        static public StrategyActionData CalcNN(int i, SimAccount ac, Gene gene)
        {
            //input data
            /*
            ac.performance_data.total_pl; //profit, loss, no (一定値以上0.075bpsくらい）
            ac.holding_data.holding_side; //buy, sell, no
            ac.holding_data.holding_size; //size (0, 0.5, 1.0)
            ac.holding_data.holding_price - MarketData.Close[i]; // sa(-100以下、-100~-50, -50~0, 
            ac.order_data.order_side; //buy, sell, no
            ac.order_data.order_price; //
            */



            return new StrategyActionData();

        }


    }
}
