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
            if (size > 1)
                Console.WriteLine("sise > 1 !");
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



        /*
        1. No / Cancel (Noでorderある場合はupdate price）
        2. New Entry
        3. Update Price
        4. Additional Entry
        5. Exit (もし既存のadditional orderがあったらまずはそれをキャンセル）
        6. Opposite Order Cancel （cancelした上でholding sizeがmax amount以下だったらpred sideのorderを出す）
        7. Others1 (既にmax amountのholdingがあり、pred side=holding sideで何もしなくて良い場合）
        8. Others2 (holding side== pred sideで既にpred sideのorderが存在しており、update priceも不要な場合）
        9. Others3 (holding side != predで既にexit orderが存在しており、update priceも不要な場合)
        10.Others4 (holding side == pred sideでorderもない場合）
         */
        /*
         * ・simだと本来約定できない状況でもlimit order executedとして処理されることがある。
         * 例えば、priceは10000.5/10000で動かない場合でもclose priceは10000.5 / 10000で変動する、sell@10000.5のorderが約定しないのでclose priceにupdateするとsell@10000となる。
         * すると次のohlcは価格が動かなくてもhigh=10000.5となるのでlimit sell orderが10000で約定したことになり、0.00025%のmaker fee分の利益が出たようになってしまう。 
         * ->entry / price updateはcloseよりも0.5だけ有利な方向にする（i.e. buy orderのときclose=10000でも10000.5にentryするので最低でもbuy/sellに合ったbid_ask値でのentryとなる）
         */
        public StrategyActionData GALimitStrategy2(int i, int nn_output, int amount, int max_amount, SimAccount ac)
        {
            var ad = new StrategyActionData();
            var output_action_list = new string[] { "no", "buy", "sell", "cancel" };
            var pred_side = output_action_list[nn_output];
            var buy_entry_price = MarketData.Close[i] - 0.5;
            var sell_entry_price = MarketData.Close[i] + 0.5;
            var update_price_kijun = 3;


            //check invalid situation
            if (ac.holding_data.holding_size > max_amount)
                Console.WriteLine("Sim Strategy: Holding size is larger than max_amount !");

            //1. No / Cancel           
            if (pred_side == "no")
            {
                if (ac.order_data.getLastOrderSide() != "" && Math.Abs(ac.order_data.getLastOrderPrice() - (ac.order_data.getLastOrderSide() == "buy" ? buy_entry_price : sell_entry_price)) > update_price_kijun)
                    ad.add_action("update price", pred_side, "limit", pred_side=="buy" ? buy_entry_price : sell_entry_price, -1, ac.order_data.getLastSerialNum(), "1. No: update order price");
            }
            else if (pred_side == "cancel")
            {
                if (ac.order_data.getLastOrderSide() != "")
                    ad.add_action("cancel", "", "", 0, 0, ac.order_data.getLastSerialNum(), "1. Cancel: cancel all order");
            }
            else
            {
                //2. New Entry
                if (ac.holding_data.holding_side == "" && pred_side != ac.order_data.getLastOrderSide() && ac.order_data.getLastOrderSide() == "")
                {
                    ad.add_action("entry", pred_side, "limit", pred_side == "buy" ? buy_entry_price : sell_entry_price, amount, -1, "2. New Entry");
                }
                //3.Update Price
                else if (ac.order_data.getLastOrderSide() == pred_side && Math.Abs(ac.order_data.getLastOrderPrice() - (ac.order_data.getLastOrderSide() == "buy" ? buy_entry_price : sell_entry_price)) > update_price_kijun)
                {
                    ad.add_action("update price", "", "limit", pred_side == "buy" ? buy_entry_price : sell_entry_price, -1, ac.order_data.getLastSerialNum(), "3. update order price");
                }
                //4. Additional Entry (pred = holding sideで現在orderなく、holding sizeにamount加えてもmax_amount以下の時に追加注文）
                else if(ac.holding_data.holding_side == pred_side && ac.holding_data.holding_size + amount <= max_amount && ac.order_data.getLastOrderSide() == "")
                {
                    ad.add_action("entry", pred_side, "limit", pred_side == "buy" ? buy_entry_price : sell_entry_price, amount, -1, "4. Additional Entry");
                }
                //5. Exit (holding side != predでかつpred sideのorderがない時にexit orderを出す）
                else if((ac.holding_data.holding_side != pred_side && ac.holding_data.holding_side != "") && (pred_side != ac.order_data.getLastOrderSide()))
                {
                    //もし既存のadditional orderがあったらまずはそれをキャンセル）
                    if (ac.order_data.getLastOrderSide() != "")
                        ad.add_action("cancel", "", "", 0, 0, ac.order_data.getLastSerialNum(), "5. cancel all order");
                    ad.add_action("entry", pred_side, "limit", pred_side == "buy" ? buy_entry_price : sell_entry_price, ac.holding_data.holding_size, -1, "5. Exit Entry");
                }
                //6. Opposite Order Cancel
                else if (pred_side != ac.order_data.getLastOrderSide() && ac.order_data.getLastOrderSide() != "")
                {
                    ad.add_action("cancel", "", "", 0, 0, ac.order_data.getLastSerialNum(), "6. cancel all order");
                    if (ac.holding_data.holding_size + amount <= max_amount)
                        ad.add_action("entry", pred_side, "limit", pred_side == "buy" ? buy_entry_price : sell_entry_price, amount, -1, "6. Opposite Entry");
                    if (ac.holding_data.holding_side != "" && ac.holding_data.holding_side != pred_side)
                        Console.WriteLine("Strategy: Opposite holding exist while cancelling opposite order !");
                }
                else
                {
                    //7. Others1 (既にmax amountのholdingがあり、pred side=holding sideで何もしなくて良い場合）
                    if (ac.holding_data.holding_size >= max_amount && ac.holding_data.holding_side == pred_side)
                    {
                    }
                    //8.Others2(holding side == pred sideで既にpred sideのorderが存在しており、その価格の更新が不要な場合）
                    else if (ac.holding_data.holding_side == pred_side && ac.order_data.getLastOrderSide() == pred_side && Math.Abs(ac.order_data.getLastOrderPrice() - (ac.order_data.getLastOrderSide() == "buy" ? buy_entry_price : sell_entry_price)) <= update_price_kijun)
                    {
                    }
                    //9. Others3 (holding side != predで既にexit orderが存在しており、update priceも不要な場合)
                    else if (ac.holding_data.holding_side != pred_side && ac.order_data.getLastOrderSide() == pred_side && Math.Abs(ac.order_data.getLastOrderPrice() - (ac.order_data.getLastOrderSide() == "buy" ? buy_entry_price : sell_entry_price)) <= update_price_kijun)
                    {

                    }
                    //10.Others4(holding side == pred sideでorderもない場合）
                    else if (ac.holding_data.holding_side == pred_side && ac.order_data.getLastOrderSide() =="")
                    {

                    }
                    else
                    {
                        Console.WriteLine("Strategy - Unknown Situation !");
                    }
                }
            }
            return ad;
        }
    }
}
