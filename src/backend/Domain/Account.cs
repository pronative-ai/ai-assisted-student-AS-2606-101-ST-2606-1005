namespace Bookkeeping.Domain;

public record Account(
    string AccountId,
    string CommunityId,
    string CategoryId,
    string AccountCode,
    string DisplayName,
    bool IsActive);
