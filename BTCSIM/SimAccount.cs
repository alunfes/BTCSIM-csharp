using System;
using System.Collections.Generic;
using System.Data;
using System.Numerics;
using System.Linq;
using System.IO;

namespace BTCSIM
{
    public class PerformanceData
    {
        public double total_pl { get; set; }
        public double realized_pl { get; set; }
        public double unrealized_pl { get; set; }
        public List<double> unrealized_pl_list { get; set; } //record unrealided pl during holding period for NN input data
        public int num_trade { get; set; }
        public int num_buy { get; set; }
        public int num_sell { get; set; }
        public int num_maker_order { get; set; }
        public int num_win { get; set; }
        public double win_rate { get; set; }
        public double total_fee { get; set; }
        public double sharp_ratio { get; set; }
        
        public PerformanceData()
        {
            total_pl = 0;
            realized_pl = 0;
            unrealized_pl = 0;
            unrealized_pl_list = new List<double>();
            num_trade = 0;
            num_buy = 0;
            num_sell = 0;
            win_rate = 0;
            num_win = 0;
            num_maker_order = 0;
            total_fee = 0;
            sharp_ratio = 0;
        }
    }

    public class OrderData
    {
        public int order_serial_num { get; set; } //active order serial num list
        public List<int> order_serial_list { get; set; } //latest order serial num
        public Dictionary<int, string> order_side { get; set; }
        public Dictionary<int, double> order_size { get; set; }
        public Dictionary<int, double> order_price { get; set; }
        public Dictionary<int, string> order_type { get; set; }
        public Dictionary<int, int> order_i { get; set; }
        public Dictionary<int, string> order_dt { get; set; }
        public Dictionary<int, Boolean> order_cancel { get; set; }
        public Dictionary<int, string> order_message { get; set; } //entry, pt, exit&entry

        public OrderData()
        {
            order_serial_num = -1;
            order_serial_list = new List<int>();
            order_side = new Dictionary<int, string>();
            order_size = new Dictionary<int, double>();
            order_price = new Dictionary<int, double>();
            order_type = new Dictionary<int, string>();
            order_i = new Dictionary<int, int>();
            order_dt = new Dictionary<int, string>();
            order_cancel = new Dictionary<int, bool>();
            order_message = new Dictionary<int, string>();
        }

        public string getLastOrderSide()
        {
            if (order_serial_list.Count > 0)
                return order_side[order_serial_list.Last()];
            else
                return "";
        }
        public double getLastOrderSize()
        {
            if (order_serial_list.Count > 0)
                return order_size[order_serial_list.Last()];
            else
                return 0;
        }
        public double getLastOrderPrice()
        {
            if (order_serial_list.Count > 0)
                return order_price[order_serial_list.Last()];
            else
                return 0;
        }
        public int getNumOrders()
        {
            if (order_serial_list.Count > 0)
                return order_serial_list.Count;
            else
                return 0;
        }
        public int getLastSerialNum()
        {
            if (order_serial_list.Count() > 0)
                return order_serial_list.Last();
            else
                return -1;
        }
    }

    public class HoldingData
    {
        public string holding_side { get; set; }
        public double holding_price { get; set; }
        public double holding_size { get; set; }
        public int holding_i { get; set; }
        public int holding_period { get; set; }

        public HoldingData()
        {
            holding_i = -1;
            holding_period = 0;
            holding_price = 0;
            holding_size = 0;
            holding_side = "";
        }

        public void update_holding(string side, double price, double size, int i)
        {
            holding_side = side;
            holding_price = price;
            holding_size = size;
            holding_i = i;
            holding_period = 0;
        }
    }

    public class LogData
    {
        public DataSet log_data_set { get; set; }
        public DataTable log_data_table { get; set; }   
        public List<double> total_pl_log { get; set; }


        public LogData()
        {
            log_data_set = new DataSet();
            log_data_table = new DataTable("LodData");
            log_data_table.Columns.Add("total_pl", typeof(double));
            log_data_table.Columns.Add("total_fee", typeof(double));
            log_data_table.Columns.Add("dt", typeof(string));
            log_data_table.Columns.Add("i", typeof(Int64));
            log_data_table.Columns.Add("order_side", typeof(string));
            log_data_table.Columns.Add("order_type", typeof(string));
            log_data_table.Columns.Add("order_size", typeof(double));
            log_data_table.Columns.Add("order_price", typeof(double));
            log_data_table.Columns.Add("order_message", typeof(string));
            log_data_table.Columns.Add("holding_side", typeof(string));
            log_data_table.Columns.Add("holding_price", typeof(double));
            log_data_table.Columns.Add("holding_size", typeof(double));
            log_data_table.Columns.Add("action", typeof(string));
            log_data_set.Tables.Add(log_data_table);
            total_pl_log = new List<double>();
        }

        public void add_log_data(int i, string dt, string action, HoldingData hd, OrderData od, PerformanceData pd)
        {
            total_pl_log.Add(pd.total_pl);
            if (od.order_serial_list.Count > 0)
            {
                log_data_table.Rows.Add(pd.total_pl, pd.total_fee, dt, i, od.order_side[od.order_serial_list.Last()], od.order_type[od.order_serial_list.Last()],
                    od.order_size[od.order_serial_list.Last()], od.order_price[od.order_serial_list.Last()], od.order_message[od.order_serial_list.Last()], hd.holding_side, hd.holding_price, hd.holding_size,
                    action);
            }
            else
            {
                log_data_table.Rows.Add(pd.total_pl, pd.total_fee, dt, i, "", "", 0, 0, "", hd.holding_side, hd.holding_price, hd.holding_size, action);
            }
        }
    }

    /*
     * iのohlcで判断して、i+1のopenでentry、i+1のohlcで約定判定
     * （キャンセルは即時反映、updateも即時反映）
     * 
     * conti simなどでもiは継続した値を使用しないといけない。
     */
    public class SimAccount
    {
        public const double taker_fee = 0.00075;
        public const double maker_fee = -0.00025;

        public int start_ind = 0;
        public int end_ind = 0;

        public PerformanceData performance_data;
        public OrderData order_data;
        public HoldingData holding_data;
        public LogData log_data;

        public SimAccount()
        {
            log_data = new LogData();
            performance_data = new PerformanceData();
            order_data = new OrderData();
            holding_data = new HoldingData();
        }

        /*should be called after all sim calc*/
        public void calc_sharp_ratio()
        {
            List<double> change = new List<double>();
            for (int i=1; i<log_data.total_pl_log.Count; i++)
            {
                if (log_data.total_pl_log[i - 1] != 0)
                    change.Add((log_data.total_pl_log[i] - log_data.total_pl_log[i - 1]) / log_data.total_pl_log[i - 1]);
                else
                    change.Add(0);
            }


            var doubleList = change.Select(a => Convert.ToDouble(a)).ToArray();

            //平均値算出
            double mean = doubleList.Average();
            //自乗和算出
            double sum2 = doubleList.Select(a => a * a).Sum();
            //分散 = 自乗和 / 要素数 - 平均値^2
            double variance = sum2 / Convert.ToDouble(doubleList.Length) - mean * mean;
            //標準偏差 = 分散の平方根
            var stdv = Math.Sqrt(variance);

            if (stdv != 0)
                performance_data.sharp_ratio = performance_data.total_pl / stdv;
            else
                performance_data.sharp_ratio = 0;
        }


        public void move_to_next(int i, string dt, double open, double high, double low, double close)
        {
            if (start_ind <= 0)
                start_ind = i;
            end_ind = i;
            check_cancel(i, dt);
            check_execution(i, dt, open, high, low);
            holding_data.holding_period = holding_data.holding_i > 0 ? i - holding_data.holding_i : 0;
            if (holding_data.holding_side != "")
            {
                //performance_data.unrealized_pl = holding_data.holding_side == "buy" ? (close - holding_data.holding_price) / holding_data.holding_price * holding_data.holding_size : (holding_data.holding_price - close) / holding_data.holding_price * holding_data.holding_size;
                performance_data.unrealized_pl = holding_data.holding_side == "buy" ? (close - holding_data.holding_price) * holding_data.holding_size : (holding_data.holding_price - close) * holding_data.holding_size;
                performance_data.unrealized_pl_list.Add(performance_data.unrealized_pl);
            }
            else
            {
                performance_data.unrealized_pl = 0;
                performance_data.unrealized_pl_list = new List<double>();
            }
            performance_data.total_pl = performance_data.realized_pl + performance_data.unrealized_pl - performance_data.total_fee;
            if (performance_data.num_trade > 0)
                performance_data.win_rate = Math.Round(Convert.ToDouble(performance_data.num_win) / Convert.ToDouble(performance_data.num_trade), 4);
            log_data.add_log_data(i, dt, "move to next", holding_data, order_data, performance_data);
        }

        public void entry_order(string type, string side, double size, double price, int i, string dt, string message)
        {
            if (size > 0 && (side =="buy" || side=="sell"))
            {
                order_data.order_serial_num++;
                order_data.order_serial_list.Add(order_data.order_serial_num);
                order_data.order_type[order_data.order_serial_num] = type;
                order_data.order_side[order_data.order_serial_num] = side;
                order_data.order_size[order_data.order_serial_num] = size;
                order_data.order_price[order_data.order_serial_num] = price;
                order_data.order_i[order_data.order_serial_num] = i;
                order_data.order_dt[order_data.order_serial_num] = dt;
                order_data.order_cancel[order_data.order_serial_num] = false;
                order_data.order_message[order_data.order_serial_num] = message;
                log_data.add_log_data(i, dt, "entry order " + side + "-" + type, holding_data, order_data, performance_data);
            }
            else
            {
                if (size <= 0)
                {
                    Console.WriteLine("entry order failed due to order size = 0 !");
                    log_data.add_log_data(i, dt, "entry order failed due to order size = 0 !", holding_data, order_data, performance_data);
                }
                else
                {
                    Console.WriteLine("entry order failed due to order side is "+side+ " !");
                    log_data.add_log_data(i, dt, "entry order failed due to order side is " + side + " !", holding_data, order_data, performance_data);
                }
            }
        }

        public void update_order_price(double update_price, int order_serial_num, int i, string dt)
        {
            if (update_price > 0 && order_data.order_serial_list.Contains(order_serial_num))
            {
                order_data.order_price[order_serial_num] = update_price;
                //order_data.order_i[order_serial_num] = i;
                //order_data.order_message[order_serial_num] = "updated-" + order_data.order_message[order_serial_num];
                log_data.add_log_data(i, dt, "update order price", holding_data, order_data, performance_data);
            }
            else
            {
                Console.WriteLine("invalid update price or order_serial_num in update_order_price !");
            }
        }

        public void update_order_amount(double update_amount, int order_serial_num, int i, string dt)
        {
            if (update_amount > 0 && order_data.order_serial_list.Contains(order_serial_num))
            {
                order_data.order_size[order_serial_num] = update_amount;
                log_data.add_log_data(i, dt, "update order amount", holding_data, order_data, performance_data);
            }
            else
            {
                Console.WriteLine("invalid update amount or order_serial_num in update_order_amount !");
            }

        }

        private void del_order(int order_serial_num, int i)
        {
            if (order_data.order_serial_list.Contains(order_serial_num))
            {
                order_data.order_serial_list.Remove(order_serial_num);
                order_data.order_side.Remove(order_serial_num);
                order_data.order_type.Remove(order_serial_num);
                order_data.order_size.Remove(order_serial_num);
                order_data.order_price.Remove(order_serial_num);
                order_data.order_i.Remove(order_serial_num);
                order_data.order_dt.Remove(order_serial_num);
                order_data.order_cancel.Remove(order_serial_num);
                order_data.order_message.Remove(order_serial_num);
            }
        }

        public void cancel_order(int order_serial_num, int i, string dt)
        {
            if (order_data.order_serial_list.Contains(order_serial_num))
            {
                if (order_data.order_cancel[order_serial_num] != true)
                {
                    order_data.order_cancel[order_serial_num] = true;   
                    //order_data.order_i[order_serial_num] = i;
                }
                else
                {
                    Console.WriteLine("Cancel Failed!");
                }
            }
        }

        public void cancel_all_order(int i, string dt)
        {
            for (int s=0; s<order_data.order_serial_list.Count; s++) { cancel_order(order_data.order_serial_list[s], i, dt); }
        }


        public void exit_all(int i, string dt)
        {
            if (holding_data.holding_size > 0)
                entry_order("market", holding_data.holding_side == "sell" ? "buy" : "sell", holding_data.holding_size, 0, i, dt, "exit all");
        }

        private void calc_fee(double size, double price, string maker_taker)
        {
            if (maker_taker == "maker")
            {
                performance_data.total_fee += price * size * maker_fee;
            }
            else if (maker_taker == "taker")
            {
                performance_data.total_fee += price * size * taker_fee;
            }
            else
            {
                Console.WriteLine("Invalid maker_taker type in calc_fee ! " + maker_taker);
            }
        }

        private void check_cancel(int i, string dt)
        {
            var serial_list = order_data.order_serial_list.ToArray();
            foreach (int s in serial_list)
            {
                //if (order_data.order_cancel[s] == true && order_data.order_i[s] < i)
                if (order_data.order_cancel[s] == true)
                {
                    del_order(s, i);
                    log_data.add_log_data(i, dt, "cancelled", holding_data, order_data, performance_data);
                }
            }
        }

        private void check_execution(int i, string dt, double open, double high, double low)
        {
            var serial_list = order_data.order_serial_list.ToArray();
            foreach (int s in serial_list)
            {
                if (order_data.order_type[s] == "market") //market orderのときはi関係なくclose valで約定
                {
                    performance_data.num_maker_order++;
                    process_execution(open, s, i, dt);
                    del_order(s, i);
                }
                else if (order_data.order_i[s] < i)
                {
                    if (order_data.order_type[s] == "limit" && ((order_data.order_side[s] == "buy" && order_data.order_price[s] > low+0.5) || (order_data.order_side[s] == "sell" && order_data.order_price[s] < high-0.5)))
                    {
                        process_execution(order_data.order_price[s], s, i, dt);
                        del_order(s, i);
                    }
                }
            }
        }

        private void process_execution(double exec_price, int order_serial_num, int i, string dt)
        {
            calc_fee(order_data.order_size[order_serial_num], exec_price, order_data.order_type[order_serial_num] == "limit" ? "maker" : "taker");
            if (holding_data.holding_side == "")
            {
                holding_data.update_holding(order_data.order_side[order_serial_num], exec_price, order_data.order_size[order_serial_num], i);
                log_data.add_log_data(i, dt, "New Entry:" + order_data.order_type[order_serial_num], holding_data, order_data, performance_data);
            }
            else if (holding_data.holding_side == order_data.order_side[order_serial_num])
            {
                var ave_price = Math.Round(((holding_data.holding_price * holding_data.holding_size) + (exec_price * order_data.order_size[order_serial_num])) / (order_data.order_size[order_serial_num] + holding_data.holding_size), 1);
                holding_data.update_holding(holding_data.holding_side, ave_price, order_data.order_size[order_serial_num] + holding_data.holding_size, i);
                log_data.add_log_data(i, dt, "Additional Entry.", holding_data, order_data, performance_data);
            }
            else if (holding_data.holding_size > order_data.order_size[order_serial_num])
            {
                calc_executed_pl(exec_price, order_data.order_size[order_serial_num], i);
                holding_data.update_holding(holding_data.holding_side, holding_data.holding_price, holding_data.holding_size - order_data.order_size[order_serial_num], i);
                log_data.add_log_data(i, dt, "Exit Order (h>o)", holding_data, order_data, performance_data);
            }
            else if (holding_data.holding_size == order_data.order_size[order_serial_num])
            {
                calc_executed_pl(exec_price, order_data.order_size[order_serial_num], i);
                //initialize_holding_data();
                holding_data = new HoldingData();
                log_data.add_log_data(i, dt, "Exit Order (h=o)", holding_data, order_data, performance_data);
            }
            else if (holding_data.holding_size < order_data.order_size[order_serial_num])
            {
                calc_executed_pl(exec_price, holding_data.holding_size, i);
                holding_data.update_holding(order_data.order_side[order_serial_num], exec_price, order_data.order_size[order_serial_num] - holding_data.holding_size, i);
                log_data.add_log_data(i, dt, "'Exit & Entry Order (h<o)", holding_data, order_data, performance_data);
            }
            else
            {
                Console.WriteLine("Unknown situation in process execution !");
            }
        }

        private void calc_executed_pl(double exec_price, double size, int i)
        {
            //var pl = holding_data.holding_side == "buy" ? (exec_price - holding_data.holding_price) / holding_data.holding_price * size : (holding_data.holding_price - exec_price) / holding_data.holding_price * size;
            var pl = holding_data.holding_side == "buy" ? (exec_price - holding_data.holding_price) * size : (holding_data.holding_price - exec_price) * size;
            performance_data.realized_pl += Math.Round(pl, 6);
            performance_data.num_trade++;
            if (pl > 0) { performance_data.num_win++; }
        }


        
    }
}
