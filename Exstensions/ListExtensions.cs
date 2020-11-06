using System;
using System.Collections.Generic;
using System.Linq;

namespace DropletDerandomizer.Extensions
{
    public static class ListExtensions
    {
        /// <summary>
        /// Binary search for equal and closest lower integer values, can probably be optimized further. Returns the first element upon failure for this project's purposes
        /// </summary>
        public static T GetFirstLowerOrEqual<T>(this List<T> list, int key, Func<T, int> selector)
        {
            if (list.Count == 0 || list == null)
                return default;

            List<int> newList = list.Select(x => selector(x)).ToList();

            int min = 0;
            int max = newList.Count - 1;

            while (min <= max)
            {
                int mid = (min + max) / 2;

                if (key >= newList[mid])
                {
                    // if key is higher than anything in the list, return. it's here to prevent ArgumentOutOfRangeException
                    if (min == newList.Count - 1)
                    {
                        return list[mid];
                    }

                    // if the next element is higher than key, we know that mid is the closest lower or equal value
                    if (key < newList[mid + 1])
                    {
                        return list[mid];
                    }

                    min = mid + 1;
                }
                else
                {
                    max = mid - 1;
                }
            }

            return list[0];
        }
    }
}
