namespace LGSTrayHID
{
    public static class Extensions
    {
        public static byte GetParam(this byte[] buffer, int index)
        {
            return buffer[4 + index];
        }
    }
} 