using SDG.Provider;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Pustalorc.Plugins.BaseClustering.API.Statics
{
    public static class Extensions
    {
        public static Vector3 AverageCenter(this IEnumerable<Vector3> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            var list = source.ToList();

            var sum = Vector3.zero;

            checked
            {
                sum = list.Aggregate(sum, (current, element) => current + element);
            }

            if (list.Count > 0) return sum / list.Count;

            throw new Exception("The collection had no elements. Cannot divide by 0.");
        }

        public static decimal AverageDistance(this IEnumerable<Vector3> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            var list = source.ToList();

            var sum = decimal.Zero;
            var count = 0;

            checked
            {
                for (var i = 0; i < list.Count; i++)
                {
                    for (var o = i+1; o < list.Count; o++)
                    {
                        sum += (decimal)Vector3.Distance(list[i], list[o]);
                        count++;
                    }
                }
            }

            if (list.Count > 0) return sum / count;

            throw new Exception("The collection had no elements. Cannot divide by 0.");
        }

        public static Vector3 GetScalar(this IEnumerable<Vector3> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            var scalar = Vector3.zero;
            var list = source.ToList();

            checked
            {
                foreach (var element in list)
                {
                    scalar.x += element.x * element.x;
                    scalar.y += element.y * element.y;
                    scalar.z += element.z * element.z;
                }
            }

            if (list.Count > 0) return scalar / list.Count;

            throw new Exception("The collection had no elements. Cannot divide by 0.");
        }

        public static List<Vector3> GetDensity(this IEnumerable<Vector3> source, Vector3 mean, Vector3 scalar)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            var list = source.ToList();
            var output = new List<Vector3>();

            checked
            {
                foreach (var element in list)
                {
                    output.Add(ExtendedMath.Pow(ExtendedMath.Abs(element - mean), 2) + scalar - ExtendedMath.Pow(mean, 2));
                }
            }

            return output;
        }

        public static Dictionary<int, Vector3> SubtractMaintainOriginalIndices(this IEnumerable<Vector3> source,
            Vector3 value)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            var list = source.ToList();
            var output = new Dictionary<int, Vector3>();

            for (var i = 0; i < list.Count; i++)
            {
                output.Add(i, ExtendedMath.Pow(list[i] - value, 2));
            }

            return output;
        }

        public static Vector3 GetScalar<TSource>(this IEnumerable<TSource> source, Func<TSource, Vector3> selector)
        {
            return source.Select(selector).GetScalar();
        }

        public static List<Vector3> GetDensity<TSource>(this IEnumerable<TSource> source,
            Func<TSource, Vector3> selector, Vector3 mean, Vector3 scalar)
        {
            return source.Select(selector).GetDensity(mean, scalar);
        }

        public static Vector3 AverageCenter<TSource>(this IEnumerable<TSource> source, Func<TSource, Vector3> selector)
        {
            return source.Select(selector).AverageCenter();
        }

        public static decimal AverageDistance<TSource>(this IEnumerable<TSource> source, Func<TSource, Vector3> selector)
        {
            return source.Select(selector).AverageDistance();
        }

        public static Dictionary<int, Vector3> SubtractMaintainOriginalIndices<TSource>(
            this IEnumerable<TSource> source, Func<TSource, Vector3> selector, Vector3 value)
        {
            return source.Select(selector).SubtractMaintainOriginalIndices(value);
        }

        public static Dictionary<int, Vector3> DivideDictionaryVector3(this Dictionary<int, Vector3> source,
            double value)
        {
            return source.ToDictionary(d => d.Key, d => d.Value / (float) value);
        }

        public static Dictionary<int, Vector3> GetMatchingWithOriginal(this Dictionary<int, Vector3> value, List<Vector3> source,
            Predicate<Vector3> match)
        {
            return value.Where(l => match(l.Value)).ToDictionary(l => l.Key, l => source[l.Key]);
        }

        public static Dictionary<int, float> GetDistances(this Dictionary<int, Vector3> list, Vector3 value)
        {
            return list.ToDictionary(l => l.Key, l => Vector3.Distance(l.Value, value));
        }

        public static List<float> GetDistances<T>(this IEnumerable<T> list, Func<T, Vector3> selector, Vector3 value)
        {
            return list.Select(selector).Select(l => Vector3.Distance(l, value)).ToList();
        }

        public static List<Vector3> GetLocalScalar(this Dictionary<int, Vector3> data)
        {
            return data.Select(d => ExtendedMath.Pow(d.Value, 2) / data.Count).ToList();
        }

        public static Dictionary<int, Vector3> GetLocalDensity(this Dictionary<int, Vector3> data, Vector3 mean,
            List<Vector3> scalar)
        {
            var density = new Dictionary<int, Vector3>();
            for (var i = 0; i < data.Count; i++)
                density.Add(data.Keys.ToList()[i],
                    ExtendedMath.Pow(ExtendedMath.Abs(data.Values.ToList()[i] - mean), 2) + scalar[i] - ExtendedMath.Pow(mean, 2));

            return density;
        }

        public static Vector3 MinVector3(this List<Vector3> vectors)
        {
            var smallestVector = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);

            foreach (var vector in vectors)
            {
                if (IsSmaller(vector, smallestVector))
                    smallestVector = vector;
            }

            return smallestVector;
        }

        public static int MinVector3(this Dictionary<int, Vector3> vectors)
        {
            var smallestVector = new KeyValuePair<int, Vector3>(int.MaxValue, new Vector3(float.MaxValue, float.MaxValue, float.MaxValue));

            foreach (var vector in vectors)
            {
                if (IsSmaller(vector.Value, smallestVector.Value))
                    smallestVector = vector;
            }

            return smallestVector.Key;
        }

        public static bool IsSmaller(Vector3 x, Vector3 y)
        {
            var total = 0;

            if (x.x < y.x)
                total++;

            if (x.y < y.y)
                total++;

            if (x.z < y.z)
                total++;

            return total >= 2;
        }
    }
}