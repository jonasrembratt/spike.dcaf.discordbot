using System;

namespace DCAF.DiscordBot._lib
{
    public static class ArrayHelper
    {
        public static T[] CopyFrom<T>(this T[] self, T[] source, int sourceIndex = 0)
        {
            for (var i = 0; i < self.Length && sourceIndex < source.Length; i++, sourceIndex++)
            {
                self[i] = source[sourceIndex];
            }

            return self;
        }
    }
}