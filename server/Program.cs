using LiteDB;
using server.Services.Database;
using server.Services.gRPC;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddGrpc();
builder.Services.AddGrpcReflection();
builder.Services.AddScoped<IAccountLogger, AccountLogger>();
builder.Services.AddScoped<IAccountManager, AccountManager>();
builder.Services.AddScoped<IActivationCodeManager, ActivationCodeManager>();
builder.Services.AddScoped<IDbHolder, DbHolder>();
builder.Services.AddScoped<ITransactionManager, TransactionManager>();
var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapGrpcService<AccountService>();
app.MapGrpcReflectionService(); // This allows clients to discover the service and call it, which is espicially usefor for auto-testing
                                // If you don't want it (for example, for security reasons), delete this line without worry
app.Run();
