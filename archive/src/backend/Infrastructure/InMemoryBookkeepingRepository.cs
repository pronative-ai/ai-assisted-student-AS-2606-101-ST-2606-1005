namespace Bookkeeping.Infrastructure;

using Bookkeeping.Domain;

public class InMemoryBookkeepingRepository : IBookkeepingRepository
{
    private readonly List<AccountCategory> _categories;
    private readonly List<Account> _accounts;
    private readonly List<LedgerEntry> _ledgerEntries;

    public InMemoryBookkeepingRepository()
    {
        (_categories, _accounts, _ledgerEntries) = SeedData.Generate();
    }

    public IEnumerable<AccountCategory> GetCategories() =>
        _categories.OrderBy(c => c.SortOrder);

    public IEnumerable<Account> GetAccounts() =>
        _accounts.Where(a => a.IsActive);

    public Account? GetAccount(string accountId) =>
        _accounts.FirstOrDefault(a => a.AccountId == accountId);

    public IEnumerable<LedgerEntry> GetLedgerEntries(string accountId) =>
        _ledgerEntries.Where(e => e.AccountId == accountId);

    public string GetCategoryName(string categoryId) =>
        _categories.FirstOrDefault(c => c.AccountCategoryId == categoryId)?.DisplayName ?? "Unknown";
}
