namespace client.Api.Entity
{
    public struct ComputerInfoEntity
    {
        public Guid ComputerId { get; set; }
        public string Name { get; set; }
        public Guid OwnerAccountId { get; set; }
        public Guid RootDirectoryId { get; set; }
    }
}
