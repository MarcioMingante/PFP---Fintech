using FinTrack.Application.Authentication;
using FinTrack.Infrastructure.Authentication;
using System.Security.Claims;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using FinTrack.Infrastructure;
using FinTrack.Infrastructure.Jobs;
using FinTrack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using FinTrack.Application.CreditCards.Commands.CreateCreditCard;
using FinTrack.Api.Contracts.CreditCards;
using FinTrack.Application.CreditCards.Validators;
using FinTrack.Application.CreditCards.Queries.ListCreditCards;
using FinTrack.Application.CreditCards.Queries.GetCreditCard;
using FinTrack.Application.CreditCards.Commands.UpdateCreditCard;
using FinTrack.Application.CreditCards.Commands.DeleteCreditCard;

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

using FinTrack.Application.Purchases;

using FinTrack.Application.Invoices;

using FinTrack.Application.Recurring;

using FinTrack.Application.Dashboard;

using FinTrack.Application.Transactions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
var migrating = args.Contains("--migrate", StringComparer.Ordinal);
if (!migrating)
{
    builder.Services.AddFinTrackAuthentication(builder.Configuration);
    builder.Services.AddBackgroundJobs(builder.Configuration);
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = builder.Configuration.GetValue("Auth:RequestsPerMinute", 20), Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    });
}
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<RecurringTransactionService>();
builder.Services.AddScoped<TransactionService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<InvoiceService>();
builder.Services.AddScoped<PurchaseService>();
builder.Services.AddScoped<CreatePurchaseCommandValidator>();
builder.Services.AddScoped<CreateCreditCardHandler>();
builder.Services.AddScoped<CreateCreditCardCommandValidator>();
builder.Services.AddScoped<ListCreditCardsHandler>();
builder.Services.AddScoped<GetCreditCardHandler>();
builder.Services.AddScoped<UpdateCreditCardHandler>();
builder.Services.AddScoped<UpdateCreditCardCommandValidator>();
builder.Services.AddScoped<DeleteCreditCardHandler>();
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
if (migrating)
{
    await using var scope = app.Services.CreateAsyncScope();
    var database = scope.ServiceProvider.GetRequiredService<FinTrackDbContext>();
    await database.Database.MigrateAsync();
    await app.DisposeAsync();
    return;
}

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

var creditCards = app.MapGroup("/credit-cards").RequireAuthorization();
var accounts = app.MapGroup("/accounts").RequireAuthorization();
var categories = app.MapGroup("/categories").RequireAuthorization();
var auth = app.MapGroup("/auth");
auth.MapPost("/register", async (Credentials credentials, IAuthService service, CancellationToken cancellationToken) =>
{
    var errors = CredentialsValidator.Validate(credentials);
    if (errors.Count > 0) return Results.ValidationProblem(errors);
    var result = await service.RegisterAsync(credentials, cancellationToken);
    return result.Status == AuthStatus.Conflict
        ? Results.Conflict(new { message = "E-mail já cadastrado." })
        : Results.Json(result.Tokens, statusCode: StatusCodes.Status201Created);
}).RequireRateLimiting("auth");
auth.MapPost("/login", async (Credentials credentials, IAuthService service, CancellationToken cancellationToken) =>
{
    var result = await service.LoginAsync(credentials, cancellationToken);
    return result.Status == AuthStatus.Success ? Results.Ok(result.Tokens) : Results.Unauthorized();
}).RequireRateLimiting("auth");
auth.MapPost("/refresh", async (RefreshRequest request, IAuthService service, CancellationToken cancellationToken) =>
{
    var result = await service.RefreshAsync(request.RefreshToken, cancellationToken);
    return result.Status == AuthStatus.Success ? Results.Ok(result.Tokens) : Results.Unauthorized();
}).RequireRateLimiting("auth");
auth.MapPost("/logout", async (ClaimsPrincipal user, IAuthService service, CancellationToken cancellationToken) =>
{
    await service.LogoutAsync(Guid.Parse(user.FindFirstValue("sub")!), Guid.Parse(user.FindFirstValue("sid")!), cancellationToken);
    return Results.NoContent();
}).RequireAuthorization();
auth.MapGet("/me", (ClaimsPrincipal user) => Results.Ok(new { id = user.FindFirstValue("sub"), email = user.FindFirstValue("email") })).RequireAuthorization();

app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy"
}));

accounts.MapPost(
    "",
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

accounts.MapGet(
    "",
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

accounts.MapGet(
    "/{id:guid}",
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

accounts.MapPut(
    "/{id:guid}",
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

accounts.MapDelete(
    "/{id:guid}",
    async (
        Guid id,
        DeleteAccountHandler handler,
        CancellationToken cancellationToken
    ) =>
    {
        var command = new DeleteAccountCommand(id);

        bool deleted;
        try { deleted = await handler.HandleAsync(command, cancellationToken); }
        catch (FinTrack.Application.Accounts.AccountInUseException exception)
        { return Results.Conflict(new { message = exception.Message }); }

        if (!deleted)
        {
            return Results.NotFound();
        }

        return Results.NoContent();
    }
);

creditCards.MapPost(
    "",
    async (
        CreateCreditCardRequest request,
        CreateCreditCardCommandValidator validator,
        CreateCreditCardHandler handler,
        CancellationToken cancellationToken
    ) =>
    {
        var command = new CreateCreditCardCommand(request.Name, request.Limit, request.ClosingDay, request.DueDay);
        var errors = validator.Validate(command);

        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var card = await handler.HandleAsync(command, cancellationToken);

        return Results.Created($"/credit-cards/{card.Id}", card);
    }
);

creditCards.MapGet(
    "",
    async (
        ListCreditCardsHandler handler,
        CancellationToken cancellationToken
    ) =>
    {
        var query = new ListCreditCardsQuery();

        var creditCards = await handler.HandleAsync(query, cancellationToken);

        return Results.Ok(creditCards);
    }
);

creditCards.MapGet(
    "/{id:guid}",
    async (
        Guid id,
        GetCreditCardHandler handler,
        CancellationToken cancellationToken
    ) =>
    {
        var query = new GetCreditCardQuery(id);

        var card = await handler.HandleAsync(
            query,
            cancellationToken
        );

        if (card is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(card);
    }
);

creditCards.MapPut(
    "/{id:guid}",
    async (
        Guid id,
        UpdateCreditCardRequest request,
        UpdateCreditCardCommandValidator validator,
        UpdateCreditCardHandler handler,
        CancellationToken cancellationToken
    ) =>
    {
        var command = new UpdateCreditCardCommand(
            id,
            request.Name, request.Limit, request.ClosingDay, request.DueDay
        );

        var errors = validator.Validate(command);

        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var card = await handler.HandleAsync(
            command,
            cancellationToken
        );

        if (card is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(card);
    }
);

creditCards.MapDelete(
    "/{id:guid}",
    async (
        Guid id,
        DeleteCreditCardHandler handler,
        CancellationToken cancellationToken
    ) =>
    {
        var command = new DeleteCreditCardCommand(id);

        bool deleted;
        try { deleted = await handler.HandleAsync(command, cancellationToken); }
        catch (InvalidOperationException exception) when (exception.Message == "Cartão com compras não pode ser excluído.")
        { return Results.Conflict(new { message = exception.Message }); }

        if (!deleted)
        {
            return Results.NotFound();
        }

        return Results.NoContent();
    }
);

creditCards.MapPost("/{id:guid}/purchases", async (Guid id, CreatePurchaseCommand request,
    PurchaseService service, CreatePurchaseCommandValidator validator, CancellationToken cancellationToken) =>
{
    var errors = validator.Validate(request);
    if (errors.Count > 0) return Results.ValidationProblem(errors);
    var result = await service.CreateAsync(id, request, cancellationToken);
    return result.Status switch
    {
        PurchaseStatus.Created => Results.Created($"/credit-cards/{id}/purchases", result.Purchase),
        PurchaseStatus.Replayed => Results.Ok(result.Purchase),
        PurchaseStatus.NotFound => Results.NotFound(),
        _ => Results.Conflict(new { message = "Chave de idempotência já usada com outra compra." })
    };
});
creditCards.MapGet("/{id:guid}/purchases", async (Guid id, PurchaseService service, CancellationToken cancellationToken) =>
{
    var purchases = await service.ListAsync(id, cancellationToken);
    return purchases is null ? Results.NotFound() : Results.Ok(purchases);
});

creditCards.MapGet("/{id:guid}/invoices", async (Guid id, InvoiceService service, CancellationToken cancellationToken) =>
{
    var invoices = await service.ListAsync(id, cancellationToken);
    return invoices is null ? Results.NotFound() : Results.Ok(invoices);
});
creditCards.MapGet("/{id:guid}/invoices/summary", async (Guid id, DateOnly? asOf, InvoiceService service, CancellationToken cancellationToken) =>
{
    var errors = InvoiceService.ValidateDate(asOf);
    if (errors.Count > 0) return Results.ValidationProblem(errors);
    var summary = await service.SummaryAsync(id, asOf, cancellationToken);
    return summary is null ? Results.NotFound() : Results.Ok(summary);
});
creditCards.MapGet("/{id:guid}/invoices/{invoiceId:guid}", async (Guid id, Guid invoiceId, InvoiceService service, CancellationToken cancellationToken) =>
{
    var invoice = (await service.ListAsync(id, cancellationToken))?.FirstOrDefault(item => item.Id == invoiceId);
    return invoice is null ? Results.NotFound() : Results.Ok(invoice);
});

var recurring = app.MapGroup("/recurring-transactions").RequireAuthorization();
recurring.MapPost("", async (CreateRecurringTransactionCommand request, RecurringTransactionService service, CancellationToken cancellationToken) =>
{
    var errors = RecurringTransactionService.Validate(request);
    if (errors.Count > 0) return Results.ValidationProblem(errors);
    var item = await service.CreateAsync(request, cancellationToken);
    return item is null ? Results.NotFound() : Results.Created($"/recurring-transactions/{item.Id}", item);
});
recurring.MapGet("", async (RecurringTransactionService service, CancellationToken cancellationToken)
    => Results.Ok(await service.ListAsync(cancellationToken)));
recurring.MapGet("/{id:guid}", async (Guid id, RecurringTransactionService service, CancellationToken cancellationToken) =>
{
    var item = await service.GetAsync(id, cancellationToken);
    return item is null ? Results.NotFound() : Results.Ok(item);
});
recurring.MapPost("/{id:guid}/deactivate", async (Guid id, RecurringTransactionService service, CancellationToken cancellationToken) =>
{
    var item = await service.DeactivateAsync(id, cancellationToken);
    return item is null ? Results.NotFound() : Results.Ok(item);
});
recurring.MapPost("/generate", async (GenerateRecurringTransactionsRequest request, RecurringTransactionService service, CancellationToken cancellationToken) =>
{
    var throughDate = request.ThroughDate ?? service.Today;
    var errors = service.ValidateThroughDate(throughDate);
    if (errors.Count > 0) return Results.ValidationProblem(errors);
    return Results.Ok(await service.GenerateAsync(throughDate, cancellationToken));
});
app.MapGet("/transactions", async (RecurringTransactionService service, CancellationToken cancellationToken)
    => Results.Ok(await service.ListTransactionsAsync(cancellationToken))).RequireAuthorization();

app.MapPost("/transactions", async (CreateTransactionCommand request, TransactionService service, CancellationToken cancellationToken) =>
{
    var errors = service.Validate(request);
    if (errors.Count > 0) return Results.ValidationProblem(errors);
    var transaction = await service.CreateAsync(request, cancellationToken);
    return transaction is null ? Results.NotFound() : Results.Created("/transactions", transaction);
}).RequireAuthorization();

app.MapGet("/dashboard/summary", async (DateOnly? asOf, DashboardService service, CancellationToken cancellationToken) =>
{
    var errors = DashboardService.Validate(asOf);
    if (errors.Count > 0) return Results.ValidationProblem(errors);
    return Results.Ok(await service.SummaryAsync(asOf, cancellationToken));
}).RequireAuthorization();

categories.MapPost(
    "",
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

categories.MapGet(
    "",
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

categories.MapGet(
    "/{id:guid}",
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

categories.MapPut(
    "/{id:guid}",
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
