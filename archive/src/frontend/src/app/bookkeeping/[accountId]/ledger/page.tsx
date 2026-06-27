"use client";

import { useEffect, useState } from "react";
import { useRouter, useParams, useSearchParams } from "next/navigation";
import Link from "next/link";
import { getAccount, getLedger, type Account, type LedgerEntry } from "@/lib/api";

const INR = (n: number) =>
  new Intl.NumberFormat("en-IN", { style: "currency", currency: "INR", maximumFractionDigits: 0 }).format(n);

const DATE_FMT = (iso: string) =>
  new Date(iso).toLocaleDateString("en-IN", { day: "2-digit", month: "short", year: "numeric" });

export default function LedgerPage() {
  const router = useRouter();
  const { accountId } = useParams<{ accountId: string }>();
  const searchParams = useSearchParams();

  const initialAsOf = searchParams.get("as_of") ?? "";
  const [asOf, setAsOf] = useState(initialAsOf ? initialAsOf.slice(0, 10) : "");
  const [account, setAccount] = useState<Account | null>(null);
  const [entries, setEntries] = useState<LedgerEntry[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!localStorage.getItem("bookkeeping_token")) {
      router.push("/login");
      return;
    }
    load(asOf);
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [accountId]);

  async function load(date: string) {
    setLoading(true);
    setError(null);
    const asOfParam = date ? `${date}T23:59:59Z` : undefined;
    try {
      const [acct, ledger] = await Promise.all([
        getAccount(accountId, asOfParam),
        getLedger(accountId, asOfParam),
      ]);
      setAccount(acct);
      setEntries(ledger);
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setLoading(false);
    }
  }

  // Compute running balance
  let running = 0;
  const rows = entries.map(e => {
    running += e.amount;
    return { ...e, running };
  });

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Header */}
      <header className="bg-white border-b border-gray-200 px-6 py-4">
        <div className="max-w-5xl mx-auto flex items-center justify-between">
          <div>
            <Link href="/bookkeeping" className="text-xs text-gray-400 hover:underline">
              ← Accounts
            </Link>
            <h1 className="text-lg font-semibold text-gray-900 mt-0.5">
              {account ? account.displayName : "Ledger"}
            </h1>
            {account && (
              <p className="text-xs text-gray-400">
                {account.categoryName} · {account.accountCode}
              </p>
            )}
          </div>
          {account && (
            <div className="text-right">
              <p className="text-xs text-gray-400">Balance</p>
              <p className={`text-2xl font-bold tabular-nums ${account.balance < 0 ? "text-red-600" : "text-gray-900"}`}>
                {INR(account.balance)}
              </p>
            </div>
          )}
        </div>
      </header>

      <main className="max-w-5xl mx-auto px-6 py-8">
        {/* Date filter */}
        <div className="flex items-center gap-3 mb-6">
          <span className="text-sm font-medium text-gray-700">View as of:</span>
          <input
            type="date"
            value={asOf}
            onChange={e => setAsOf(e.target.value)}
            className="border border-gray-300 rounded px-3 py-1.5 text-sm focus:outline-none focus:ring-1 focus:ring-blue-500"
          />
          <button
            onClick={() => load(asOf)}
            className="px-3 py-1.5 bg-blue-600 text-white text-sm rounded hover:bg-blue-700 transition-colors"
          >
            Apply
          </button>
          {asOf && (
            <button
              onClick={() => { setAsOf(""); load(""); }}
              className="text-sm text-gray-400 hover:text-gray-700 underline"
            >
              Clear
            </button>
          )}
        </div>

        {error && (
          <div className="mb-4 text-sm text-red-700 bg-red-50 border border-red-200 rounded px-3 py-2">
            {error}
          </div>
        )}

        {loading ? (
          <p className="text-sm text-gray-400">Loading ledger…</p>
        ) : entries.length === 0 ? (
          <div className="text-center py-16 text-gray-400 bg-white border border-gray-200 rounded-lg">
            <p className="text-sm">No transactions{asOf ? ` up to ${asOf}` : ""} for this account.</p>
          </div>
        ) : (
          <div className="bg-white border border-gray-200 rounded-lg overflow-hidden">
            <table className="w-full text-sm">
              <thead className="bg-gray-50 border-b border-gray-100">
                <tr>
                  <th className="px-4 py-2.5 text-left text-xs font-medium text-gray-500 w-32">Date</th>
                  <th className="px-4 py-2.5 text-left text-xs font-medium text-gray-500">Description</th>
                  <th className="px-4 py-2.5 text-xs font-medium text-gray-500 w-24 text-center">Ref</th>
                  <th className="px-4 py-2.5 text-right text-xs font-medium text-gray-500 w-28">Amount</th>
                  <th className="px-4 py-2.5 text-right text-xs font-medium text-gray-500 w-28">Balance</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-50">
                {rows.map(row => (
                  <tr key={row.ledgerEntryId} className="hover:bg-gray-50 transition-colors">
                    <td className="px-4 py-3 text-xs text-gray-500 tabular-nums">{DATE_FMT(row.postingTimestamp)}</td>
                    <td className="px-4 py-3 text-gray-800">{row.description}</td>
                    <td className="px-4 py-3 text-center font-mono text-xs text-gray-400">{row.referenceCode ?? "—"}</td>
                    <td className={`px-4 py-3 text-right tabular-nums font-medium ${row.amount < 0 ? "text-red-600" : "text-green-700"}`}>
                      {row.amount < 0 ? `(${INR(Math.abs(row.amount))})` : INR(row.amount)}
                    </td>
                    <td className={`px-4 py-3 text-right tabular-nums font-medium ${row.running < 0 ? "text-red-600" : "text-gray-900"}`}>
                      {INR(row.running)}
                    </td>
                  </tr>
                ))}
              </tbody>
              <tfoot className="bg-gray-50 border-t border-gray-100">
                <tr>
                  <td colSpan={3} className="px-4 py-2.5 text-xs font-semibold text-gray-600">
                    {entries.length} transaction{entries.length !== 1 ? "s" : ""}
                  </td>
                  <td className="px-4 py-2.5 text-right font-semibold text-sm text-gray-700 tabular-nums">
                    {INR(entries.reduce((s, e) => s + e.amount, 0))}
                  </td>
                  <td className={`px-4 py-2.5 text-right font-bold text-sm tabular-nums ${(account?.balance ?? 0) < 0 ? "text-red-600" : "text-gray-900"}`}>
                    {INR(account?.balance ?? 0)}
                  </td>
                </tr>
              </tfoot>
            </table>
          </div>
        )}
      </main>
    </div>
  );
}
