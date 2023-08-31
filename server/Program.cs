using server.Services.Authenticate;
using server.Services.Authorize;
using server.Services.Database;
using server.Services.gRPC;
using System.IdentityModel.Tokens.Jwt;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddGrpc();
builder.Services.AddGrpcReflection();
builder.Services.AddSingleton<JwtSecurityTokenHandler>();
builder.Services.AddSingleton<IRsaPairManager, RsaPairManager>();
builder.Services.AddSingleton<IRsaPubkeyManager, RsaPairManager>();
builder.Services.AddSingleton<ITokenService, TokenService>();
builder.Services.AddSingleton<AuthHelper>();
builder.Services.AddSingleton<IAccountLogger, AccountLogger>();
builder.Services.AddSingleton<IAccountManager, AccountManager>();
builder.Services.AddSingleton<IActivationCodeManager, ActivationCodeManager>();
builder.Services.AddSingleton<IComputerManager, ComputerManager>();
builder.Services.AddSingleton<IFileDataManager, FileDataManager>();
builder.Services.AddSingleton<IFileSystemManager, FileSystemManager>();
builder.Services.AddSingleton<ITransactionController, TransactionController>();
builder.Services.AddSingleton<IDbHolder, DbHolder>();
var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapGrpcService<AccountService>();
app.MapGrpcReflectionService(); // This allows clients to discover the service and call it, which is espicially usefor for auto-testing
                                // If you don't want it (for example, for security reasons), delete this line without worry
app.Map("/", () => "Please use a gRPC client");
app.Run();
