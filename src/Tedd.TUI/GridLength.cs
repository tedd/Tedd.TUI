using System;

namespace Tedd.TUI;

public enum GridUnitType
{
    Auto,
    Pixel,
    Star
}

public struct GridLength
{
    public double Value;
    public GridUnitType GridUnitType;

    public GridLength(double value, GridUnitType type)
    {
        Value = value;
        GridUnitType = type;
    }

    public static GridLength Auto => new GridLength(1, GridUnitType.Auto);
    public static GridLength Star => new GridLength(1, GridUnitType.Star);
    public static GridLength Pixel(int value) => new GridLength(value, GridUnitType.Pixel);
}
