﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace BTCSIM
{
    public class Sim
    {
        public Sim()
        {
        }

        public SimAccount sim_ga(int from, int to, Gene2 chromo, SimAccount ac)
        {
            var nn = new NN();
            var strategy = new Strategy();
            int amount = 1;
            var nn_input_data_generator = new NNInputDataGenerator();

            for (int i = from; i < to; i++)
            {
                var nn_inputs = nn_input_data_generator.generateNNInputData(ac, i);
                var nn_outputs = nn.calcNN(nn_inputs, chromo.num_units, chromo, 1);
                var pred = nn.getActivatedUnit(nn_outputs);
                var actions = strategy.GAStrategy(pred, amount, ac);
                for (int j = 0; j < actions.action.Count; j++)
                {
                    if (actions.action[j] == "entry")
                        ac.entry_order(actions.order_type[j], actions.order_side[j], actions.order_size[j], actions.order_price[j], i, MarketData.Dt[i].ToString(), actions.order_message[j]);
                }
                ac.move_to_next(i + 1, MarketData.Dt[i + 1].ToString(), MarketData.Open[i + 1], MarketData.High[i + 1], MarketData.Low[i + 1], MarketData.Close[i + 1]);
            }
            ac.calc_sharp_ratio();
            return ac;
        }

        public SimAccount sim_ga_limit(int from, int to, int max_amount, Gene2 chromo, SimAccount ac)
        {
            var nn = new NN();
            var strategy = new Strategy();
            int amount = 1;
            var nn_input_data_generator = new NNInputDataGenerator();

            for (int i = from; i < to; i++)
            {
                var nn_inputs = nn_input_data_generator.generateNNInputDataLimit(ac, i);
                var nn_outputs = nn.calcNN(nn_inputs, chromo.num_units, chromo, 1);
                var pred = nn.getActivatedUnit(nn_outputs);
                var actions = strategy.GALimitStrategy2(i, pred, amount, max_amount, ac);

                //check invalid ac situation
                if (ac.order_data.order_side.Count > 1)
                    Console.WriteLine("Sim: # of order is more than 1 !");

                for (int j = 0; j < actions.action.Count; j++)
                {
                    if (actions.action[j] == "entry")
                        ac.entry_order(actions.order_type[j], actions.order_side[j], actions.order_size[j], actions.order_price[j], i, MarketData.Dt[i].ToString(), actions.order_message[j]);
                    else if (actions.action[j] == "cancel")
                        ac.cancel_all_order(i, MarketData.Dt[i].ToString());
                    else if (actions.action[j] == "update amount")
                        ac.update_order_amount(actions.order_size[j], actions.order_serial_num[j], i, MarketData.Dt[i].ToString());
                    else if (actions.action[j] == "update price")
                        ac.update_order_price(actions.order_price[j], actions.order_serial_num[j], i, MarketData.Dt[i].ToString());
                    else
                        Console.WriteLine("Sim: Unknown strategy action !");
                }
                ac.move_to_next(i + 1, MarketData.Dt[i + 1].ToString(), MarketData.Open[i + 1], MarketData.High[i + 1], MarketData.Low[i + 1], MarketData.Close[i + 1]);
            }
            ac.calc_sharp_ratio();
            return ac;
        }


        public SimAccount sim_ga_market_limit(int from, int to, int max_amount, Gene2 chromo, SimAccount ac)
        {
            var nn = new NN();
            var strategy = new Strategy();
            int amount = 1;
            var nn_input_data_generator = new NNInputDataGenerator();
            var zero_trade_check_point = 0.1; //to - fromの間のx%進捗した時にnum trade=0だったらsimを打ち切る。

            for (int i = from; i < to; i++)
            {
                //check num trade=0 discontinue sim
                if (i - from > ((to - from) * zero_trade_check_point) && ac.performance_data.num_trade == 0)
                {
                    //Console.WriteLine("Stopped sim due to zero trade.");
                    break;
                }

                var nn_inputs = nn_input_data_generator.generateNNInputDataLimit(ac, i);
                var nn_outputs = nn.calcNN(nn_inputs, chromo.num_units, chromo, 1);
                var pred = nn.getActivatedUnitLimitMarket(nn_outputs);
                var actions = strategy.GALimitMarketStrategy(i, pred, amount, max_amount, ac);

                //check invalid ac situation
                if (ac.order_data.order_side.Count > 1)
                    Console.WriteLine("Sim: # of order is more than 1 !");

                for (int j = 0; j < actions.action.Count; j++)
                {
                    if (actions.action[j] == "entry")
                        ac.entry_order(actions.order_type[j], actions.order_side[j], actions.order_size[j], actions.order_price[j], i, MarketData.Dt[i].ToString(), actions.order_message[j]);
                    else if (actions.action[j] == "cancel")
                        ac.cancel_all_order(i, MarketData.Dt[i].ToString());
                    else if (actions.action[j] == "update amount")
                        ac.update_order_amount(actions.order_size[j], actions.order_serial_num[j], i, MarketData.Dt[i].ToString());
                    else if (actions.action[j] == "update price")
                        ac.update_order_price(actions.order_price[j], actions.order_serial_num[j], i, MarketData.Dt[i].ToString());
                    else
                        Console.WriteLine("Sim: Unknown strategy action !");
                }
                ac.move_to_next(i + 1, MarketData.Dt[i + 1].ToString(), MarketData.Open[i + 1], MarketData.High[i + 1], MarketData.Low[i + 1], MarketData.Close[i + 1]);
            }
            ac.calc_sharp_ratio();
            return ac;
        }

        /*
        public SimAccount sim_ga_limit(int from, int to, int amount, Gene chromo, SimAccount ac)
        {
            var strategy = new Strategy();
            for (int i = from; i < to; i++)
            {
                if (ac.holding_data.holding_size > amount)
                    Console.WriteLine("invalid holding size=" + ac.holding_data.holding_size.ToString());
                var actions = strategy.GAStrategyLimit(chromo, i, amount, ac);
                for (int j = 0; j < actions.action.Count; j++)
                {
                    if (actions.action[j] == "entry")
                        ac.entry_order(actions.order_type[j], actions.order_side[j], actions.order_size[j], actions.order_price[j], i, MarketData.Dt[i].ToString(), actions.order_message[j]);
                    else if (actions.action[j] == "cancel")
                        ac.cancel_all_order(i, MarketData.Dt[i].ToString());
                    else if (actions.action[j] == "update")
                        ac.update_order_price(actions.order_price[j], actions.order_serial_num[j], i, MarketData.Dt[i].ToString());
                }
                ac.move_to_next(i + 1, MarketData.Dt[i + 1].ToString(), MarketData.Open[i + 1], MarketData.High[i + 1], MarketData.Low[i + 1], MarketData.Close[i + 1]);
            }
            ac.calc_sharp_ratio();
            return ac;
        }
        public SimAccount sim_ga_limit_conti_entry(int from, int to, int amount, Gene chromo, SimAccount ac, int num_conti)
        {
            var strategy = new Strategy();
            for (int i = from; i < to; i++)
            {
                if (ac.holding_data.holding_size > amount)
                    Console.WriteLine("invalid holding size=" + ac.holding_data.holding_size.ToString());
                var actions = strategy.GAStrategyLimitContiEntry(from, chromo, i, amount, ac, num_conti);
                for (int j = 0; j < actions.action.Count; j++)
                {
                    if (actions.action[j] == "entry")
                        ac.entry_order(actions.order_type[j], actions.order_side[j], actions.order_size[j], actions.order_price[j], i, MarketData.Dt[i].ToString(), actions.order_message[j]);
                    else if (actions.action[j] == "cancel")
                        ac.cancel_all_order(i, MarketData.Dt[i].ToString());
                    else if (actions.action[j] == "update")
                        ac.update_order_price(actions.order_price[j], actions.order_serial_num[j], i, MarketData.Dt[i].ToString());
                }
                ac.move_to_next(i + 1, MarketData.Dt[i + 1].ToString(), MarketData.Open[i + 1], MarketData.High[i + 1], MarketData.Low[i + 1], MarketData.Close[i + 1]);
            }
            ac.calc_sharp_ratio();
            return ac;
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
            ac.calc_sharp_ratio();
            return ac;
        }*/
    }
}