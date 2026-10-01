using FinTrack.Infrastructure;
using FinTrack.Application.Accounts.Commands.CreateAccount;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<CreateAccountHandler>();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy"
}));

app.Run();
