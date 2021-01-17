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

        public double[] calcNN(double[] input_vals, int[] num_units, double[] weight1, double[] weight2, double[] bias1, double[] bias2, int activation)
        {
            if (input_vals.Contains(Double.NaN))
            {
                Console.WriteLine("NN-calcNN: nan in included in input_vals !");
            }
            if (input_vals.Length * num_units[1] == weight1.Length)
            {
                //first weight
                double[] sum_first_outputs = new double[num_units[1]];
                for (int i = 0; i < num_units[1]; i++)
                {
                    var sum_v = 0.0;
                    for (int j = 0; j < input_vals.Length; j++)
                    {
                        sum_v += input_vals[j] * weight1[i];
                    }
                    sum_v += bias1[i];
                    sum_first_outputs[i] = (activation == 0 ? sigmoid(sum_v) : tanh(sum_v));
                }

                //second weight
                double[] sum_second_outputs = new double[num_units[2]];
                for (int i = 0; i < num_units[2]; i++)
                {
                    var sum_v = 0.0;
                    for (int j = 0; j < sum_first_outputs.Length; j++)
                    {
                        sum_v += sum_first_outputs[j] * weight2[i];
                    }
                    sum_v += bias2[i];
                    sum_second_outputs[i] = sigmoid(sum_v);
                }
                return sum_second_outputs;
            }
            else
            {
                Console.WriteLine("# of input vals and units in first layer is not matched!");
                return new double[0];
            }
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
    }
}
