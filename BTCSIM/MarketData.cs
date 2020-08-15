using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
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
        static private Dictionary<int, List<double>> sma;
        static private Dictionary<int, List<double>> divergence;
        static private Dictionary<int, List<double>> trendfollow;

        
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
        static public ref Dictionary<int, List<double>> Sma
        {
            get { return ref sma; }
        }
        static public ref Dictionary<int, List<double>> Divergence
        {
            get { return ref divergence; }
        }
        static public ref Dictionary<int, List<double>> Trendfollow
        {
            get { return ref trendfollow; }
        }


        static public void initializer(List<int> terms)
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
            sma = new Dictionary<int, List<double>>();
            divergence = new Dictionary<int, List<double>>();
            trendfollow = new Dictionary<int, List<double>>();
             
            read_data();
            calc_index(terms);

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
                unix_time.Add(Convert.ToDouble(data[6]));
                dt.Add(Convert.ToDateTime(data[0]));
                open.Add(Convert.ToDouble(data[1]));
                high.Add(Convert.ToDouble(data[2]));
                low.Add(Convert.ToDouble(data[3]));
                close.Add(Convert.ToDouble(data[4]));
                size.Add(Convert.ToDouble(data[5]));
            }
            Console.WriteLine("Completed read data.");
        }

        static private void calc_index(List<int> terms)
        {
            foreach(int t in terms)
            {
                sma[t] = new List<double>();
                divergence[t] = new List<double>();
                trendfollow[t] = new List<double>();
                sma[t] = calc_sma(t);
                divergence[t] = calc_divergence(t);
                trendfollow[t] = calc_trendfollow(t);
            }
        }

        static private List<double> calc_sma(int term)
        {
            List<double> res = new List<double>();
            for(int i=0; i<term; i++){res.Add(double.NaN);}
            var sumv = 0.0;
            for (int j = 0; j < term; j++){sumv += close[j];}
            for (int i=term; i<close.Count; i++)
            {
                res.Add(sumv / Convert.ToDouble(term));
                sumv = sumv - close[i - term] + close[i];
            }
            return res;
        }

        static private List<double> calc_divergence(int term)
        {
            List<double> res = new List<double>();
            for(int i=0; i<sma[term].Count; i++)
            {
                if (double.IsNaN(sma[term][i])){ res.Add(double.NaN); }
                else{res.Add((close[i] - sma[term][i]) / sma[term][i]);}
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
                else { res.Add(sma[term][i] - sma[term][i-1]); }
            }
            return res;
        }
    }
}
