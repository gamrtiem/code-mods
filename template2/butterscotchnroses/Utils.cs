using UnityEngine;

namespace BNR;

public class BNRUtils
{
    public static Color Color255(int r, int g, int b, int a)
    {
        return new Color(r / 255f, g / 255f, b / 255f, a / 255f);
    }
    
    public static Color Color255(int r, int g, int b)
    {
        return new Color(r / 255f, g / 255f, b / 255f, 1);
    }
}