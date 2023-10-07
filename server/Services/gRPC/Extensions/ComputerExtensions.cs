using server.Protos;

namespace server.Services.gRPC.Extensions
{
    public static class ComputerExtensions
    {
        public static ComputerInfo ToComputerInfo(this Entities.Computer computer)
        {
            return new ComputerInfo()
            {
                Id = computer.Id.ToByteString(),
                Name = computer.Name,
                Owner = computer.Owner.ToByteString(),
                RootDirectory = computer.RootDirectory.ToByteString(),
            };
        }
    }
}