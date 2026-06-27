namespace Bookkeeping.Infrastructure;

using Bookkeeping.Domain;

public static class SeedData
{
    public static (List<AccountCategory>, List<Account>, List<LedgerEntry>) Generate()
    {
        var categories = new List<AccountCategory>
        {
            new("CAT-INC",  "INCOME",  "Income",        1),
            new("CAT-EXP",  "EXPENSE", "Expense",       2),
            new("CAT-AST",  "ASSET",   "Asset",         3),
            new("CAT-RES",  "RESERVE", "Reserve Fund",  4),
        };

        const string communityId = "COMM-001";

        var accounts = new List<Account>
        {
            // Individual member ledgers
            new("ACC-F01", communityId, "CAT-INC", "INC-F01", "Flat 1 – Maintenance",       true),
            new("ACC-F02", communityId, "CAT-INC", "INC-F02", "Flat 2 – Maintenance",       true),
            new("ACC-F03", communityId, "CAT-INC", "INC-F03", "Flat 3 – Maintenance",       true),
            new("ACC-F04", communityId, "CAT-INC", "INC-F04", "Flat 4 – Maintenance",       true),
            new("ACC-F05", communityId, "CAT-INC", "INC-F05", "Flat 5 – Maintenance",       true),
            new("ACC-F06", communityId, "CAT-INC", "INC-F06", "Flat 6 – Maintenance",       true),
            new("ACC-F07", communityId, "CAT-INC", "INC-F07", "Flat 7 – Maintenance",       true),
            new("ACC-F08", communityId, "CAT-INC", "INC-F08", "Flat 8 – Maintenance",       true),
            new("ACC-F09", communityId, "CAT-INC", "INC-F09", "Flat 9 – Maintenance",       true),
            new("ACC-F10", communityId, "CAT-INC", "INC-F10", "Flat 10 – Maintenance",      true),
            new("ACC-MISC",communityId, "CAT-INC", "INC-MISC","Miscellaneous Income",       true),

            // Expense accounts (vendor ledgers)
            new("ACC-ELEC",  communityId, "CAT-EXP", "EXP-ELEC",  "Electricity Bills",          true),
            new("ACC-WATER", communityId, "CAT-EXP", "EXP-WATER", "Water & Sewage",             true),
            new("ACC-SEC",   communityId, "CAT-EXP", "EXP-SEC",   "Security Services",          true),
            new("ACC-CLEAN", communityId, "CAT-EXP", "EXP-CLEAN", "Cleaning & Housekeeping",    true),
            new("ACC-REP",   communityId, "CAT-EXP", "EXP-REP",   "Maintenance & Repairs",      true),

            // Asset accounts (bank/cash book)
            new("ACC-BANK", communityId, "CAT-AST", "AST-BANK", "Community Bank Account", true),
            new("ACC-CASH", communityId, "CAT-AST", "AST-CASH", "Petty Cash",             true),

            // Reserve
            new("ACC-EMRG", communityId, "CAT-RES", "RES-EMRG", "Emergency Reserve Fund", true),
        };

        var entries = new List<LedgerEntry>();
        int seq = 1;

        LedgerEntry Entry(string accountId, DateTime date, decimal amount, string description, string? refCode = null) =>
            new($"LE-{seq++:D4}", accountId, date, amount, description, refCode, date);

        // ── January 2026 ──────────────────────────────────────────────
        // Flat maintenance payments
        for (int flat = 1; flat <= 10; flat++)
        {
            int day = 3 + (flat % 5); // spread over a few days
            entries.Add(Entry($"ACC-F{flat:D2}", new DateTime(2026, 1, day), 3000m,
                $"Maintenance for January 2026 – Flat {flat}", $"JAN26-F{flat:D2}"));
        }

        // January expenses
        entries.Add(Entry("ACC-ELEC",  new DateTime(2026, 1, 31), 7800m,  "Electricity bill – January 2026",    "ELEC-JAN26"));
        entries.Add(Entry("ACC-WATER", new DateTime(2026, 1, 31), 2100m,  "Water & sewage – January 2026",      "WATER-JAN26"));
        entries.Add(Entry("ACC-SEC",   new DateTime(2026, 1, 31), 5000m,  "Security services – January 2026",   "SEC-JAN26"));
        entries.Add(Entry("ACC-CLEAN", new DateTime(2026, 1, 31), 3000m,  "Cleaning services – January 2026",   "CLEAN-JAN26"));

        // Bank book – Jan (deposit maintenance, then pay bills)
        entries.Add(Entry("ACC-BANK", new DateTime(2026, 1, 10),  30000m, "Maintenance collected – January 2026"));
        entries.Add(Entry("ACC-BANK", new DateTime(2026, 1, 31), -17900m, "Bills paid – January 2026 (elec+water+sec+clean)"));

        // ── February 2026 ─────────────────────────────────────────────
        for (int flat = 1; flat <= 10; flat++)
        {
            int day = 3 + (flat % 5);
            entries.Add(Entry($"ACC-F{flat:D2}", new DateTime(2026, 2, day), 3000m,
                $"Maintenance for February 2026 – Flat {flat}", $"FEB26-F{flat:D2}"));
        }

        entries.Add(Entry("ACC-ELEC",  new DateTime(2026, 2, 28), 8200m, "Electricity bill – February 2026",   "ELEC-FEB26"));
        entries.Add(Entry("ACC-WATER", new DateTime(2026, 2, 28), 2000m, "Water & sewage – February 2026",     "WATER-FEB26"));
        entries.Add(Entry("ACC-SEC",   new DateTime(2026, 2, 28), 5000m, "Security services – February 2026",  "SEC-FEB26"));
        entries.Add(Entry("ACC-CLEAN", new DateTime(2026, 2, 28), 3000m, "Cleaning services – February 2026",  "CLEAN-FEB26"));
        entries.Add(Entry("ACC-REP",   new DateTime(2026, 2, 15), 2500m, "Emergency plumbing repair – Block A", "REP-FEB26-001"));

        entries.Add(Entry("ACC-BANK", new DateTime(2026, 2, 8),   30000m, "Maintenance collected – February 2026"));
        entries.Add(Entry("ACC-BANK", new DateTime(2026, 2, 28), -18200m, "Bills paid – February 2026 (elec+water+sec+clean)"));
        entries.Add(Entry("ACC-CASH", new DateTime(2026, 2, 15),  -2500m, "Emergency plumbing repair payment"));
        entries.Add(Entry("ACC-CASH", new DateTime(2026, 2, 15),   2500m, "Reimbursed from bank for repair"));

        // Miscellaneous income (hall booking)
        entries.Add(Entry("ACC-MISC", new DateTime(2026, 2, 20), 1500m, "Community hall booking – Feb 2026", "MISC-FEB26-001"));
        entries.Add(Entry("ACC-BANK", new DateTime(2026, 2, 20),  1500m, "Hall booking fee received"));

        // ── March 2026 ────────────────────────────────────────────────
        // Flat 10 has not paid March maintenance (pending dues scenario)
        for (int flat = 1; flat <= 9; flat++)
        {
            int day = 3 + (flat % 5);
            entries.Add(Entry($"ACC-F{flat:D2}", new DateTime(2026, 3, day), 3000m,
                $"Maintenance for March 2026 – Flat {flat}", $"MAR26-F{flat:D2}"));
        }

        entries.Add(Entry("ACC-ELEC",  new DateTime(2026, 3, 31), 8100m, "Electricity bill – March 2026",      "ELEC-MAR26"));
        entries.Add(Entry("ACC-WATER", new DateTime(2026, 3, 31), 1900m, "Water & sewage – March 2026",        "WATER-MAR26"));
        entries.Add(Entry("ACC-SEC",   new DateTime(2026, 3, 31), 5000m, "Security services – March 2026",     "SEC-MAR26"));
        entries.Add(Entry("ACC-CLEAN", new DateTime(2026, 3, 31), 3000m, "Cleaning services – March 2026",     "CLEAN-MAR26"));
        entries.Add(Entry("ACC-REP",   new DateTime(2026, 3, 10), 1500m, "Electrician – common area wiring",   "REP-MAR26-001"));

        entries.Add(Entry("ACC-BANK", new DateTime(2026, 3, 8),   27000m, "Maintenance collected – March 2026 (9 flats)"));
        entries.Add(Entry("ACC-BANK", new DateTime(2026, 3, 31), -18000m, "Bills paid – March 2026 (elec+water+sec+clean)"));
        entries.Add(Entry("ACC-CASH", new DateTime(2026, 3, 10),  -1500m, "Electrician payment – common area"));
        entries.Add(Entry("ACC-CASH", new DateTime(2026, 3, 10),   1500m, "Reimbursed from bank for electrician"));

        // Emergency reserve – seeded with opening balance
        entries.Add(Entry("ACC-EMRG", new DateTime(2026, 1, 1), 50000m, "Opening balance – Emergency Reserve Fund", "OPEN-RES"));
        entries.Add(Entry("ACC-EMRG", new DateTime(2026, 2, 28), 5000m, "Monthly contribution – February 2026"));
        entries.Add(Entry("ACC-EMRG", new DateTime(2026, 3, 31), 5000m, "Monthly contribution – March 2026"));

        return (categories, accounts, entries);
    }
}
