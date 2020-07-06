using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace ARCore.Helpers
{
    // A container class for various class extensions used throughout ARCore
    public static class Extensions
    {
        public static bool ValueFitsStandardDeviation(this IEnumerable<double> Object, double Value)
        {
            double average = 0;
            if (Object.Count() > 0)
                average = Object.Average(); //We calculate the average value in the array
            else
                return false;
                //throw new ArgumentException("Collection may not be empty for standard deviation calculations.");
            double sumOfSquaresOfdifferences = Object.Select(val => (val - average) * (val - average)).Sum(); //We calculate the sum of all differences squared
            double standardDeviantion = Math.Sqrt(sumOfSquaresOfdifferences / Object.Count()); //We calculate the average 'difference' of the array.
            //Return if the absolute difference between the value and average is within double the standard deviation
            return Math.Abs(Value - average) < (2 * standardDeviantion);
        }

        public static T Shift<T>(this ConcurrentQueue<T> queue, T item)
        {
            queue.TryDequeue(out T tmp);
            queue.Enqueue(item);
            return tmp;
        }

        /// <summary>
        /// Removes an item from the list, and returns the removed item
        /// </summary>
        /// <param name="index">The index of the item you want removed. Default: last item</param>
        /// <returns>The removed item</returns>
        public static T Pop<T>(this List<T> source, int index = -1)
        {
            T item = source[index];
            source.RemoveAt(index > -1 ? index : source.Count - 1);

            return item;
        }

        /// <summary>
        /// Centers a string using padding on both sides
        /// </summary>
        /// <param name="source">The string being padded</param>
        /// <param name="length">The total string length, including padding</param>
        /// <param name="padchar">The character to pad with</param>
        /// <returns>The padded string</returns>
        public static string Center(this string source, int length, char padchar = ' ')
        {
            int spaces = length - source.Length;
            int padLeft = spaces / 2 + source.Length;
            return source.PadLeft(padLeft, padchar).PadRight(length, padchar);

        }

        /// <summary>
        /// An Async version of ForEach. Iterates through an IEnumerable, executing a function on each item.
        /// </summary>
        /// <typeparam name="T">The Type stored in the IEnumerable</typeparam>
        /// <param name="Enumerable">The IEnumerable being iterated over</param>
        /// <param name="func">The function being executed on each item</param>
        public static async Task ForEachAsync<T>(this IEnumerable<T> Enumerable, Func<T, Task> func)
        {
            foreach (var item in Enumerable)
                await func(item);
        }

        /// <summary>
        /// Iterates through an IEnumerable, executing a function on each item.
        /// </summary>
        /// <typeparam name="T">The Type stored in the IEnumerable</typeparam>
        /// <param name="Enumerable">The IEnumerable being iterated over</param>
        /// <param name="func">The function being executed on each item</param>
        public static void ForEach<T>(this IEnumerable<T> Enumerable, Action<T> Action)
        {
            foreach (var item in Enumerable)
                Action(item);
        }
    }
}
