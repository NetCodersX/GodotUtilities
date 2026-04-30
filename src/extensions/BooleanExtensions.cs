namespace Utilities;

public static class BooleanExtensions
{
    public static float ToSingle(this bool value)
    {
        return value ? 1f : 0f;
    }

    public static int ToSign(this bool value)
    {
        return value ? 1 : -1;
    }
}