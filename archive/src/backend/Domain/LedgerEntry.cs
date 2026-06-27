namespace Bookkeeping.Domain;

public record LedgerEntry(
    string LedgerEntryId,
    string AccountId,
    DateTime PostingTimestamp,
    decimal Amount,
    string Description,
    string? ReferenceCode,
    DateTime CreatedAt);
