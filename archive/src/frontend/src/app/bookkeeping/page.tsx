"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { getAccounts, logout, type Account } from "@/lib/api";

const INR = (n: number) =>
  new Intl.NumberFormat("en-IN", { style: "currency", currency: "INR", maximumFractionDigits: 0 }).format(n);

function toAsOfParam(date: string) {
  return date ? `${date}T23:59:59Z` : undefined;
}

export default function BookkeepingPage() {
  const router = useRouter();
  const [accounts, setAccounts] = useState<Account[]>([]);
  const [asOf, setAsOf] = useState("");
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!localStorage.getItem("bookkeeping_token")) {
      router.push("/login");
      return;
    }
    load();
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  async function load(date?: string) {
    setLoading(true);
    setError(null);
    try {
      const data = await getAccounts(toAsOfParam(date ?? ""));
      setAccounts(data);
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setLoading(false);
    }
  }

  function handleSignOut() {
    logout();
    router.push("/login");
  }

  // Group by category name in order returned from API (server sorts by sortOrder)
  const groups = accounts.reduce<Record<string, Account[]>>((acc, a) => {
    (acc[a.categoryName] ??= []).push(a);
    return acc;
  }, {});

  const categoryOrder = Array.from(new Set(accounts.map(a => a.categoryName)));

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Header */}
      <header className="bg-white border-b border-gray-200 px-6 py-4">
        <div className="max-w-5xl mx-auto flex items-center justify-between">
          <div>
            <h1 className="text-lg font-semibold text-gray-900">Green Park Apartments</h1>
            <p className="text-xs text-gray-400">Community Bookkeeping · intent-2606-101-1005-0001</p>
          </div>
          <div className="flex items-center gap-5">
            <Link href="/bookkeeping/summary" className="text-sm text-blue-600 hover:underline">
              Financial Summary
            </Link>
            <button onClick={handleSignOut} className="text-sm text-gray-500 hover:text-gray-800">
              Sign out
            </button>
          </div>
        </div>
      </header>

      <main className="max-w-5xl mx-auto px-6 py-8">
        {/* Point-in-time filter */}
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
          {asOf && (
            <span className="text-xs text-amber-600 bg-amber-50 border border-amber-200 rounded px-2 py-1">
              Showing balances as of {asOf}
            </span>
          )}
        </div>

        {error && (
          <div className="mb-4 text-sm text-red-700 bg-red-50 border border-red-200 rounded px-3 py-2">
            {error}
          </div>
        )}

        {loading ? (
          <p className="text-sm text-gray-400">Loading accounts…</p>
        ) : (
          categoryOrder.map(catName => {
            const accs = groups[catName];
            const total = accs.reduce((s, a) => s + a.balance, 0);
            return (
              <section key={catName} className="mb-8">
                <h2 className="text-xs font-semibold uppercase tracking-widest text-gray-400 mb-2">
                  {catName}
                </h2>
                <div className="bg-white border border-gray-200 rounded-lg overflow-hidden">
                  <table className="w-full text-sm">
                    <thead className="bg-gray-50 border-b border-gray-100">
                      <tr>
                        <th className="px-4 py-2.5 text-left text-xs font-medium text-gray-500 w-28">Code</th>
                        <th className="px-4 py-2.5 text-left text-xs font-medium text-gray-500">Account</th>
                        <th className="px-4 py-2.5 text-right text-xs font-medium text-gray-500 w-36">Balance</th>
                        <th className="px-4 py-2.5 w-24" />
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-gray-50">
                      {accs.map(acc => (
                        <tr key={acc.accountId} className="hover:bg-gray-50 transition-colors">
                          <td className="px-4 py-3 font-mono text-xs text-gray-400">{acc.accountCode}</td>
                          <td className="px-4 py-3 text-gray-800">{acc.displayName}</td>
                          <td className={`px-4 py-3 text-right font-medium tabular-nums ${acc.balance < 0 ? "text-red-600" : "text-gray-900"}`}>
                            {INR(acc.balance)}
                          </td>
                          <td className="px-4 py-3 text-right">
                            <Link
                              href={`/bookkeeping/${acc.accountId}/ledger${asOf ? `?as_of=${toAsOfParam(asOf)}` : ""}`}
                              className="text-xs text-blue-500 hover:underline"
                            >
                              Ledger →
                            </Link>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                    <tfoot className="bg-gray-50 border-t border-gray-100">
                      <tr>
                        <td colSpan={2} className="px-4 py-2.5 text-xs font-semibold text-gray-600">Total</td>
                        <td className={`px-4 py-2.5 text-right text-sm font-bold tabular-nums ${total < 0 ? "text-red-600" : "text-gray-900"}`}>
                          {INR(total)}
                        </td>
                        <td />
                      </tr>
                    </tfoot>
                  </table>
                </div>
              </section>
            );
          })
        )}
      </main>
    </div>
  );
}
