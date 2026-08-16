"use client";

import { FormEvent, useEffect, useState } from "react";
import { AnimatePresence, motion } from "framer-motion";
import { api } from "@/lib/api";
import { setStoredUser } from "@/lib/format";
import { normalizeLang } from "@/lib/i18n";
import type { UserDto } from "@/lib/types";
import { useLanguage } from "./LanguageProvider";

type Tab = "profile" | "prefs";

type Props = {
  open: boolean;
  userId: string;
  onClose: () => void;
  onSaved: (user: UserDto) => void;
};

export function AccountPanel({ open, userId, onClose, onSaved }: Props) {
  const { t, lang, setLang } = useLanguage();
  const [tab, setTab] = useState<Tab>("profile");
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [ok, setOk] = useState<string | null>(null);

  const [firstName, setFirstName] = useState("");
  const [lastName, setLastName] = useState("");
  const [email, setEmail] = useState("");
  const [currency, setCurrency] = useState("TRY");
  const [language, setLanguage] = useState(lang);
  const [notify, setNotify] = useState(true);

  useEffect(() => {
    if (!open) return;
    setError(null);
    setOk(null);
    setTab("profile");
    setLoading(true);
    api
      .getUser(userId)
      .then((u) => {
        setFirstName(u.firstName || "");
        setLastName(u.lastName || "");
        setEmail(u.email);
        setCurrency(u.preferredCurrency || "TRY");
        const next = normalizeLang(u.preferredLanguage);
        setLanguage(next);
        setLang(next);
        setNotify(u.emailNotificationsEnabled);
      })
      .catch((e) =>
        setError(e instanceof Error ? e.message : t.loadError)
      )
      .finally(() => setLoading(false));
  }, [open, userId, setLang, t.loadError]);

  async function save(e: FormEvent) {
    e.preventDefault();
    setSaving(true);
    setError(null);
    setOk(null);
    try {
      const updated = await api.updateUser(userId, {
        firstName: firstName.trim(),
        lastName: lastName.trim(),
        email: email.trim(),
        preferredCurrency: currency,
        preferredLanguage: normalizeLang(language),
        emailNotificationsEnabled: notify,
      });
      setStoredUser(
        updated.id,
        updated.displayName,
        updated.email,
        updated.preferredLanguage
      );
      const next = normalizeLang(updated.preferredLanguage);
      setLanguage(next);
      setLang(next);
      setOk(t.saved);
      onSaved(updated);
    } catch (err) {
      const raw = err instanceof Error ? err.message : t.saveError;
      setError(
        raw.includes("zaten") || raw.includes("Conflict") || raw.includes("already")
          ? t.emailTaken
          : raw
      );
    } finally {
      setSaving(false);
    }
  }

  return (
    <AnimatePresence>
      {open && (
        <motion.div
          className="fixed inset-0 z-50 flex items-end justify-center bg-black/45 p-4 sm:items-center"
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          exit={{ opacity: 0 }}
          onClick={onClose}
        >
          <motion.div
            initial={{ y: 36, opacity: 0, scale: 0.98 }}
            animate={{ y: 0, opacity: 1, scale: 1 }}
            exit={{ y: 20, opacity: 0 }}
            transition={{ type: "spring", stiffness: 340, damping: 28 }}
            onClick={(e) => e.stopPropagation()}
            className="relative max-h-[90vh] w-full max-w-xl overflow-hidden rounded-[1.75rem] border border-border bg-panel shadow-glass dark:shadow-glass-dark"
          >
            <div className="relative border-b border-border px-5 pt-5 sm:px-6">
              <div className="flex items-start justify-between gap-3">
                <div>
                  <p className="text-[11px] font-semibold uppercase tracking-[0.2em] text-accent">
                    {t.account}
                  </p>
                  <h2 className="mt-1 font-display text-2xl text-foreground">
                    {t.personalize}
                  </h2>
                </div>
                <button
                  type="button"
                  onClick={onClose}
                  className="rounded-xl border border-border px-3 py-1.5 text-sm text-muted transition hover:bg-accentsoft"
                >
                  {t.close}
                </button>
              </div>

              <div className="mt-4 flex gap-1 pb-3">
                {(
                  [
                    ["profile", t.profile],
                    ["prefs", t.prefs],
                  ] as const
                ).map(([id, label]) => (
                  <button
                    key={id}
                    type="button"
                    onClick={() => setTab(id)}
                    className={`rounded-full px-3.5 py-1.5 text-sm font-medium transition ${
                      tab === id
                        ? "bg-accent text-white dark:text-[#0b0f14]"
                        : "text-muted hover:bg-accentsoft hover:text-foreground"
                    }`}
                  >
                    {label}
                  </button>
                ))}
              </div>
            </div>

            <div className="relative max-h-[min(60vh,520px)] overflow-y-auto px-5 py-5 sm:px-6">
              {loading ? (
                <p className="text-sm text-muted">{t.loading}</p>
              ) : (
                <form onSubmit={save} className="space-y-5">
                  {tab === "profile" && (
                    <div className="space-y-4">
                      <div className="grid gap-3 sm:grid-cols-2">
                        <label className="block">
                          <span className="mb-1.5 block text-xs text-muted">
                            {t.firstName}
                          </span>
                          <input
                            required
                            value={firstName}
                            onChange={(e) => setFirstName(e.target.value)}
                            className="field-input"
                          />
                        </label>
                        <label className="block">
                          <span className="mb-1.5 block text-xs text-muted">
                            {t.lastName}
                          </span>
                          <input
                            required
                            value={lastName}
                            onChange={(e) => setLastName(e.target.value)}
                            className="field-input"
                          />
                        </label>
                      </div>
                      <label className="block">
                        <span className="mb-1.5 block text-xs text-muted">
                          {t.email}
                        </span>
                        <input
                          required
                          type="email"
                          value={email}
                          onChange={(e) => setEmail(e.target.value)}
                          className="field-input"
                        />
                      </label>
                    </div>
                  )}

                  {tab === "prefs" && (
                    <div className="space-y-4">
                      <label className="block">
                        <span className="mb-1.5 block text-xs text-muted">
                          {t.currency}
                        </span>
                        <select
                          value={currency}
                          onChange={(e) => setCurrency(e.target.value)}
                          className="field-input"
                        >
                          <option value="TRY">TRY</option>
                          <option value="USD">USD</option>
                          <option value="EUR">EUR</option>
                          <option value="GBP">GBP</option>
                        </select>
                      </label>
                      <label className="block">
                        <span className="mb-1.5 block text-xs text-muted">
                          {t.language}
                        </span>
                        <select
                          value={language}
                          onChange={(e) => {
                            const next = normalizeLang(e.target.value);
                            setLanguage(next);
                            setLang(next);
                          }}
                          className="field-input"
                        >
                          <option value="tr">{t.langTr}</option>
                          <option value="en">{t.langEn}</option>
                        </select>
                      </label>
                      <label className="flex cursor-pointer items-center justify-between gap-4 rounded-2xl border border-border bg-accentsoft/50 px-4 py-3">
                        <div>
                          <p className="text-sm font-medium text-foreground">
                            {t.notifications}
                          </p>
                          <p className="mt-0.5 text-xs text-muted">
                            {t.notificationsHint}
                          </p>
                        </div>
                        <input
                          type="checkbox"
                          checked={notify}
                          onChange={(e) => setNotify(e.target.checked)}
                          className="h-5 w-5 accent-[var(--accent)]"
                        />
                      </label>
                    </div>
                  )}

                  <div className="flex flex-wrap items-center gap-3 pt-1">
                    <button
                      type="submit"
                      disabled={saving}
                      className="rounded-xl bg-accent px-4 py-2.5 text-sm font-semibold text-white transition hover:opacity-95 disabled:opacity-60 dark:text-[#0b0f14]"
                    >
                      {saving ? t.saving : t.save}
                    </button>
                    {ok && <span className="text-sm text-fall">{ok}</span>}
                    {error && <span className="text-sm text-rise">{error}</span>}
                  </div>
                </form>
              )}
            </div>
          </motion.div>
        </motion.div>
      )}
    </AnimatePresence>
  );
}
