using System;
namespace BTCSIM
{
    public class Sim
    {
        public Sim()
        {
        }

        public SimAccount sim_trendfollow_limit(int from, int to, int trendfollow_key, double buy_incre_kijun, double sell_incre_kijun, double pt_ratio, double lc_ratio, SimAccount ac)
        {
            if (pt_ratio > 0 && pt_ratio < 1.0 && lc_ratio > 0 && lc_ratio < 1.0)
            {
                var strategy = new Strategy();
                for (int i = from; i < to; i++)
                {
                    var actions = strategy.sma_trendfollow(i, trendfollow_key, buy_incre_kijun, sell_incre_kijun, 1, pt_ratio, lc_ratio, ac);
                    for (int j = 0; j < actions.action.Count; j++)
                    {
                        if (actions.action[j] == "entry")
                        {
                            ac.entry_order(actions.order_type[j], actions.order_side[j], actions.order_size[j], actions.order_price[j], i, MarketData.Dt[i].ToString(), actions.order_message[j]);
                        }
                        else if (actions.action[j] == "cancel")
                        {
                            ac.cancel_all_order(i, MarketData.Dt[i].ToString());
                        }
                        else if (actions.action[j] == "update")
                        {
                            ac.update_order_price(actions.order_price[j], actions.order_serial_num[j], i, MarketData.Dt[i].ToString());
                        }
                    }
                    ac.move_to_next(i + 1, MarketData.Dt[i + 1].ToString(), MarketData.Open[i + 1], MarketData.High[i + 1], MarketData.Low[i + 1], MarketData.Open[i + 1]);
                }
            }
            else
            {
                Console.WriteLine("Invalid pt_ratio or lc_ratio!");
            }
            return ac;
        }
    }
}
