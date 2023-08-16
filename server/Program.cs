using server.Helpers;
using server.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddGrpc();
builder.Services.AddSingleton<IDbHolder, DbHolder>();
builder.Services.AddSingleton<IAccountLogger, AccountLogger>();
var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapGrpcService<AccountService>();
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client.");
app.Run();
