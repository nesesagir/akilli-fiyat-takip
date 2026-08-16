"use client";

import { useCallback, useEffect, useState } from "react";
import { AnimatePresence, motion } from "framer-motion";
import { api } from "@/lib/api";
import type { DashboardSummaryDto, TrackedItemDto } from "@/lib/types";
import {
  clearStoredUserId,
  getStoredDisplayName,
  setStoredUser,
} from "@/lib/format";
import { normalizeLang } from "@/lib/i18n";
import { AccountPanel } from "./AccountPanel";
import { AddProductForm } from "./AddProductForm";
import { DashboardInsights } from "./DashboardInsights";
import { DashboardSkeleton } from "./Skeleton";
import { ItemDetailModal } from "./ItemDetailModal";
import { useLanguage } from "./LanguageProvider";
import { ProductCard } from "./ProductCard";
import { ThemeToggle } from "./ThemeToggle";

type Props = {
  userId: string;
  onReset: () => void;
};

export function Dashboard({ userId, onReset }: Props) {
  const { t, setLang } = useLanguage();
  const [data, setData] = useState<DashboardSummaryDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [selected, setSelected] = useState<TrackedItemDto | null>(null);
  const [displayName, setDisplayName] = useState<string | null>(null);
  const [accountOpen, setAccountOpen] = useState(false);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const [summary, u] = await Promise.all([
        api.getDashboard(userId),
        api.getUser(userId).catch(() => null),
      ]);
      setData(summary);
      setDisplayName(u?.displayName ?? getStoredDisplayName());
      if (u) {
        const nextLang = normalizeLang(u.preferredLanguage);
        setLang(nextLang);
        setStoredUser(u.id, u.displayName, u.email, nextLang);
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : t.loadError);
    } finally {
      setLoading(false);
    }
  }, [userId, setLang, t.loadError]);

  useEffect(() => {
    load();
  }, [load]);

  function resetUser() {
    clearStoredUserId();
    onReset();
  }

  async function deleteItem(item: TrackedItemDto) {
    await api.deactivateItem(item.id);
    if (selected?.id === item.id) setSelected(null);
    await load();
  }

  return (
    <div className="relative min-h-screen overflow-hidden">
      <div
        aria-hidden
        className="pointer-events-none absolute inset-0"
        style={{
          background:
            "radial-gradient(ellipse 80% 50% at 10% -10%, var(--glow), transparent 55%), radial-gradient(ellipse 60% 40% at 100% 0%, rgba(148,163,184,0.15), transparent 50%), linear-gradient(180deg, transparent, var(--background))",
        }}
      />

      <div className="relative mx-auto max-w-6xl px-4 py-8 sm:px-6 lg:px-8">
        <header className="mb-8 flex flex-wrap items-center justify-between gap-4">
          <div>
            <p className="text-xs font-medium uppercase tracking-[0.2em] text-accent">
              {t.brand}
            </p>
            <h1 className="font-display text-3xl text-foreground sm:text-4xl">
              {displayName ? `${t.hi}, ${displayName}` : t.yourTracked}
            </h1>
            {displayName && (
              <p className="mt-1 text-sm text-muted">{t.yourTrackedHere}</p>
            )}
          </div>
          <div className="flex flex-wrap items-center gap-2">
            <ThemeToggle />
            <button
              type="button"
              onClick={() => setAccountOpen(true)}
              className="rounded-xl border border-border bg-panel px-3 py-2 text-sm font-medium transition hover:border-accent/40"
            >
              {t.account}
            </button>
            <button
              type="button"
              onClick={load}
              className="rounded-xl border border-border bg-panel px-3 py-2 text-sm"
            >
              {t.refresh}
            </button>
            <button
              type="button"
              onClick={resetUser}
              className="rounded-xl border border-border px-3 py-2 text-sm text-muted"
            >
              {t.signOut}
            </button>
          </div>
        </header>

        {loading && <DashboardSkeleton />}

        {!loading && error && (
          <div className="glass rounded-2xl p-6 text-rise">
            <p className="font-medium">{t.apiFailed}</p>
            <p className="mt-1 text-sm opacity-80">{error}</p>
            <p className="mt-3 text-sm text-muted">{t.apiFailedHint}</p>
          </div>
        )}

        {!loading && !error && data && (
          <>
            <DashboardInsights
              savings={Number(data.potentialMonthlySavings)}
              deal={data.dealOfTheDay}
              onOpenDeal={setSelected}
            />

            <div className="mt-4">
              <AddProductForm userId={userId} onCreated={load} />
            </div>

            <section className="mt-8">
              <h2 className="mb-4 font-display text-2xl text-foreground">
                {t.trackedProducts}
              </h2>
              {data.items.length === 0 ? (
                <div className="glass rounded-2xl p-8 text-center text-muted">
                  {t.noProducts}
                </div>
              ) : (
                <div className="grid gap-5 sm:grid-cols-2 lg:grid-cols-3">
                  <AnimatePresence mode="popLayout">
                    {data.items.map((item, i) => (
                      <motion.div
                        key={item.id}
                        layout
                        initial={{ opacity: 0, y: 16 }}
                        animate={{ opacity: 1, y: 0 }}
                        exit={{ opacity: 0, scale: 0.92, y: -8 }}
                        transition={{ delay: 0.04 * i }}
                        className="min-h-[1px]"
                      >
                        <ProductCard
                          item={item}
                          onOpen={setSelected}
                          onDelete={deleteItem}
                        />
                      </motion.div>
                    ))}
                  </AnimatePresence>
                </div>
              )}
            </section>
          </>
        )}
      </div>

      <div className="h-4" aria-hidden />

      <ItemDetailModal
        item={selected}
        onClose={() => setSelected(null)}
        onChecked={load}
        onDeleted={load}
      />

      <AccountPanel
        open={accountOpen}
        userId={userId}
        onClose={() => setAccountOpen(false)}
        onSaved={(u) => {
          setDisplayName(u.displayName);
          setLang(normalizeLang(u.preferredLanguage));
        }}
      />
    </div>
  );
}
