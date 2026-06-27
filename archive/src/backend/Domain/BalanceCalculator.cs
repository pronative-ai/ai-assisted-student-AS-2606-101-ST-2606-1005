namespace Bookkeeping.Domain;

public class BalanceCalculator
{
    public decimal ComputeBalance(IEnumerable<LedgerEntry> entries, DateTime? cutoff = null)
        => FilterByDate(entries, cutoff).Sum(e => e.Amount);

    public IEnumerable<LedgerEntry> FilterByDate(IEnumerable<LedgerEntry> entries, DateTime? cutoff = null)
    {
        var filtered = cutoff.HasValue
            ? entries.Where(e => e.PostingTimestamp <= cutoff.Value)
            : entries;
        return filtered.OrderBy(e => e.PostingTimestamp).ThenBy(e => e.LedgerEntryId);
    }
}
