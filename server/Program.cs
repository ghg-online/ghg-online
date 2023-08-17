using LiteDB;
using server.Managers;
using server.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddGrpc();
builder.Services.AddScoped<IAccountLogger, AccountLogger>();
builder.Services.AddScoped<IAccountManager, AccountManager>();
builder.Services.AddScoped<IActivationCodeManager, ActivationCodeManager>();
builder.Services.AddScoped<IDbHolder, DbHolder>();
builder.Services.AddScoped<ITransactionManager, TransactionManager>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapGrpcService<AccountService>();
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client.");
app.Run();
