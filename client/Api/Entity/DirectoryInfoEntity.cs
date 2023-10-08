namespace client.Api.Entity
{
    public struct DirectoryInfoEntity
    {
        public Guid DirectoryId { get; set; }
        public string Name { get; set; }
        public Guid ParentId { get; set; }
    }
}
