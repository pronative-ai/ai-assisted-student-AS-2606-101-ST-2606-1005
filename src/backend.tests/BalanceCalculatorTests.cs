using Bookkeeping.Domain;

namespace Bookkeeping.Tests;

public class BalanceCalculatorTests
{
    private static LedgerEntry Entry(string id, DateTime date, decimal amount) =>
        new(id, "ACC-TEST", date, amount, "Test entry", null, date);

    private readonly BalanceCalculator _calc = new();

    [Fact]
    public void EmptyLedger_ReturnsZeroBalance()
    {
        var balance = _calc.ComputeBalance([]);
        Assert.Equal(0m, balance);
    }

    [Fact]
    public void CurrentBalance_SumsAllEntries()
    {
        var entries = new[]
        {
            Entry("LE-01", new DateTime(2026, 1, 5), 3000m),
            Entry("LE-02", new DateTime(2026, 2, 5), 3000m),
            Entry("LE-03", new DateTime(2026, 3, 5), 3000m),
        };

        Assert.Equal(9000m, _calc.ComputeBalance(entries));
    }

    [Fact]
    public void HistoricalCutoff_ExcludesEntriesAfterCutoff()
    {
        var entries = new[]
        {
            Entry("LE-01", new DateTime(2026, 1, 5), 3000m),
            Entry("LE-02", new DateTime(2026, 2, 5), 3000m),
            Entry("LE-03", new DateTime(2026, 3, 5), 3000m),  // after cutoff
        };

        var cutoff = new DateTime(2026, 2, 28, 23, 59, 59);
        Assert.Equal(6000m, _calc.ComputeBalance(entries, cutoff));
    }

    [Fact]
    public void HistoricalCutoff_IncludesEntryExactlyAtCutoff()
    {
        var cutoff = new DateTime(2026, 2, 28, 23, 59, 59);
        var entries = new[]
        {
            Entry("LE-01", new DateTime(2026, 1, 5), 3000m),
            Entry("LE-02", cutoff, 3000m),  // exactly at cutoff boundary
        };

        Assert.Equal(6000m, _calc.ComputeBalance(entries, cutoff));
    }

    [Fact]
    public void ZeroBalance_AccountWithOffsetEntries()
    {
        var entries = new[]
        {
            Entry("LE-01", new DateTime(2026, 1, 1), 5000m),
            Entry("LE-02", new DateTime(2026, 1, 2), -5000m),
        };

        Assert.Equal(0m, _calc.ComputeBalance(entries));
    }

    [Fact]
    public void FilterByDate_OrdersByTimestampThenId()
    {
        // Two entries on the same day — deterministic tie-break by ledger entry ID
        var entries = new[]
        {
            Entry("LE-02", new DateTime(2026, 1, 5), 200m),
            Entry("LE-01", new DateTime(2026, 1, 5), 100m),
        };

        var ordered = _calc.FilterByDate(entries).ToList();

        Assert.Equal("LE-01", ordered[0].LedgerEntryId);
        Assert.Equal("LE-02", ordered[1].LedgerEntryId);
    }

    [Fact]
    public void NoCutoff_AllEntriesIncluded()
    {
        var entries = new[]
        {
            Entry("LE-01", new DateTime(2020, 1, 1), 1000m),
            Entry("LE-02", new DateTime(2030, 1, 1), 1000m),  // far future
        };

        Assert.Equal(2000m, _calc.ComputeBalance(entries, cutoff: null));
    }

    [Fact]
    public void CutoffBeforeAllEntries_ReturnsZero()
    {
        var entries = new[]
        {
            Entry("LE-01", new DateTime(2026, 6, 1), 3000m),
        };

        var cutoff = new DateTime(2025, 12, 31);
        Assert.Equal(0m, _calc.ComputeBalance(entries, cutoff));
    }

    [Fact]
    public void NegativeEntries_ComputeCorrectly()
    {
        var entries = new[]
        {
            Entry("LE-01", new DateTime(2026, 1, 10),  30000m),
            Entry("LE-02", new DateTime(2026, 1, 31), -17900m),
        };

        Assert.Equal(12100m, _calc.ComputeBalance(entries));
    }
}
