using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Pustalorc.Plugins.BaseClustering.API.Statics
{
    public static class ExtendedMath
    {
        public static Vector3 Pow(Vector3 x, int y)
        {
            if (y == 0)
            {
                if (x.x == 0)
                    x.x = float.NaN;
                else
                    x.x = 1;

                if (x.y == 0)
                    x.y = float.NaN;
                else
                    x.y = 1;

                if (x.z == 0)
                    x.z = float.NaN;
                else
                    x.z = 1;

                return x;
            }

            var output = x;

            while (y > 1)
            {
                output.x *= x.x;
                output.y *= x.y;
                output.z *= x.z;
                y--;
            }

            return output;
        }

        public static Vector3 Abs(Vector3 element)
        {
            element.x = Math.Abs(element.x);
            element.y = Math.Abs(element.y);
            element.z = Math.Abs(element.z);
            return element;
        }

        public static double StandardDeviation(IEnumerable<float> values)
        {
            double standardDeviation = 0;
            values = values.ToList();

            if (!values.Any()) return standardDeviation;

            var avg = values.Average();
            var sum = values.Sum(d => Math.Pow(d - avg, 2));

            standardDeviation = Math.Sqrt(sum / (values.Count() - 1));

            return standardDeviation;
        }
    }
}