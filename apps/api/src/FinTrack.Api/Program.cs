using FinTrack.Infrastructure;
using FinTrack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using FinTrack.Application.Accounts.Commands.CreateAccount;
using FinTrack.Api.Contracts.Accounts;
using FinTrack.Application.Accounts.Validators;
using FinTrack.Application.Accounts.Queries.ListAccounts;
using FinTrack.Application.Accounts.Queries.GetAccount;
using FinTrack.Application.Accounts.Commands.UpdateAccount;
using FinTrack.Application.Accounts.Commands.DeleteAccount;
using FinTrack.Api.Contracts.Categories;
using FinTrack.Application.Categories.Commands.CreateCategory;
using FinTrack.Application.Categories.Validators;
using FinTrack.Application.Categories.Queries.ListCategories;
using FinTrack.Application.Categories.Queries.GetCategory;
using FinTrack.Application.Categories.Commands.UpdateCategory;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<CreateAccountHandler>();
builder.Services.AddScoped<CreateAccountCommandValidator>();
builder.Services.AddScoped<ListAccountsHandler>();
builder.Services.AddScoped<GetAccountHandler>();
builder.Services.AddScoped<UpdateAccountHandler>();
builder.Services.AddScoped<UpdateAccountCommandValidator>();
builder.Services.AddScoped<DeleteAccountHandler>();
builder.Services.AddScoped<CreateCategoryHandler>();
builder.Services.AddScoped<CreateCategoryCommandValidator>();
builder.Services.AddScoped<ListCategoriesHandler>();
builder.Services.AddScoped<GetCategoryHandler>();
builder.Services.AddScoped<UpdateCategoryHandler>();
builder.Services.AddScoped<UpdateCategoryCommandValidator>();

var app = builder.Build();

// Compose runs migrations in a one-off container before starting the HTTP API.
if (args.Contains("--migrate", StringComparer.Ordinal))
{
    await using var scope = app.Services.CreateAsyncScope();
    var database = scope.ServiceProvider.GetRequiredService<FinTrackDbContext>();
    await database.Database.MigrateAsync();
    await app.DisposeAsync();
    return;
}

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

app.MapPut(
    "/accounts/{id:guid}",
    async (
        Guid id,
        UpdateAccountRequest request,
        UpdateAccountCommandValidator validator,
        UpdateAccountHandler handler,
        CancellationToken cancellationToken
    ) =>
    {
        var command = new UpdateAccountCommand(
            id,
            request.Name
        );

        var errors = validator.Validate(command);

        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var account = await handler.HandleAsync(
            command,
            cancellationToken
        );

        if (account is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(account);
    }
);

app.MapDelete(
    "/accounts/{id:guid}",
    async (
        Guid id,
        DeleteAccountHandler handler,
        CancellationToken cancellationToken
    ) =>
    {
        var command = new DeleteAccountCommand(id);

        var deleted = await handler.HandleAsync(
            command,
            cancellationToken
        );

        if (!deleted)
        {
            return Results.NotFound();
        }

        return Results.NoContent();
    }
);

app.MapPost(
    "/categories",
    async (
        CreateCategoryRequest request,
        CreateCategoryCommandValidator validator,
        CreateCategoryHandler handler,
        CancellationToken cancellationToken
    ) =>
    {
        var command = new CreateCategoryCommand(request.Name);
        var errors = validator.Validate(command);

        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var category = await handler.HandleAsync(
            command,
            cancellationToken
        );

        return Results.Created(
            $"/categories/{category.Id}",
            category
        );
    }
);

app.MapGet(
    "/categories",
    async (
        ListCategoriesHandler handler,
        CancellationToken cancellationToken
    ) =>
    {
        var query = new ListCategoriesQuery();

        var categories = await handler.HandleAsync(query, cancellationToken);

        return Results.Ok(categories);
    }
);

app.MapGet(
    "/categories/{id:guid}",
    async (
        Guid id,
        GetCategoryHandler handler,
        CancellationToken cancellationToken
    ) =>
    {
        var query = new GetCategoryQuery(id);

        var category = await handler.HandleAsync(query, cancellationToken);

        if (category is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(category);
    }
);

app.MapPut(
    "/categories/{id:guid}",
    async (
        Guid id,
        UpdateCategoryRequest request,
        UpdateCategoryCommandValidator validator,
        UpdateCategoryHandler handler,
        CancellationToken cancellationToken
    ) =>
    {
        var command = new UpdateCategoryCommand(id, request.Name);
        var errors = validator.Validate(command);

        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var category = await handler.HandleAsync(command, cancellationToken);

        if (category is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(category);
    }
);

app.Run();

public partial class Program { }
