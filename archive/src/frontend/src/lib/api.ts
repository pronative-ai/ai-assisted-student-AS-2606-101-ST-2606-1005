const API_URL = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000";

function getToken(): string | null {
  if (typeof window === "undefined") return null;
  return localStorage.getItem("bookkeeping_token");
}

async function apiFetch<T>(path: string): Promise<T> {
  const token = getToken();
  const res = await fetch(`${API_URL}${path}`, {
    headers: token ? { Authorization: `Bearer ${token}` } : {},
  });

  if (res.status === 401) {
    if (typeof window !== "undefined") {
      localStorage.removeItem("bookkeeping_token");
      window.location.href = "/login";
    }
    throw new Error("Unauthorized");
  }

  if (!res.ok) throw new Error(`Request failed: ${res.status}`);
  return res.json() as Promise<T>;
}

export async function login(username: string, password: string) {
  const res = await fetch(`${API_URL}/api/auth/token`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ username, password }),
  });
  if (!res.ok) throw new Error("Invalid credentials");
  const data = (await res.json()) as { token: string; username: string; role: string };
  localStorage.setItem("bookkeeping_token", data.token);
  return data;
}

export function logout() {
  localStorage.removeItem("bookkeeping_token");
}

export async function getAccounts(asOf?: string) {
  const q = asOf ? `?as_of=${encodeURIComponent(asOf)}` : "";
  return apiFetch<Account[]>(`/api/accounts${q}`);
}

export async function getAccount(accountId: string, asOf?: string) {
  const q = asOf ? `?as_of=${encodeURIComponent(asOf)}` : "";
  return apiFetch<Account>(`/api/accounts/${accountId}${q}`);
}

export async function getLedger(accountId: string, asOf?: string) {
  const q = asOf ? `?as_of=${encodeURIComponent(asOf)}` : "";
  return apiFetch<LedgerEntry[]>(`/api/accounts/${accountId}/ledger${q}`);
}

export async function getFinancialSummary(asOf?: string) {
  const q = asOf ? `?as_of=${encodeURIComponent(asOf)}` : "";
  return apiFetch<FinancialSummary>(`/api/financial-summary${q}`);
}

export async function getCategories() {
  return apiFetch<AccountCategory[]>("/api/account-categories");
}

// ── Types ─────────────────────────────────────────────────────────────────────

export interface AccountCategory {
  accountCategoryId: string;
  code: string;
  displayName: string;
  sortOrder: number;
}

export interface Account {
  accountId: string;
  accountCode: string;
  displayName: string;
  categoryId: string;
  categoryName: string;
  isActive: boolean;
  balance: number;
}

export interface LedgerEntry {
  ledgerEntryId: string;
  postingTimestamp: string;
  description: string;
  amount: number;
  referenceCode: string | null;
}

export interface CategorySummary {
  code: string;
  displayName: string;
  total: number;
}

export interface FinancialSummary {
  byCategory: CategorySummary[];
  totalIncome: number;
  totalExpense: number;
  netBalance: number;
  asOf: string;
}
