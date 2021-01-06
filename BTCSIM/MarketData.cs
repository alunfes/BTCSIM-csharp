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
            get { return ref Buysellvol_price_ratio; }
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
            vol_ma_divergence_minmax_scale = new Dictionary<int, List<double>>();
            buysell_vol_ratio = new Dictionary<int, List<double>>();
            buysell_vol_ratio_minmax_scale = new Dictionary<int, List<double>>();
            Buysellvol_price_ratio = new Dictionary<int, List<double>>();

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
            }
            calcVolakyoriMinMaxScaler();
            calcDivergenceMinMaxScaler();
            calcVolMaDivergence();
            calcVolMaDivergenceMinMaxScaler();
            calcBuySellVolRatioMinmaxScaler();
        }

        static private List<double> calc_sma(int term)
        {
            List<double> res = new List<double>();
            for (int i = 0; i < term-1; i++) { res.Add(double.NaN); }
            var sumv = 0.0;
            for (int j = 0; j < term; j++) { sumv += close[j]; }
            for (int i = term; i < close.Count; i++)
            {
                res.Add(sumv / Convert.ToDouble(term));
                sumv = sumv - close[i - term] + close[i];
            }
            return res;
        }

        static private List<double> calc_divergence(int term)
        {
            List<double> res = new List<double>();
            for (int i = 0; i < sma[term].Count; i++)
            {
                if (double.IsNaN(sma[term][i])) { res.Add(double.NaN); }
                else { res.Add((close[i] - sma[term][i]) / sma[term][i]); }
            }
            return res;
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
        static private void calcDivergenceMinMaxScaler()
        {
            //detect max num of nan in divergence in all terms
            var nan_ind = 0;
            foreach(var t in terms)
            {
                var ind = 0;
                for(int i=0; i<divergence[t].Count; i++)
                {
                    if (double.IsNaN(divergence[t][i]) == false)
                    {
                        ind = i;
                        break;
                    }
                }
                nan_ind = Math.Max(nan_ind, ind);
            }
            //term[-1]まではnanなのでList<double>=nanを入れる。
            var tmp = new List<double>();
            for (int j = 0; j < divergence.Keys.Count; j++) { tmp.Add(double.NaN); }
            for (int i = 0; i < nan_ind; i++){divergence_minmax_scale[i] = tmp;}
            //nan値以降の値をminmax scaleする。
            for(int i=nan_ind; i<divergence[terms[0]].Count; i++)
            {
                var res = new List<double>();
                var data = new List<double>();
                foreach (var t in terms)
                    data.Add(divergence[t][i]);
                var maxv = data.Max();
                var minv = data.Min();
                foreach (var d in data)
                    res.Add( (d - minv) / (maxv - minv) );
                divergence_minmax_scale[i] = res;
            }
        }


        /*1分毎の二乗変化率合計値の移動平均をtermで割った値
         * （各termのvola kyoriを同じiでmin max scaleした時にtermが長いものほど1に近い値になるのを調整する)
         */
        static private List<double> calcVolaKyori(int term)
        {
            List<double> res = new List<double>();
            for (int i = 0; i < term; i++) { res.Add(double.NaN); }
            res.Add(double.NaN);
            var change = new List<double>();
            for (int i = 1; i < close.Count; i++)
                change.Add(Math.Pow((close[i] - close[i - 1]) / close[i-1], 2.0));
            var sumv = change.GetRange(0, term).Sum();
            res.Add(sumv/term);
            for (int i = term + 1; i < change.Count; i++)
            {
                sumv = sumv - change[i - 1] + change[i];
                res.Add(sumv/term);
            }
            return res;
        }

        //各termのvola_kyoriの値を同じiについて並べて、minmax scaleしたもの
        //
        static private void calcVolakyoriMinMaxScaler()
        {
            //detect max num of nan in divergence in all terms
            var nan_ind = 0;
            foreach (var t in terms)
            {
                var ind = 0;
                for (int i = 0; i < vola_kyori[t].Count; i++)
                {
                    if (double.IsNaN(vola_kyori[t][i]) == false)
                    {
                        ind = i;
                        break;
                    }
                }
                nan_ind = Math.Max(nan_ind, ind);
            }

            for (int i = 0; i < nan_ind; i++)
            {
                var tmp = new List<double>();
                for (int j = 0; j < terms.Count; j++)
                    tmp.Add(double.NaN);
                vola_kyori_minmax_scale[i] = tmp;
            }
            for (int i = nan_ind; i < vola_kyori[terms[0]].Count; i++)
            {
                var res = new List<double>();
                var data = new List<double>();
                foreach (var t in terms)
                    data.Add(vola_kyori[t][i]);
                var maxv = data.Max();
                var minv = data.Min();
                foreach (var d in data)
                    res.Add((d - minv) / (maxv - minv));
                vola_kyori_minmax_scale[i] = res;
            }
        }

        static private Dictionary<int, List<double>> calcVolMaDivergence()
        {
            var res = new Dictionary<int, List<double>>();
            foreach (var t in terms)
            {
                //calc vol ma
                List<double> vol_ma = new List<double>();
                for (int i = 0; i < t - 1; i++) { vol_ma.Add(double.NaN); }
                var sumv = 0.0;
                for (int j = 0; j < t; j++) { sumv += size[j]; }
                for (int i = t; i < size.Count; i++)
                {
                    vol_ma.Add(sumv / Convert.ToDouble(t));
                    sumv = sumv - size[i - t] + size[i];
                }

                //calc divergence
                var vol_ma_div = new List<double>();
                for (int i = 0; i < vol_ma.Count; i++)
                {
                    if (double.IsNaN(vol_ma[i])) { vol_ma_div.Add(double.NaN); }
                    else { vol_ma_div.Add((close[i] - vol_ma[i]) / vol_ma[i]); }
                }
                res[t] = vol_ma_div;
            }
            return res;
        }


        static private void calcVolMaDivergenceMinMaxScaler()
        {
            //detect max num of nan in divergence in all terms
            var nan_ind = 0;
            foreach (var t in terms)
            {
                var ind = 0;
                for (int i = 0; i < vol_ma_divergence[t].Count; i++)
                {
                    if (double.IsNaN(vol_ma_divergence[t][i]) == false)
                    {
                        ind = i;
                        break;
                    }
                }
                nan_ind = Math.Max(nan_ind, ind);
            }

            for (int i = 0; i < nan_ind; i++)
            {
                var tmp = new List<double>();
                for (int j = 0; j < terms.Count; j++)
                    tmp.Add(double.NaN);
                vol_ma_divergence_minmax_scale[i] = tmp;
            }
            for (int i = nan_ind; i < vol_ma_divergence[terms[0]].Count; i++)
            {
                var res = new List<double>();
                var data = new List<double>();
                foreach (var t in terms)
                    data.Add(vol_ma_divergence[t][i]);
                var maxv = data.Max();
                var minv = data.Min();
                foreach (var d in data)
                    res.Add((d - minv) / (maxv - minv));
                vol_ma_divergence_minmax_scale[i] = res;
            }
        }


        static private List<double> calcBuysellVolRatio(int term)
        {
            List<double> res = new List<double>();
            for (int i = 0; i < term; i++) { res.Add(double.NaN); }
            res.Add(double.NaN);
            var buy_sum = new List<double>();
            var sell_sum = new List<double>();
            for (int i = 0; i < term - 1; i++) { res.Add(double.NaN); }
            var buy_sumv = 0.0;
            var sell_sumv = 0.0;
            for (int j = 0; j < term; j++)
            {
                buy_sumv += buy_vol[j];
                sell_sumv += sell_vol[j];
            }
            for (int i = term; i < close.Count; i++)
            {
                res.Add(buy_sumv / sell_sumv);
                buy_sumv = buy_sumv - buy_vol[i - term] + buy_vol[i];
                sell_sumv = sell_sumv - sell_vol[i - term] + sell_vol[i];
            }
            return res;
        }


        static private void calcBuySellVolRatioMinmaxScaler()
        {
            var nan_ind = 0;
            foreach (var t in terms)
            {
                var ind = 0;
                for (int i = 0; i < buysell_vol_ratio[t].Count; i++)
                {
                    if (double.IsNaN(buysell_vol_ratio[t][i]) == false)
                    {
                        ind = i;
                        break;
                    }
                }
                nan_ind = Math.Max(nan_ind, ind);
            }

            for (int i = 0; i < nan_ind; i++)
            {
                var tmp = new List<double>();
                for (int j = 0; j < terms.Count; j++)
                    tmp.Add(double.NaN);
                buysell_vol_ratio_minmax_scale[i] = tmp;
            }
            for (int i = nan_ind; i < buysell_vol_ratio[terms[0]].Count; i++)
            {
                var res = new List<double>();
                var data = new List<double>();
                foreach (var t in terms)
                    data.Add(buysell_vol_ratio[t][i]);
                var maxv = data.Max();
                var minv = data.Min();
                foreach (var d in data)
                    res.Add((d - minv) / (maxv - minv));
                buysell_vol_ratio_minmax_scale[i] = res;
            }
        }

        //その期間の価格変化率 / buysell vol ratioの割合
        static private List<double> calcBusellvolPriceRatio(int term)
        {
            List<double> res = new List<double>();
            for (int i = 0; i < term; i++) { res.Add(double.NaN); }
            res.Add(double.NaN);
            for (int i = 0; i < term - 1; i++) { res.Add(double.NaN); }

            for (int i = term; i < close.Count; i++)
            {
                res.Add(buy_sumv / sell_sumv);
                buy_sumv = buy_sumv - buy_vol[i - term] + buy_vol[i];
                sell_sumv = sell_sumv - sell_vol[i - term] + sell_vol[i];
            }
            return res;
        }
    }
}
