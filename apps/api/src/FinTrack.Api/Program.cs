using FinTrack.Infrastructure;
using FinTrack.Application.Accounts.Commands.CreateAccount;
using FinTrack.Api.Contracts.Accounts;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<CreateAccountHandler>();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy"
}));

app.MapPost(
    "/accounts",
    async (
        CreateAccountRequest request,
        CreateAccountHandler handler,
        CancellationToken cancellationToken
    ) =>
    {
        var command = new CreateAccountCommand(request.Name);

        var account = await handler.HandleAsync(command, cancellationToken);

        return Results.Created($"/accounts/{account.Id}", account);
    }
);

app.Run();
