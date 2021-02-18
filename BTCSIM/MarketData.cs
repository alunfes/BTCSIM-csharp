using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Linq;

namespace BTCSIM
{
    static public class MarketData
    {
        static private List<double> unix_time;
        static private List<DateTime> dt;
        static private List<double> open;
        static private List<double> high;
        static private List<double> low;
        static private List<double> close;
        static private List<double> size;
        static private List<double> bid;
        static private List<double> ask;
        static private List<double> buy_vol;
        static private List<double> sell_vol;
        static public List<int> terms;
        static private Dictionary<int, List<double>> sma;
        static private Dictionary<int, List<double>> divergence;
        static private Dictionary<int, List<double>> trendfollow;
        static private Dictionary<int, List<double>> divergence_minmax_scale; //i, scaled data for all terms
        static private Dictionary<int, List<double>> vola_kyori;
        static private Dictionary<int, List<double>> vola_kyori_minmax_scale; //i, scaled data for all terms
        static private Dictionary<int, List<double>> vol_ma_divergence;
        static private Dictionary<int, List<double>> vol_ma_divergence_minmax_scale;
        static private Dictionary<int, List<double>> buysell_vol_ratio;
        static private Dictionary<int, List<double>> buysell_vol_ratio_minmax_scale;
        static private Dictionary<int, List<double>> buysellvol_price_ratio;
        static private Dictionary<int, List<double>> rsi;
        static private Dictionary<int, List<double>> rsi_scale;
        static private Dictionary<int, List<double>> uwahige;
        static private Dictionary<int, List<double>> shitahige;
        static private Dictionary<int, List<double>> uwahige_scale;
        static private Dictionary<int, List<double>> shitahige_scale;

        static public ref List<double> UnixTime
        {
            get { return ref unix_time; }
        }
        static public ref List<DateTime> Dt
        {
            get { return ref dt; }
        }
        static public ref List<double> Open
        {
            get { return ref open; }
        }
        static public ref List<double> High
        {
            get { return ref high; }
        }
        static public ref List<double> Low
        {
            get { return ref low; }
        }
        static public ref List<double> Close
        {
            get { return ref close; }
        }
        static public ref List<double> Size
        {
            get { return ref size; }
        }
        static public ref List<double> Bid
        {
            get { return ref bid; }
        }
        static public ref List<double> Ask
        {
            get { return ref ask; }
        }
        static public ref List<double> Buyvol
        {
            get { return ref buy_vol; }
        }
        static public ref List<double> Sellvol
        {
            get { return ref sell_vol; }
        }
        static public ref Dictionary<int, List<double>> Sma
        {
            get { return ref sma; }
        }
        static public ref Dictionary<int, List<double>> Divergence
        {
            get { return ref divergence; }
        }
        static public ref Dictionary<int, List<double>> Divergence_minmax_scale
        {
            get { return ref divergence_minmax_scale; }
        }
        static public ref Dictionary<int, List<double>> Trendfollow
        {
            get { return ref trendfollow; }
        }
        static public ref Dictionary<int, List<double>> Volakyori_minmax_scale
        {
            get { return ref vola_kyori_minmax_scale; }
        }
        static public ref Dictionary<int, List<double>> Vol_ma_divergence_minmax_scale
        {
            get { return ref vol_ma_divergence_minmax_scale; }
        }
        static public ref Dictionary<int, List<double>> Buysell_vol_ratio
        {
            get { return ref buysell_vol_ratio; }
        }
        static public ref Dictionary<int, List<double>> Buysell_vol_ratio_minmax_scale
        {
            get { return ref buysell_vol_ratio_minmax_scale; }
        }
        static public ref Dictionary<int, List<double>> Buysellvol_price_ratio
        {
            get { return ref buysellvol_price_ratio; }
        }
        static public ref Dictionary<int, List<double>> Rsi
        {
            get { return ref rsi; }
        }
        static public ref Dictionary<int, List<double>> Rsi_scale
        {
            get { return ref rsi_scale; }
        }
        static public ref Dictionary<int, List<double>> Uwahige
        {
            get { return ref uwahige; }
        }
        static public ref Dictionary<int, List<double>> Shitahige
        {
            get { return ref shitahige; }
        }
        static public ref Dictionary<int, List<double>> Uwahige_scale
        {
            get { return ref uwahige_scale; }
        }
        static public ref Dictionary<int, List<double>> Shitahige_scale
        {
            get { return ref shitahige_scale; }
        }

        static public void initializer(List<int> terms_list)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            unix_time = new List<double>();
            dt = new List<DateTime>();
            open = new List<double>();
            high = new List<double>();
            low = new List<double>();
            close = new List<double>();
            size = new List<double>();
            bid = new List<double>();
            ask = new List<double>();
            buy_vol = new List<double>();
            sell_vol = new List<double>();
            terms = new List<int>();
            sma = new Dictionary<int, List<double>>();
            divergence = new Dictionary<int, List<double>>();
            divergence_minmax_scale = new Dictionary<int, List<double>>();
            vola_kyori = new Dictionary<int, List<double>>();
            trendfollow = new Dictionary<int, List<double>>();
            vola_kyori_minmax_scale = new Dictionary<int, List<double>>();
            vol_ma_divergence = new Dictionary<int, List<double>>();
            vol_ma_divergence_minmax_scale = new Dictionary<int, List<double>>(); //pythonと若干の乖離あり
            buysell_vol_ratio = new Dictionary<int, List<double>>();
            buysell_vol_ratio_minmax_scale = new Dictionary<int, List<double>>();
            buysellvol_price_ratio = new Dictionary<int, List<double>>();
            rsi = new Dictionary<int, List<double>>();
            rsi_scale = new Dictionary<int, List<double>>();
            uwahige = new Dictionary<int, List<double>>();
            shitahige = new Dictionary<int, List<double>>();
            read_data();
            calc_index(terms_list);
            
            stopWatch.Stop();
            Console.WriteLine("Completed initialize MarketData. " + stopWatch.Elapsed.Seconds.ToString() + " seconds for " + close.Count.ToString() + " data.");
        }

        static private void read_data()
        {
            var d = Directory.GetFiles(@"./Data");
            StreamReader sr = new StreamReader(@"./Data/onemin_bybit.csv");
            sr.ReadLine();
            while (!sr.EndOfStream)
            {
                var line = sr.ReadLine();
                var data = line.Split(',');
                //unix_time.Add(Convert.ToDouble(data[6]));
                dt.Add(Convert.ToDateTime(data[0]));
                open.Add(Convert.ToDouble(data[1]));
                high.Add(Convert.ToDouble(data[2]));
                low.Add(Convert.ToDouble(data[3]));
                close.Add(Convert.ToDouble(data[4]));
                size.Add(Convert.ToDouble(data[5]));
                bid.Add(Convert.ToDouble(data[6]));
                ask.Add(Convert.ToDouble(data[7]));
                buy_vol.Add(Convert.ToDouble(data[8]));
                sell_vol.Add(Convert.ToDouble(data[9]));
            }
            if (size.Contains(Double.NaN))
                Console.WriteLine("MarketData-read_data: Nan in size !");
            if (buy_vol.Contains(Double.NaN))
                Console.WriteLine("MarketData-read_data: Nan in buy_vol !");
            if (sell_vol.Contains(Double.NaN))
                Console.WriteLine("MarketData-read_data: Nan in sell_vol !");
            Console.WriteLine("Completed read data.");
        }

        static private void calc_index(List<int> terms_list)
        {
            terms = terms_list;
            foreach (int t in terms)
            {
                sma[t] = new List<double>();
                divergence[t] = new List<double>();
                trendfollow[t] = new List<double>();
                sma[t] = calc_sma(t);
                divergence[t] = calc_divergence(t);
                vola_kyori[t] = calcVolaKyori(t);
                trendfollow[t] = calc_trendfollow(t);
                buysell_vol_ratio[t] = calcBuysellVolRatio(t);
                Buysellvol_price_ratio[t] = calcBusellvolPriceRatio(t);
                vol_ma_divergence[t] = calcVolMaDivergence(t);
                rsi[t] = calcRsi(t);
                uwahige[t] = calcUwahige(t);
                shitahige[t] = calcShitahige(t);
                if(vol_ma_divergence[t].GetRange(t+1, vol_ma_divergence[t].Count - t-1).Contains(double.NaN))
                    Console.WriteLine("Nan in vol_ma_divergence!" + " - "+t.ToString());
            }
            calcVolakyoriMinMaxScaler();
            calcDivergenceMinMaxScaler();
            calcVolMaDivergenceMinMaxScaler();
            calcBuySellVolRatioMinmaxScaler();
            calcRsiScaler();
            calcUwahigeScale();
            calcShitahigeScale();
        }

        //use to calc sma in MarketData for other index
        static private List<double> calc_sma(int term, List<double> data)
        {
            var res = new List<double>();
            //detect nan_ind
            var nan_ind = 0;
            for (int i = 0; i < data.Count; i++)
            {
                if (double.IsNaN(data[i]) == false)
                {
                    nan_ind = i;
                    break;
                }
            }
            for (int i = 0; i < term+nan_ind-1; i++) { res.Add(double.NaN); }
            var sumv = 0.0;
            for(int i=nan_ind; i<term+nan_ind; i++){ sumv += data[i]; }
            res.Add(sumv / term);
            for (int i=term+nan_ind; i < data.Count; i++)
            {
                sumv = sumv - data[i - term] + data[i];
                res.Add(sumv / term);
            }
            return res;
        }

        static private List<double> calc_divergence(List<double> price, List<double> ma)
        {
            List<double> res = new List<double>();
            if (price.Count == ma.Count)
            {
                for (int i = 0; i < price.Count; i++)
                {
                    if (price[i] == double.NaN || ma[i] == double.NaN) { res.Add(double.NaN); }
                    else if(price[i] == 0 && ma[i] == 0) { res.Add(0); }
                    else { res.Add((price[i] - ma[i]) / ma[i]); }
                }

            }
            else
                Console.WriteLine("marketData: Length is not matched in calc_divergence !");
            return res;
        }

        static private List<double> calc_ratio(List<double> denominator, List<double> numerator)
        {
            var res = new List<double>();
            if (denominator.Count == numerator.Count)
            {
                for (int i = 0; i < denominator.Count; i++)
                {
                    if (denominator[i] == double.NaN || numerator[i] == double.NaN) { res.Add(double.NaN); }
                    else if (Math.Abs(denominator[i]) == 0) { res.Add(numerator[i]); }
                    else { res.Add(numerator[i] / denominator[i]); }
                }
            }
            else
                Console.WriteLine("MarketData: data length is not matched in calc_ratio !");
            return res;
        }


        /*
         * 各termのdataの値を同じiについて並べて、minmax scaleする
         * data structure image
         * term=100: nan, nan, nan,,, nan, 0,2, 0,3,,
         * term=200: nan, nan, nan,,, nan, nan, 0,2, 0,3,,
         * term=300: nan, nan, nan,,, nan, nan, nan, 0,2, 0,3,,
         * 上記データを同じi番目のそれぞれのtermのデータをminmax scaleする。
         * 全部同じ値のときは0を出力とする（python minmax scalerと同じ）
         */
        static private Dictionary<int, List<double>> calc_minmax_scale(Dictionary<int, List<double>> data)
        {
            var res = new Dictionary<int, List<double>>();

            //detect max num of nan in data in all terms
            var nan_ind = 0;
            foreach (var t in terms)
            {
                var ind = 0;
                for (int i = 0; i < data[t].Count; i++)
                {
                    if (double.IsNaN(data[t][i]) == false)
                    {
                        ind = i;
                        break;
                    }
                }
                nan_ind = Math.Max(nan_ind, ind);
            }
            //don't do minmax scale calc and insert nan for first nan_ind index
            var tmp = new List<double>();
            for (int i = 0; i < terms.Count; i++) { tmp.Add(double.NaN); }
            for (int i = 0; i < nan_ind; i++) { res[i] = tmp; } //nan_ind is not NaN
            //calc minmax for all index from nan_ind
            for (int i = nan_ind; i < data[terms[0]].Count; i++)
            {
                var minmaxed_data = new List<double>();
                var data_with_i_converted = new List<double>();
                foreach (var t in terms)
                    data_with_i_converted.Add(data[t][i]);
                //check if all data is same (if so then min max scaled data should be 0)
                double check_same = 0.0;
                bool flg = false;
                for(int j=0; j<data_with_i_converted.Count; j++)
                {
                    if (j == 0) { check_same = data_with_i_converted[j]; }
                    else
                    {
                        if (check_same != data_with_i_converted[j])
                        {
                            flg = true;
                            break;
                        }
                    }
                }
                var maxv = data_with_i_converted.Max();
                var minv = data_with_i_converted.Min();
                foreach (var d in data_with_i_converted)
                {
                    if (flg)
                        minmaxed_data.Add((d - minv) / (maxv - minv));
                    else
                        minmaxed_data.Add(0);
                }
                res[i] = minmaxed_data;
            }
            return res;
        }



        static private List<double> calc_sma(int term)
        {
            return calc_sma(term, close);
        }

        
        static private List<double> calc_divergence(int term)
        {
            return calc_divergence(close, sma[term]);
        }

        static private List<double> calc_trendfollow(int term)
        {
            List<double> res = new List<double>();
            res.Add(double.NaN);
            for (int i = 1; i < sma[term].Count; i++)
            {
                if (double.IsNaN(sma[term][i])) { res.Add(double.NaN); }
                else { res.Add(sma[term][i] - sma[term][i - 1]); }
            }
            return res;
        }

        //各termのdivergenceの値を同じiについて並べて、minmax scaleしたもの
        /*
         * data structure image
         term=100: nan, nan, nan,,, nan, 0,2, 0,3,,
         term=200: nan, nan, nan,,, nan, nan, 0,2, 0,3,,
         term=300: nan, nan, nan,,, nan, nan, nan, 0,2, 0,3,,
        上記データを同じi番目のそれぞれのtermのデータをminmax scaleする。
         */
        static private void calcDivergenceMinMaxScaler()
        {
            divergence_minmax_scale = calc_minmax_scale(divergence);
        }


        /*1分毎のclose price二乗変化率合計値の移動平均
         * （各termのvola kyoriを同じiでmin max scaleした時にtermが長いものほど1に近い値になるのを調整する)
         */
        static private List<double> calcVolaKyori(int term)
        {
            var change = new List<double>();
            change.Add(double.NaN);//changeの計算分
            for (int i = 1; i < close.Count; i++)
                change.Add(Math.Pow(close[i] - close[i - 1], 2.0));
            return calc_sma(term, change);
        }

        

        //各termのvola_kyoriの値を同じiについて並べて、minmax scaleしたもの
        static private void calcVolakyoriMinMaxScaler()
        {
            vola_kyori_minmax_scale = calc_minmax_scale(vola_kyori);
        }


        static private List<double> calcVolMaDivergence(int term)
        {
            var vol_ma = new List<double>();
            vol_ma = calc_sma(term, size);
            return calc_divergence(size, vol_ma);
        }


        static private void calcVolMaDivergenceMinMaxScaler()
        {
            vol_ma_divergence_minmax_scale = calc_minmax_scale(vol_ma_divergence);
        }


        static private List<double> calcBuysellVolRatio(int term)
        {
            List<double> res = new List<double>();
            var buy_ma = calc_sma(term, buy_vol);
            var sell_ma = calc_sma(term, sell_vol);
            return calc_ratio(sell_ma, buy_ma);
        }


        static private void calcBuySellVolRatioMinmaxScaler()
        {
            buysell_vol_ratio_minmax_scale = calc_minmax_scale(Buysell_vol_ratio);
        }

        //その期間の価格変化率 / buysell vol ratioの割合
        static private List<double> calcBusellvolPriceRatio(int term)
        {
            var price_change = new List<double>();
            for (int i = 0; i < term; i++) { price_change.Add(double.NaN); }
            for (int i = term; i < close.Count; i++) { price_change.Add(close[i]/close[i-term]); }
            return calc_ratio(buysell_vol_ratio[term], price_change);
        }

        static private List<double> calcRsi(int term)
        {
            var res = new List<double>();
            var up_list = new List<double>();
            var down_list = new List<double>();
            for (int i = 0; i < term-1; i++)
            {
                if (close[i+1] - close[i] > 0)
                {
                    up_list.Add(close[i+1] - close[i]);
                    down_list.Add(0);
                }
                else
                {
                    down_list.Add(close[i+1] - close[i]);
                    up_list.Add(0);
                }
                res.Add(double.NaN);
            }
            var up = up_list.Sum() / Convert.ToDouble(term);
            var down = -down_list.Sum() / Convert.ToDouble(term);
            var r = up / (up + down);
            res.Add(r);
            for (int i = term-1; i < close.Count-1; i++)
            {
                if (close[i+1] - close[i] > 0) //yosen
                {
                    up_list.Add(close[i+1] - close[i]);
                    down_list.Add(0);
                }
                else
                {
                    down_list.Add(close[i+1] - close[i]);
                    up_list.Add(0);
                }
                up_list.RemoveAt(0);
                down_list.RemoveAt(0);
                up = up_list.Sum() / Convert.ToDouble(term);
                down = -down_list.Sum() / Convert.ToDouble(term);
                r = up / (up + down);
                if (up == 0 && down == 0)
                    r = 0;
                res.Add(r);
            }
            return res;
        }

        static private void calcRsiScaler()
        {
            rsi_scale = calc_minmax_scale(rsi);
        }

        static private List<double> calcUwahige(int term)
        {
            var res = new List<double>();
            var close_list = close.GetRange(0, term);
            for (int i = 0; i < term; i++)
                res.Add(double.NaN);
            for(int i=term; i<close.Count; i++)
            {
                if (close_list[0] > close_list[close_list.Count-1]) //insen
                    res.Add( 1000.0 * (close_list.Max() - close_list[0]) / close_list[close_list.Count-1] / Convert.ToDouble(term));
                else //yosen
                    res.Add( 1000.0 * (close_list.Max() - close_list[close_list.Count-1]) / close_list[close_list.Count - 1] / Convert.ToDouble(term));
                close_list.RemoveAt(0);
                close_list.Add(close[i]);
            }
            return res;
        }

        static private List<double> calcShitahige(int term)
        {
            var res = new List<double>();
            var close_list = close.GetRange(0, term);
            for (int i = 0; i < term; i++)
                res.Add(double.NaN);
            for (int i = term; i < close.Count; i++)
            {
                if (close_list[0] > close_list[close_list.Count - 1]) //insen
                    res.Add(1000.0 * (close_list[close_list.Count-1] - close_list.Min()) / close_list[close_list.Count - 1] / Convert.ToDouble(term));
                else //yosen
                    res.Add(1000.0 * (close_list[0] - close_list.Min()) / close_list[close_list.Count - 1] / Convert.ToDouble(term));
                close_list.RemoveAt(0);
                close_list.Add(close[i]);
            }
            var min = res.Min();
            var max = res.Max();
            return res;
        }

        static private void calcUwahigeScale()
        {
            uwahige_scale = calc_minmax_scale(uwahige);
        }

        static private void calcShitahigeScale()
        {
            shitahige_scale = calc_minmax_scale(shitahige);
        }


    }
}
