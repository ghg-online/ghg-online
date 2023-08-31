using Grpc.Core;
using Microsoft.IdentityModel.Tokens;
using server.Protos;
using server.Services.Authorize;
using server.Services.Database;
using server.Services.gRPC.Extensions;

namespace server.Services.gRPC
{
    public class ComputerService : Computer.ComputerBase
    {
        readonly AuthHelper authHelper;
        readonly IComputerManager computerManager;

        public ComputerService(AuthHelper authHelper, IComputerManager computerManager)
        {
            this.authHelper = authHelper;
            this.computerManager = computerManager;
        }

        public override Task<GetMyComputerRespond> GetMyComputer(GetMyComputerRequest request, ServerCallContext context)
        {
            var tokenInfo = authHelper.GetValidatedToken(context);
            var ownedComputers = computerManager.QueryComputerByOwner(tokenInfo.Id);
            if (ownedComputers.IsNullOrEmpty())
            {
                throw new RpcException(new Status(StatusCode.NotFound, "You don't have any computer!"));
            }
            else
            {
                var computer = ownedComputers.FirstOrDefault()!;
                var info = new ComputerInfo()
                {
                    Id = computer.Id.ToByteString(),
                    Name = computer.Name,
                    Owner = computer.Owner.ToByteString(),
                    RootDirectory = computer.RootDirectory.ToByteString(),
                };
                return Task<GetMyComputerRespond>.FromResult(new GetMyComputerRespond() { Info = info });
            }
        }
    }
}
