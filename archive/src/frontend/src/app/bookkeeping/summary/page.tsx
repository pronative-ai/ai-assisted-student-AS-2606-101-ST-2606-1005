"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { getFinancialSummary, logout, type FinancialSummary } from "@/lib/api";

const INR = (n: number) =>
  new Intl.NumberFormat("en-IN", { style: "currency", currency: "INR", maximumFractionDigits: 0 }).format(n);

export default function SummaryPage() {
  const router = useRouter();
  const [summary, setSummary] = useState<FinancialSummary | null>(null);
  const [asOf, setAsOf] = useState("");
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!localStorage.getItem("bookkeeping_token")) {
      router.push("/login");
      return;
    }
    load("");
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  async function load(date: string) {
    setLoading(true);
    setError(null);
    const asOfParam = date ? `${date}T23:59:59Z` : undefined;
    try {
      const data = await getFinancialSummary(asOfParam);
      setSummary(data);
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Header */}
      <header className="bg-white border-b border-gray-200 px-6 py-4">
        <div className="max-w-5xl mx-auto flex items-center justify-between">
          <div>
            <Link href="/bookkeeping" className="text-xs text-gray-400 hover:underline">
              ← Accounts
            </Link>
            <h1 className="text-lg font-semibold text-gray-900 mt-0.5">Financial Summary</h1>
            {summary && (
              <p className="text-xs text-gray-400">
                As of {new Date(summary.asOf).toLocaleDateString("en-IN", { day: "2-digit", month: "long", year: "numeric" })}
              </p>
            )}
          </div>
          <button
            onClick={() => { logout(); router.push("/login"); }}
            className="text-sm text-gray-500 hover:text-gray-800"
          >
            Sign out
          </button>
        </div>
      </header>

      <main className="max-w-5xl mx-auto px-6 py-8">
        {/* Date filter */}
        <div className="flex items-center gap-3 mb-8">
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
          <p className="text-sm text-gray-400">Loading summary…</p>
        ) : summary ? (
          <div className="space-y-6">
            {/* Top KPI cards */}
            <div className="grid grid-cols-3 gap-4">
              <div className="bg-white border border-gray-200 rounded-lg p-5">
                <p className="text-xs font-medium text-gray-400 uppercase tracking-wide">Total Income</p>
                <p className="text-3xl font-bold text-green-700 mt-1 tabular-nums">{INR(summary.totalIncome)}</p>
                <p className="text-xs text-gray-400 mt-1">Maintenance &amp; other collections</p>
              </div>
              <div className="bg-white border border-gray-200 rounded-lg p-5">
                <p className="text-xs font-medium text-gray-400 uppercase tracking-wide">Total Expense</p>
                <p className="text-3xl font-bold text-red-600 mt-1 tabular-nums">{INR(summary.totalExpense)}</p>
                <p className="text-xs text-gray-400 mt-1">Bills, services &amp; repairs</p>
              </div>
              <div className={`border rounded-lg p-5 ${summary.netBalance >= 0 ? "bg-blue-50 border-blue-200" : "bg-red-50 border-red-200"}`}>
                <p className="text-xs font-medium text-gray-500 uppercase tracking-wide">Net Balance</p>
                <p className={`text-3xl font-bold mt-1 tabular-nums ${summary.netBalance >= 0 ? "text-blue-700" : "text-red-700"}`}>
                  {INR(summary.netBalance)}
                </p>
                <p className="text-xs text-gray-400 mt-1">Income minus Expenses</p>
              </div>
            </div>

            {/* Category breakdown */}
            <div className="bg-white border border-gray-200 rounded-lg overflow-hidden">
              <div className="px-4 py-3 border-b border-gray-100">
                <h2 className="text-sm font-semibold text-gray-700">Breakdown by Category</h2>
              </div>
              <table className="w-full text-sm">
                <thead className="bg-gray-50">
                  <tr>
                    <th className="px-4 py-2.5 text-left text-xs font-medium text-gray-500">Category</th>
                    <th className="px-4 py-2.5 text-right text-xs font-medium text-gray-500">Total</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-50">
                  {summary.byCategory.map(cat => (
                    <tr key={cat.code} className="hover:bg-gray-50">
                      <td className="px-4 py-3 text-gray-800">{cat.displayName}</td>
                      <td className={`px-4 py-3 text-right font-medium tabular-nums ${cat.total < 0 ? "text-red-600" : "text-gray-900"}`}>
                        {INR(cat.total)}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            {/* Reconciliation note */}
            <p className="text-xs text-gray-400 text-center">
              Summary totals are derived from the same ledger entries shown in the accounts view.
              Net balance = Total Income − Total Expense.
            </p>
          </div>
        ) : null}
      </main>
    </div>
  );
}
