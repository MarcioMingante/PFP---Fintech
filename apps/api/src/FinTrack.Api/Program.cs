using FinTrack.Infrastructure;
using FinTrack.Application.Accounts.Commands.CreateAccount;
using FinTrack.Api.Contracts.Accounts;
using FinTrack.Application.Accounts.Validators;
using FinTrack.Application.Accounts.Queries.ListAccounts;
using FinTrack.Application.Accounts.Queries.GetAccount;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<CreateAccountHandler>();
builder.Services.AddScoped<CreateAccountCommandValidator>();
builder.Services.AddScoped<ListAccountsHandler>();
builder.Services.AddScoped<GetAccountHandler>();

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

app.MapGet(
    "/accounts/{id:guid}",
    async (
        Guid id,
        GetAccountHandler handler,
        CancellationToken cancellationToken
    ) =>
    {
        var query = new GetAccountQuery(id);

        var account = await handler.HandleAsync(
            query,
            cancellationToken
        );

        if (account is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(account);
    }
);

app.Run();
