using LiteDB;
using server.Services.Database;
using server.Services.gRPC;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddGrpc();
builder.Services.AddGrpcReflection();
builder.Services.AddSingleton<IAccountLogger, AccountLogger>();
builder.Services.AddSingleton<IAccountManager, AccountManager>();
builder.Services.AddSingleton<IActivationCodeManager, ActivationCodeManager>();
builder.Services.AddSingleton<IDbHolder, DbHolder>();
var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapGrpcService<AccountService>();
app.MapGrpcReflectionService(); // This allows clients to discover the service and call it, which is espicially usefor for auto-testing
                                // If you don't want it (for example, for security reasons), delete this line without worry
app.Map("/", () => "Please use a gRPC client");
app.Run();
