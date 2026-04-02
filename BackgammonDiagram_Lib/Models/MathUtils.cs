namespace BackgammonDiagram_Lib;

internal static class MathUtils
{
    public static bool IsPowerOfTwo(int n) => n > 0 && (n & (n - 1)) == 0;
}