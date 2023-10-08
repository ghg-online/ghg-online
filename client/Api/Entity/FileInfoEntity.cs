using server.Protos;
using System.Runtime.CompilerServices;

namespace client.Api.Entity
{
    public struct FileInfoEntity
    {
        public Guid FileId { get; set; }
        public string Name { get; set; }
        public Guid ParentId { get; set; }
        public File.TypeCode TypeCode { get; set; }
    }
}
