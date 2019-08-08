using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ghGear.Util
{
    //show ratio
    public class GCD
    {
        public double[] numbers;
        public List<double> ratios = new List<double>();

        public GCD(List<int> elements)
        {
            numbers = new double[elements.Count];
            for (int i = 0; i < elements.Count; i++)
            {
                numbers[i] = elements[i];
            }
        }

        public double gcd(double a, double b)
        {
            if (a == 0)
                return b;
            return gcd(b % a, a);
        }

        public List<double> getGCD()
        {
            double result = numbers[0];
            int id = 0;
            for (int i = 0; i < numbers.Length; i++)
            {
                if (numbers[i] < result) { result = numbers[i]; id = i; }
            }

            for (int i = 0; i < numbers.Length; i++)
            {
                result = gcd(numbers[i], result);
                ratios.Add(numbers[i] / result);
            }
            return ratios;
        }

        public String printGCD()
        {
            String ratio = "";
            double result = numbers[0];
            int id = 0;

            for (int i = 0; i < numbers.Length; i++)
            {
                if (numbers[i] < result) { result = numbers[i]; id = i; }
            }

            for (int i = 0; i < numbers.Length; i++)
            {
                if (i < numbers.Length - 1)
                {
                    result = gcd(numbers[i], result);
                    ratio += (numbers[i] / result).ToString();
                    ratio += ":";
                }
                else
                {
                    result = gcd(numbers[i], result);
                    ratio += (numbers[i] / result).ToString();
                }
            }
            return ratio;
        }

    }
}
