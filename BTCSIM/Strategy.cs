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
        /*
         */
        public StrategyActionData GA(int i)
        {
        }




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
                            var update_pt_price = ac.order_data.order_side[s] == "sell" ? Convert.ToInt64(Math.Round(MarketData.Close[i] * (1.0 + pt_ratio))) : Convert.ToInt64(Math.Round(MarketData.Close[i] * (1.0 - pt_ratio)));
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
