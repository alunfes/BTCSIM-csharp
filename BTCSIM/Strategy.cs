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
        4. pred_side != holding_sideのときは、amount + holding_sizeのlimit orderを出す*/
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
                if (pred_side == ac.order_data.getLastOrderSide() && ac.holding_data.holding_size + ac.order_data.getLastOrderSize() < max_amount) //1.
                {
                    ad.add_action("update amount", pred_side, "limit", 0, ac.order_data.getLastOrderSize() + amount, ac.order_data.order_serial_list.Last(), "update order amount");
                    ad.add_action("update price", pred_side, "limit", MarketData.Close[i], ac.order_data.getLastOrderSize() + amount, ac.order_data.order_serial_list.Last(), "update order price");
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


        /*
        public StrategyActionData GAStrategyLimit(Gene chromo, int i, int amount, SimAccount ac)
        {
            var ad = new StrategyActionData();
            var pred_side = chromo.position_gene[i] == 1 ? "buy" : "sell";
            if (pred_side == "buy" && ac.holding_data.holding_side == "" && ac.order_data.order_side.Keys.Count == 0)
                ad.add_action("entry", pred_side, "limit",  MarketData.Close[i], amount, -1, "entry order");
            else if (pred_side == "buy" && ac.holding_data.holding_side == "sell" && ac.order_data.order_side.Keys.Count == 0)
                ad.add_action("entry", pred_side, "limit", MarketData.Close[i], ac.holding_data.holding_size + amount, -1, "exit & entry order");
            else if (pred_side == "sell" && ac.holding_data.holding_side == "" && ac.order_data.order_side.Keys.Count == 0)
                ad.add_action("entry", pred_side, "limit", MarketData.Close[i], amount, -1, "entry order");
            else if (pred_side == "sell" && ac.holding_data.holding_side == "buy" && ac.order_data.order_side.Keys.Count == 0)
                ad.add_action("entry", pred_side, "limit", MarketData.Close[i], ac.holding_data.holding_size + amount, -1, "exit & entry order");
            else if (pred_side == "sell" && ac.holding_data.holding_side == "buy" && ac.order_data.order_side.Keys.Count == 0)
                ad.add_action("entry", pred_side, "limit", MarketData.Close[i], ac.holding_data.holding_size + amount, -1, "exit & entry order");
            else if (ac.holding_data.holding_side != "" && pred_side != ac.holding_data.holding_side && ac.order_data.order_side.Values.ToArray()[0] == pred_side)
                ad.add_action("update", "", "", MarketData.Close[i], 0, ac.order_data.order_side.Keys.ToArray()[0], "");
            else if (ac.holding_data.holding_side != "" && pred_side != ac.holding_data.holding_side && ac.order_data.order_side.Keys.Count > 0) //orderが約定しないうちに逆方向のpredになった場合->cancel order and exit and reentry
            {
                ad.add_action("cancel", "", "", 0, 0, -1, "");
                ad.add_action("entry", pred_side, "limit", MarketData.Close[i], ac.holding_data.holding_size + amount, -1, "exit & entry order");
            }
            else if (ac.order_data.order_side.Values.Count > 0)
                ad.add_action("update", "", "", MarketData.Close[i], 0, ac.order_data.order_side.Keys.ToArray()[0], "");
            return ad;
        }*/

        /*Entry with Limit order only when buy/sell pred continue for num_conti*/
        /*
        public StrategyActionData GAStrategyLimitContiEntry(int from, Gene chromo, int i, int amount, SimAccount ac, int num_conti)
        {
            var ad = new StrategyActionData();
            var pred_buy = true;
            var pred_sell = true;
            for(int j=0; j<num_conti; j++)
            {
                if (chromo.position_gene[i - j] == 1)
                    pred_sell = false;
                else if (chromo.position_gene[i - j] == 2)
                    pred_buy = false;
            }
            var pred_side = "";
            if (pred_buy)
                pred_side = "buy";
            else if (pred_sell)
                pred_side = "sell";
            else
                pred_side = ac.holding_data.holding_side;

            if (pred_side == "buy" && ac.holding_data.holding_side == "" && ac.order_data.order_side.Keys.Count == 0)
                ad.add_action("entry", pred_side, "limit", MarketData.Close[i], amount, -1, "entry order");
            else if (pred_side == "buy" && ac.holding_data.holding_side == "sell" && ac.order_data.order_side.Keys.Count == 0)
                ad.add_action("entry", pred_side, "limit", MarketData.Close[i], ac.holding_data.holding_size + amount, -1, "exit & entry order");
            else if (pred_side == "sell" && ac.holding_data.holding_side == "" && ac.order_data.order_side.Keys.Count == 0)
                ad.add_action("entry", pred_side, "limit", MarketData.Close[i], amount, -1, "entry order");
            else if (pred_side == "sell" && ac.holding_data.holding_side == "buy" && ac.order_data.order_side.Keys.Count == 0)
                ad.add_action("entry", pred_side, "limit", MarketData.Close[i], ac.holding_data.holding_size + amount, -1, "exit & entry order");
            else if (pred_side == "sell" && ac.holding_data.holding_side == "buy" && ac.order_data.order_side.Keys.Count == 0)
                ad.add_action("entry", pred_side, "limit", MarketData.Close[i], ac.holding_data.holding_size + amount, -1, "exit & entry order");
            else if (ac.holding_data.holding_side != "" && pred_side != ac.holding_data.holding_side && ac.order_data.order_side.Values.ToArray()[0] == pred_side)
                ad.add_action("update", "", "", MarketData.Close[i], 0, ac.order_data.order_side.Keys.ToArray()[0], "");
            else if (ac.holding_data.holding_side != "" && pred_side != ac.holding_data.holding_side && ac.order_data.order_side.Keys.Count > 0) //orderが約定しないうちに逆方向のpredになった場合->cancel order and exit and reentry
            {
                ad.add_action("cancel", "", "", 0, 0, -1, "");
                ad.add_action("entry", pred_side, "limit", MarketData.Close[i], ac.holding_data.holding_size + amount, -1, "exit & entry order");
            }
            else if (ac.order_data.order_side.Values.Count > 0)
                ad.add_action("update", "", "", MarketData.Close[i], 0, ac.order_data.order_side.Keys.ToArray()[0], "");
            return ad;
        }*/





        /*entryとpt orderを同じタイミングで出すので、pt orderが先に約定することを想定していない。（pt / lcは1.0に設定すべき）
         * orderは常に1を想定（order / ptを一つと考えた時）
         */
        public StrategyActionData sma_trendfollow(int i, int trendfollow_key, double buy_incre_kijun, double sell_incre_kijun, double amount, double pt_ratio, double lc_ratio, SimAccount ac)
        {
            var ad = new StrategyActionData();
            var pred_side = "no";
            if (MarketData.Trendfollow[trendfollow_key][i] >= buy_incre_kijun){pred_side = "buy";}
            else if(MarketData.Trendfollow[trendfollow_key][i] <= sell_incre_kijun) { pred_side = "sell"; }
            var pt_price = pred_side == "buy" ? Convert.ToInt64(Math.Round(MarketData.Close[i] * (1.0+pt_ratio))) : Convert.ToInt64(Math.Round(MarketData.Close[i] * (1.0 - pt_ratio)));
            var lc_price = pred_side == "buy" ? Convert.ToInt64(Math.Round(ac.holding_data.holding_price * (1.0 - lc_ratio))) : Convert.ToInt64(Math.Round(ac.holding_data.holding_price * (1.0 + lc_ratio)));

            if (pred_side == "buy" || pred_side == "sell")
            {
                if (ac.holding_data.holding_side == "" && ac.order_data.order_serial_list.Count == 0)
                {
                    ad.add_action("entry", pred_side, "limit", MarketData.Close[i], amount, -1, "entry"); //new entry
                    ad.add_action("entry", pred_side=="sell" ? "buy" : "sell", "limit", pt_price, amount, -1, "pt"); //pt order
                }
                else if (ac.holding_data.holding_side != "" && ac.holding_data.holding_side != pred_side)
                {
                    ad.add_action("cancel", "", "", 0, 0, -1, "");
                    ad.add_action("entry", pred_side, "limit", MarketData.Close[i], ac.holding_data.holding_size + amount, -1, "exit&entry"); //opposite entry
                    ad.add_action("entry", pred_side == "sell" ? "buy" : "sell", "limit", pt_price, amount, -1, "pt"); //pt order
                }
                else if (ac.holding_data.holding_side == "" && ac.order_data.order_serial_list.Count > 0)//order / pt_orderをそれぞれ現在のcloseに合わせてupdate, orderは2つだけの状態を想定
                {
                    var serial_list = ac.order_data.order_serial_list.ToArray();
                    foreach(int s in serial_list)
                    {
                        if (ac.order_data.order_message[s].Contains("entry") && ac.order_data.order_cancel[s] != true)
                        {
                            ad.add_action("update", "", "", MarketData.Close[i], 0, s, "");
                        }
                        else if (ac.order_data.order_message[s].Contains("pt") && ac.order_data.order_cancel[s] != true)
                        {
                            var update_pt_price = ac.order_data.order_side[s] == "sell" ? Convert.ToInt64(Math.Round(MarketData.Close[i] * (1.0 - pt_ratio))) : Convert.ToInt64(Math.Round(MarketData.Close[i] * (1.0 + pt_ratio)));
                            ad.add_action("update", "", "", update_pt_price, 0, s, "");
                        }
                    }
                }
                else //check losscut
                {
                    if (ac.holding_data.holding_side != "" && (lc_price >= MarketData.Low[i] && ac.holding_data.holding_side=="buy") || (lc_price <= MarketData.High[i] && ac.holding_data.holding_side == "sell"))
                    {
                        ad.add_action("cancel", "", "", 0, 0, ac.order_data.order_serial_list.Last(), "");
                        ad.add_action("entry", ac.holding_data.holding_side == "buy" ? "sell" : "buy", "market", 0, ac.holding_data.holding_size, -1, "losscut");
                    }
                }
            }
            else //check losscut
            {
                if (ac.holding_data.holding_side != "" && (lc_price >= MarketData.Low[i] && ac.holding_data.holding_side == "buy") || (lc_price <= MarketData.High[i] && ac.holding_data.holding_side == "sell"))
                {
                    ad.add_action("cancel", "", "", 0, 0, ac.order_data.order_serial_list.Last(), "");
                    ad.add_action("entry", ac.holding_data.holding_side == "buy" ? "sell" : "buy", "market", 0, ac.holding_data.holding_size, -1, "losscut");
                }
            }
            return ad;
        }
    }
}
