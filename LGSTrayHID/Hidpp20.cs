namespace LGSTrayHID
{
    public class Hidpp20
    {
        private readonly byte[] _buffer;

        public Hidpp20(byte[] buffer)
        {
            _buffer = buffer;
        }

        public int Length => _buffer.Length;

        public byte GetFeatureIndex()
        {
            return _buffer[2];
        }

        public byte GetSoftwareId()
        {
            return (byte)(_buffer[3] & 0x0F);
        }

        public Span<byte> GetParams()
        {
            return _buffer.AsSpan(4);
        }

        public byte GetParam(int paramIdx)
        {
            return _buffer[4 + paramIdx];
        }

        public static implicit operator byte[](Hidpp20 hidpp20)
        {
            return hidpp20._buffer;
        }

        public static implicit operator Hidpp20(byte[] buffer)
        {
            return new Hidpp20(buffer);
        }
    }
} 