using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;

namespace BTCSIM
{
    static public class LineChart
    {
        static public void DisplayLineChart(List<double> data, string title)
        {
            Console.WriteLine("displaying line chart...");
            Encoding enc = Encoding.GetEncoding("UTF-8");
            StreamWriter writer = new StreamWriter(@"./line_chart.html", false, enc);
            writer.WriteLine(@"<!DOCTYPE html>"+ "\r\n" +
                @"<html lang=""ja"">"+ "\r\n" +
                @"<head>" + "\r\n" +
                @"<meta charset = ""utf-8"">" + "\r\n" +
                @"<title> グラフ </title>" + "\r\n" +
                @"</head>" + "\r\n" +
                @"<body>" + "\r\n" +
                @"<h1>" + title +"</h1>" + "\r\n"+
                @"<canvas id=""myLineChart""></canvas>" + "\r\n" +
                @"<script src=""https://cdnjs.cloudflare.com/ajax/libs/Chart.js/2.7.2/Chart.bundle.js""></script>" + "\r\n" +
                @"<script>" + "\r\n" +
                @"var ctx=document.getElementById(""myLineChart"");" + "\r\n"+
                @"var myLineChart=new Chart(ctx, {" + "\r\n"+
                @"type: 'line'," + "\r\n"+
                @"data: {" + "\r\n"+
                GenerateNumericalLabel(data) +
                @"datasets: [" + "\r\n"+
                @"{" + "\r\n"+
                @"label: 'PL'," + "\r\n"+
                GenerateData(data) +
                @"borderColor: ""rgba(255,0,0,1)""," + "\r\n"+
                @"backgroundColor: ""rgba(0,0,0,0)""" + "\r\n"+
                @"}" + "\r\n"+
                @"]," + "\r\n"+
                @"}," + "\r\n"+
                @"options: {" + "\r\n"+
                @"title: {" + "\r\n"+
                @"display: true," + "\r\n"+
                @"text: 'PL Log'" + "\r\n"+
                @"}," + "\r\n"+
                @"scales: {" + "\r\n"+
                @"yAxes: [{" + "\r\n"+
                @"ticks: {" + "\r\n"+
                @"suggestedMax:" +data.Max().ToString()+ ",\r\n" +
                @"suggestedMin:"+data.Min().ToString() + ",\r\n" +
                //@"stepSize: 10," + "\r\n"+
                @"callback: function(value, index, values){" + "\r\n"+
                @"return  value +  'usd'" + "\r\n"+
                @"}" + "\r\n" +
                @"}" + "\r\n" +
                @"}]" + "\r\n" +
                @"}," + "\r\n" +
                @"}" + "\r\n" +
                @"});" + "\r\n" +
                @"</script>" + "\r\n"+
                @"</body>" + "\r\n"+
                @"</html>"+ "\r\n"
                );
                writer.Close();

            System.Diagnostics.Process.Start(@"/Applications/Google Chrome.app", @"./line_chart.html");
        }


        static private string GenerateNumericalLabel(List<double> data)
        {
            string label = "labels: [";
            var num_array = Enumerable.Range(0, data.Count).ToArray();
            label += string.Join(", ", num_array) + "],\r\n";
            return label;
        }

        static private string GenerateData(List<double> data)
        {
            string d = "data: [";
            d += string.Join(", ", data) + "],\r\n";
            return d;
        }
    }
}