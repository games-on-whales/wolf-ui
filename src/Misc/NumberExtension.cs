

using System;

namespace WolfUI.Misc;

public static class NumberExtension
{
    public static int Between(this int value, int min, int max)
    {
        return Math.Min(max, Math.Max(min, value));
    }
}