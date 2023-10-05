// This fileId is a copy of server/Services/gRPC/Extensions/GuidExtensions.cs

namespace server.Services.gRPC.Extensions
{
    public static class GuidExtensions
    {
        public static Google.Protobuf.ByteString ToByteString(this Guid guid)
        {
            return Google.Protobuf.ByteString.CopyFrom(guid.ToByteArray());
        }

        public static Guid ToGuid(this Google.Protobuf.ByteString bytes)
        {
            return new Guid(bytes.ToByteArray());
        }
    }
}
