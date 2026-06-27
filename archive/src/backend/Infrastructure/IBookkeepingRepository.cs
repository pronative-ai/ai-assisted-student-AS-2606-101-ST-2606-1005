namespace Bookkeeping.Infrastructure;

using Bookkeeping.Domain;

public interface IBookkeepingRepository
{
    IEnumerable<AccountCategory> GetCategories();
    IEnumerable<Account> GetAccounts();
    Account? GetAccount(string accountId);
    IEnumerable<LedgerEntry> GetLedgerEntries(string accountId);
    string GetCategoryName(string categoryId);
}
