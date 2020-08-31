using System;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;


namespace BTCSIM
{
    public class GraphForm : Form
    {
        public GraphForm(double[] y_values)
        {
            var chart1 = new Chart();
            chart1.Size = new Size(300,300);
            // フォームをロードするときの処理
            chart1.Series.Clear();  // ← 最初からSeriesが1つあるのでクリアします
            chart1.ChartAreas.Clear();

            // ChartにChartAreaを追加します
            ChartArea ca = new ChartArea();
            ca.Name = "ChartArea1";
            ca.BackColor = Color.White;
            ca.BorderColor = Color.FromArgb(26, 59, 105);
            ca.BorderWidth = 0;
            ca.BorderDashStyle = ChartDashStyle.Solid;
            ca.AxisX = new Axis();
            ca.AxisY = new Axis();
            chart1.ChartAreas.Add(ca);
            // ChartにSeriesを追加します
            string legend1 = "Graph1";
            chart1.Series.Add(legend1);
            // グラフの種別を指定
            chart1.Series[legend1].ChartType = SeriesChartType.Line; // 折れ線グラフを指定してみます


            // データをシリーズにセットします
            for (int i = 0; i < y_values.Length; i++)
            {
                chart1.Series[legend1].Points.AddY(y_values[i]);
            }

            
        }


    }
}
