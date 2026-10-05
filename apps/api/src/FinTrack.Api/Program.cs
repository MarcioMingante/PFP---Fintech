using FinTrack.Infrastructure;
using FinTrack.Application.Accounts.Commands.CreateAccount;
using FinTrack.Api.Contracts.Accounts;
using FinTrack.Application.Accounts.Validators;
using FinTrack.Application.Accounts.Queries.ListAccounts;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<CreateAccountHandler>();
builder.Services.AddScoped<CreateAccountCommandValidator>();
builder.Services.AddScoped<ListAccountsHandler>();
var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy"
}));

app.MapPost(
    "/accounts",
    async (
        CreateAccountRequest request,
        CreateAccountCommandValidator validator,
        CreateAccountHandler handler,
        CancellationToken cancellationToken
    ) =>
    {
        var command = new CreateAccountCommand(request.Name);
        var errors = validator.Validate(command);

        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var account = await handler.HandleAsync(command, cancellationToken);

        return Results.Created($"/accounts/{account.Id}", account);
    }
);

app.MapGet(
    "/accounts",
    async (
        ListAccountsHandler handler,
        CancellationToken cancellationToken
    ) =>
    {
        var query = new ListAccountsQuery();

        var accounts = await handler.HandleAsync(query, cancellationToken);

        return Results.Ok(accounts);
    }
);

app.Run();
