using Google.Protobuf;

namespace client.Utils
{
    public static class ByteArrayExtensions
    {
        public static ByteString ToByteString(this byte[] bytes)
        {
            return ByteString.CopyFrom(bytes);
        }
    }
}
