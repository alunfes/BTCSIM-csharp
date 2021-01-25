using System;
using System.Collections.Generic;
using System.Linq;

namespace BTCSIM
{
    /*input data:*/
    public class NN
    {
        private double tanh(double input_val)
        {
            return Math.Tanh(input_val);
        }

        private double sigmoid(double input_val)
        {
            return 1.0 / (1.0 + Math.Exp(-input_val));
        }


        
        private double[] calcWeights(double[] input_vals, Gene2 chromo, int layer_key, int activation)
        {
            var res = new double[chromo.weight_gene[layer_key].Count];
            for (int i = 0; i < chromo.weight_gene[layer_key].Count; i++) //for units
            {
                var sum_v = 0.0;
                for (int j = 0; j < input_vals.Length; j++) //for weight
                    sum_v += input_vals[j] * chromo.weight_gene[layer_key][i][j];  //weight_gene[layer][input unit][output unit]
                sum_v += chromo.bias_gene[layer_key][i];
                res[i] = (activation == 0 ? sigmoid(sum_v) : tanh(sum_v));
            }
            return res;
        }

        public double[] calcNN(double[] input_vals, int[] num_units, Gene2 chromo, int activation)
        {
            if (input_vals.Contains(Double.NaN))
            {
                Console.WriteLine("NN-calcNN: nan in included in input_vals !");
            }
            //input layer
            var inputs = calcWeights(input_vals, chromo, 0, activation);
            //middle layers
            for (int i = 1; i < chromo.weight_gene.Count; i++) //do calc for each layers
            {
                var outputs = calcWeights(inputs, chromo, i, activation);
                inputs = outputs;
            }
            return calcWeights(inputs, chromo, chromo.weight_gene.Count - 1, 0);
        }

        public int getActivatedUnit(double[] output_vals)
        {
            double maxv = 0.0;
            int max_ind = -1;
            for (int i = 0; i < output_vals.Length; i++)
            {
                if (maxv < output_vals[i])
                {
                    maxv = output_vals[i];
                    max_ind = i;
                }
            }
            if (max_ind < 0)
            {
                Console.WriteLine("NN-getActivatedUnit: Invalid output val !");
            }
            return max_ind;
        }

        //nn_output = "no", "buy", "sell", "cancel", "Market / Limit"
        /*int[action, order_type]
         * order_type: 0-> Market, 1->Limit
         */
        public List<int> getActivatedUnitLimitMarket(double[] output_vals)
        {
            var res = new List<int>();
            double maxv = 0.0;
            int max_ind = -1;
            for (int i = 0; i < output_vals.Length - 1; i++)
            {
                if (maxv < output_vals[i])
                {
                    maxv = output_vals[i];
                    max_ind = i;
                }
            }
            if (max_ind < 0)
            {
                Console.WriteLine("NN-getActivatedUnit: Invalid output val !");
            }
            res.Add(max_ind);
            //order type
            int otype = output_vals[output_vals.Length - 1] >= 0.5 ? 0 : 1;
            res.Add(otype);
            return res;
        }
    }
}