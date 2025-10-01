using System;
using System.Collections.Generic;

namespace z3nCore
{
    public static class ListExtentions
    {
        public static object GetRandom(this List<string> list)
        {
            if (list.Count > 0)
            return list[new Random().Next(0, list.Count - 1)];
            return null;
        }

    }
}