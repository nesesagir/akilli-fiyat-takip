const USER_KEY = "pricetracker.userId";
const NAME_KEY = "pricetracker.displayName";
const EMAIL_KEY = "pricetracker.email";
const LANG_KEY = "pricetracker.preferredLanguage";

export function getStoredUserId(): string | null {
  if (typeof window === "undefined") return null;
  return localStorage.getItem(USER_KEY);
}

export function getStoredDisplayName(): string | null {
  if (typeof window === "undefined") return null;
  return localStorage.getItem(NAME_KEY);
}

export function getStoredLanguage(): string | null {
  if (typeof window === "undefined") return null;
  return localStorage.getItem(LANG_KEY);
}

export function setStoredLanguage(preferredLanguage: string) {
  localStorage.setItem(
    LANG_KEY,
    preferredLanguage === "en" ? "en" : "tr"
  );
}

export function setStoredUser(
  id: string,
  displayName: string,
  email: string,
  preferredLanguage?: string
) {
  localStorage.setItem(USER_KEY, id);
  localStorage.setItem(NAME_KEY, displayName);
  localStorage.setItem(EMAIL_KEY, email);
  if (preferredLanguage) {
    setStoredLanguage(preferredLanguage);
  }
}

export function setStoredUserId(id: string) {
  localStorage.setItem(USER_KEY, id);
}

export function clearStoredUserId() {
  localStorage.removeItem(USER_KEY);
  localStorage.removeItem(NAME_KEY);
  localStorage.removeItem(EMAIL_KEY);
  localStorage.removeItem(LANG_KEY);
}

export function formatMoney(value?: number | null, currency = "TRY") {
  if (value == null || Number.isNaN(value)) return "—";
  return new Intl.NumberFormat("tr-TR", {
    style: "currency",
    currency,
    maximumFractionDigits: 2,
  }).format(value);
}
