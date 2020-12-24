using System;
using System.Collections.Generic;
using System.Linq;

namespace BTCSIM
{
    public class StrategyActionData
    {
        public List<string> action;
        public List<string> order_side;
        public List<string> order_type;
        public List<double> order_price;
        public List<double> order_size;
        public List<int> order_serial_num;
        public List<string> order_message;

        public StrategyActionData()
        {
            action = new List<string>();
            order_price = new List<double>();
            order_side = new List<string>();
            order_size = new List<double>();
            order_type = new List<string>();
            order_serial_num = new List<int>();
            order_message = new List<string>();
        }
        public void add_action(string action, string side, string type, double price, double size, int serial_num, string message)
        {
            this.action.Add(action);
            order_side.Add(side);
            order_type.Add(type);
            order_price.Add(price);
            order_size.Add(size);
            order_serial_num.Add(serial_num);
            order_message.Add(message);
        }
    }

    public class Strategy
    {
        public StrategyActionData GAStrategy(int nn_output, int amount, SimAccount ac)
        {
            var ad = new StrategyActionData();
            var pred_side = nn_output == 1 ? "buy" : "sell";
            if (nn_output == 0)
                pred_side = "no";
            if (pred_side == "buy" && ac.holding_data.holding_side == "")
                ad.add_action("entry", pred_side, "market", 0, amount, -1, "entry order");
            else if (pred_side == "buy" && ac.holding_data.holding_side == "sell")
                ad.add_action("entry", pred_side, "market", 0, ac.holding_data.holding_size + amount, -1, "exit & entry order");
            else if (pred_side == "sell" && ac.holding_data.holding_side == "")
                ad.add_action("entry", pred_side, "market", 0, amount, -1, "entry order");
            else if (pred_side == "sell" && ac.holding_data.holding_side == "buy")
                ad.add_action("entry", pred_side, "market", 0, ac.holding_data.holding_size + amount, -1, "exit & entry order");
            return ad;
        }


        /*常にlimit entry、num order = 1以下。
        1. pred_side == order_sideのときは、max_amountに達するまでamountを追加してupdate price
        2. pred_side != order_sideのときは、cancel all orders, pred_sideにamountのlimit orderを出す。
        3. pred_side == holding_sideのときは、max_amountに達するまでamountのlimit orderを出す。
        4. pred_side != holding_sideのときは、amount + holding_sizeのlimit orderを出す
        
         */
        public StrategyActionData GALimitStrategy(int i, int nn_output, int amount, int max_amount, SimAccount ac)
        {
            var ad = new StrategyActionData();
            var output_action_list = new string[] {"no", "buy", "sell", "cancel"};
            var pred_side = output_action_list[nn_output];
            if (pred_side == "no")
            {
            }
            else if (pred_side == "cancel")
            {
                if (ac.order_data.getLastSerialNum() > 0)
                    ad.add_action("cancel", "", "", 0, 0, ac.order_data.order_serial_list.Last(), "cancel all order");
            }
            else
            {
                if (pred_side == ac.order_data.getLastOrderSide()) //1.
                {
                    if (ac.holding_data.holding_size + ac.order_data.getLastOrderSize() < max_amount)
                        ad.add_action("update amount", pred_side, "limit", 0, ac.order_data.getLastOrderSize() + amount, ac.order_data.order_serial_list.Last(), "update order amount");
                    //if ((ac.order_data.getLastOrderSide() == "buy" && MarketData.Close[i] > ac.order_data.getLastOrderPrice()) || (ac.order_data.getLastOrderSide() == "sell" && MarketData.Close[i] < ac.order_data.getLastOrderPrice()))
                    if (ac.order_data.getLastOrderPrice() != MarketData.Close[i])
                        ad.add_action("update price", pred_side, "limit", MarketData.Close[i], ac.order_data.getLastOrderSize(), ac.order_data.order_serial_list.Last(), "update order price");
                }
                else if (pred_side != ac.order_data.getLastOrderSide()) //2.
                {
                    if (ac.order_data.getLastOrderSide() != "")
                        ad.add_action("cancel", "", "", 0, 0, ac.order_data.order_serial_list.Last(), "cancel all order");
                    if ((pred_side == ac.holding_data.holding_side && ac.holding_data.holding_size + amount > max_amount) == false)
                        ad.add_action("entry", pred_side, "limit", MarketData.Close[i], amount, -1, "entry order");
                }
                else if (pred_side == ac.holding_data.holding_side && ac.holding_data.holding_size + ac.order_data.getLastOrderSize() < max_amount) //3.
                    ad.add_action("entry", pred_side, "limit", MarketData.Close[i], amount, -1, "entry order");
                else if (pred_side != ac.holding_data.holding_side && ac.order_data.getLastOrderSide() != pred_side) //4.
                    ad.add_action("entry", pred_side, "limit", MarketData.Close[i], Math.Min(ac.holding_data.holding_size + amount, ac.holding_data.holding_size + max_amount), -1, "entry order");
            }
            return ad;
        }
    }
}
