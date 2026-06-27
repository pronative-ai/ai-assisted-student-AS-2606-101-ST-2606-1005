using Bookkeeping.Domain;
using Bookkeeping.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// CORS
var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:3000"];

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// Domain & infrastructure services
builder.Services.AddSingleton<IBookkeepingRepository, InMemoryBookkeepingRepository>();
builder.Services.AddSingleton<BalanceCalculator>();
builder.Services.AddSingleton<TokenStore>();

var app = builder.Build();

app.UseCors();

// ── Auth middleware ────────────────────────────────────────────────────────────
app.Use(async (context, next) =>
{
    var path = context.Request.Path;

    // Public endpoints
    if (path.StartsWithSegments("/api/ping") || path.StartsWithSegments("/api/auth"))
    {
        await next(context);
        return;
    }

    if (path.StartsWithSegments("/api"))
    {
        var authHeader = context.Request.Headers.Authorization.ToString();
        if (!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "Unauthorized" });
            return;
        }

        var token = authHeader["Bearer ".Length..].Trim();
        var store = context.RequestServices.GetRequiredService<TokenStore>();
        if (!store.IsValid(token))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "Unauthorized" });
            return;
        }
    }

    await next(context);
});

// ── Public ────────────────────────────────────────────────────────────────────
app.MapGet("/api/ping", () =>
    Results.Ok(new { message = "pong", timestamp = DateTime.UtcNow }));

app.MapPost("/api/auth/token", (LoginRequest req, IConfiguration config, TokenStore store) =>
{
    var users = config.GetSection("DemoUsers").Get<DemoUser[]>() ?? [];
    var user = Array.Find(users, u => u.Username == req.Username && u.Password == req.Password);
    if (user is null)
        return Results.Unauthorized();

    var token = store.Issue(user.Username, user.Role);
    return Results.Ok(new { token, username = user.Username, role = user.Role });
});

// ── Bookkeeping API (protected) ───────────────────────────────────────────────
app.MapGet("/api/account-categories", (IBookkeepingRepository repo) =>
    Results.Ok(repo.GetCategories()));

app.MapGet("/api/accounts", (IBookkeepingRepository repo, BalanceCalculator calc, string? as_of) =>
{
    var cutoff = ParseCutoff(as_of);
    var result = repo.GetAccounts()
        .Select(a => ToAccountDto(a, repo, calc, cutoff));
    return Results.Ok(result);
});

app.MapGet("/api/accounts/{accountId}", (string accountId, IBookkeepingRepository repo, BalanceCalculator calc, string? as_of) =>
{
    var account = repo.GetAccount(accountId);
    if (account is null) return Results.NotFound();
    return Results.Ok(ToAccountDto(account, repo, calc, ParseCutoff(as_of)));
});

app.MapGet("/api/accounts/{accountId}/ledger", (string accountId, IBookkeepingRepository repo, BalanceCalculator calc, string? as_of) =>
{
    if (repo.GetAccount(accountId) is null) return Results.NotFound();
    var entries = calc.FilterByDate(repo.GetLedgerEntries(accountId), ParseCutoff(as_of));
    return Results.Ok(entries.Select(ToLedgerDto));
});

app.MapGet("/api/financial-summary", (IBookkeepingRepository repo, BalanceCalculator calc, string? as_of) =>
{
    var cutoff = ParseCutoff(as_of);
    var categories = repo.GetCategories().ToList();
    var accounts = repo.GetAccounts().ToList();

    var byCategory = categories.Select(cat =>
    {
        var total = accounts
            .Where(a => a.CategoryId == cat.AccountCategoryId)
            .Sum(a => calc.ComputeBalance(repo.GetLedgerEntries(a.AccountId), cutoff));
        return new CategorySummaryDto(cat.Code, cat.DisplayName, total);
    }).ToList();

    var totalIncome = byCategory.Where(s => s.Code == "INCOME").Sum(s => s.Total);
    var totalExpense = byCategory.Where(s => s.Code == "EXPENSE").Sum(s => s.Total);

    return Results.Ok(new FinancialSummaryDto(
        byCategory,
        totalIncome,
        totalExpense,
        totalIncome - totalExpense,
        cutoff ?? DateTime.UtcNow));
});

app.Run();

// ── Helpers ───────────────────────────────────────────────────────────────────
static DateTime? ParseCutoff(string? asOf) =>
    asOf is not null
        ? DateTime.Parse(asOf, null, System.Globalization.DateTimeStyles.RoundtripKind)
        : null;

static AccountDto ToAccountDto(Account a, IBookkeepingRepository repo, BalanceCalculator calc, DateTime? cutoff) =>
    new(a.AccountId, a.AccountCode, a.DisplayName, a.CategoryId,
        repo.GetCategoryName(a.CategoryId), a.IsActive,
        calc.ComputeBalance(repo.GetLedgerEntries(a.AccountId), cutoff));

static LedgerEntryDto ToLedgerDto(LedgerEntry e) =>
    new(e.LedgerEntryId, e.PostingTimestamp, e.Description, e.Amount, e.ReferenceCode);

// ── DTOs & config types ───────────────────────────────────────────────────────
record LoginRequest(string Username, string Password);
record DemoUser(string Username, string Password, string Role);
record AccountDto(
    string AccountId, string AccountCode, string DisplayName,
    string CategoryId, string CategoryName, bool IsActive, decimal Balance);
record LedgerEntryDto(
    string LedgerEntryId, DateTime PostingTimestamp,
    string Description, decimal Amount, string? ReferenceCode);
record CategorySummaryDto(string Code, string DisplayName, decimal Total);
record FinancialSummaryDto(
    List<CategorySummaryDto> ByCategory,
    decimal TotalIncome, decimal TotalExpense, decimal NetBalance, DateTime AsOf);
