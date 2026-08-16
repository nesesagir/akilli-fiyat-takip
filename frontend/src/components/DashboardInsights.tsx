"use client";

import Image from "next/image";
import { motion } from "framer-motion";
import type { TrackedItemDto } from "@/lib/types";
import { formatMoney } from "@/lib/format";
import { useLanguage } from "./LanguageProvider";

type Props = {
  savings: number;
  deal: TrackedItemDto | null | undefined;
  onOpenDeal: (item: TrackedItemDto) => void;
};

export function DashboardInsights({ savings, deal, onOpenDeal }: Props) {
  const { t } = useLanguage();
  const progress = deal?.progressToTargetPercent ?? 0;
  const gap =
    deal?.currentPrice != null
      ? Math.max(0, deal.currentPrice - deal.targetPrice)
      : null;

  return (
    <div className="grid gap-4 lg:grid-cols-12">
      <motion.section
        initial={{ opacity: 0, y: 14 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ delay: 0.05, duration: 0.45 }}
        whileHover={{ y: -4 }}
        className="relative overflow-hidden rounded-[1.75rem] border border-border bg-panel p-6 shadow-glass lg:col-span-4 dark:shadow-glass-dark"
      >
        <div
          aria-hidden
          className="pointer-events-none absolute -right-10 -top-16 h-44 w-44 rounded-full bg-accent/10 blur-3xl"
        />
        <div
          aria-hidden
          className="pointer-events-none absolute -bottom-12 left-0 h-32 w-32 rounded-full bg-fall/10 blur-3xl"
        />

        <div className="relative flex h-full flex-col">
          <div className="flex items-center justify-between gap-3">
            <p className="text-[11px] font-semibold uppercase tracking-[0.2em] text-muted">
              {t.monthlySavings}
            </p>
            <span className="rounded-full bg-fall/10 px-2.5 py-1 text-[11px] font-semibold text-fall">
              {t.estimated}
            </span>
          </div>

          <p className="mt-6 font-display text-4xl tracking-tight text-foreground sm:text-5xl">
            {formatMoney(savings)}
          </p>

          <div className="mt-5 h-1.5 overflow-hidden rounded-full bg-accentsoft">
            <motion.div
              className="h-full rounded-full bg-gradient-to-r from-accent to-fall"
              initial={{ width: 0 }}
              animate={{
                width:
                  savings > 0
                    ? `${Math.min(100, 28 + Math.log10(savings + 1) * 18)}%`
                    : "8%",
              }}
              transition={{ delay: 0.25, duration: 0.8, ease: [0.22, 1, 0.36, 1] }}
            />
          </div>

          <p className="mt-4 text-sm leading-relaxed text-muted">{t.savingsHint}</p>
        </div>
      </motion.section>

      <motion.section
        initial={{ opacity: 0, y: 14 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ delay: 0.12, duration: 0.45 }}
        whileHover={{ y: -4 }}
        className="relative overflow-hidden rounded-[1.75rem] border border-border bg-panel shadow-glass lg:col-span-8 dark:shadow-glass-dark"
      >
        <div
          aria-hidden
          className="pointer-events-none absolute inset-y-0 right-0 w-1/2 bg-gradient-to-l from-accent/8 via-transparent to-transparent"
        />

        {deal ? (
          <div className="relative grid gap-0 sm:grid-cols-[minmax(0,200px)_1fr]">
            <div className="relative min-h-[180px] overflow-hidden bg-accentsoft sm:min-h-full">
              {deal.imageUrl ? (
                <Image
                  src={deal.imageUrl}
                  alt={deal.title}
                  fill
                  className="object-cover transition duration-700 hover:scale-105"
                  sizes="220px"
                  unoptimized
                  referrerPolicy="no-referrer"
                />
              ) : (
                <div className="flex h-full min-h-[180px] items-center justify-center text-sm text-muted">
                  {t.noImage}
                </div>
              )}
              <div className="absolute inset-x-0 bottom-0 bg-gradient-to-t from-black/50 to-transparent p-3 sm:hidden">
                <p className="text-[11px] font-semibold uppercase tracking-[0.16em] text-white/90">
                  {t.dealOfDay}
                </p>
              </div>
            </div>

            <div className="flex flex-col justify-between gap-5 p-5 sm:p-6">
              <div>
                <div className="mb-3 flex flex-wrap items-center gap-2">
                  <span className="hidden text-[11px] font-semibold uppercase tracking-[0.2em] text-muted sm:inline">
                    {t.dealOfDay}
                  </span>
                  <span className="rounded-full border border-border bg-accentsoft px-2.5 py-1 text-[11px] font-medium text-foreground">
                    {deal.storeName || t.store}
                  </span>
                  <span className="rounded-full bg-accent/12 px-2.5 py-1 text-[11px] font-semibold text-accent">
                    {t.toTarget} %{Math.round(Number(progress))}
                  </span>
                </div>

                <h2 className="font-display text-2xl leading-snug text-foreground sm:text-[1.7rem]">
                  {deal.title}
                </h2>

                <div className="mt-5 flex flex-wrap items-end gap-6">
                  <div>
                    <p className="text-[11px] uppercase tracking-[0.14em] text-muted">
                      {t.current}
                    </p>
                    <p className="mt-1 text-3xl font-semibold tracking-tight text-foreground">
                      {formatMoney(deal.currentPrice, deal.currency)}
                    </p>
                  </div>
                  <div>
                    <p className="text-[11px] uppercase tracking-[0.14em] text-muted">
                      {t.target}
                    </p>
                    <p className="mt-1 text-xl font-semibold text-fall">
                      {formatMoney(deal.targetPrice, deal.currency)}
                    </p>
                  </div>
                  {gap != null && gap > 0 && (
                    <div>
                      <p className="text-[11px] uppercase tracking-[0.14em] text-muted">
                        {t.remaining}
                      </p>
                      <p className="mt-1 text-lg font-medium text-muted">
                        {formatMoney(gap, deal.currency)}
                      </p>
                    </div>
                  )}
                </div>

                <div className="mt-5 h-1.5 overflow-hidden rounded-full bg-accentsoft">
                  <motion.div
                    className="h-full rounded-full bg-accent"
                    initial={{ width: 0 }}
                    animate={{ width: `${Math.min(100, Number(progress))}%` }}
                    transition={{ delay: 0.3, duration: 0.85, ease: [0.22, 1, 0.36, 1] }}
                  />
                </div>
              </div>

              <div className="flex flex-wrap items-center gap-3">
                <motion.button
                  type="button"
                  whileHover={{ scale: 1.03 }}
                  whileTap={{ scale: 0.97 }}
                  onClick={() => onOpenDeal(deal)}
                  className="rounded-xl bg-accent px-4 py-2.5 text-sm font-semibold text-white transition hover:opacity-95 dark:text-[#0b0f14]"
                >
                  {t.detailsChart}
                </motion.button>
                <a
                  href={deal.productUrl}
                  target="_blank"
                  rel="noreferrer"
                  className="rounded-xl border border-border px-4 py-2.5 text-sm font-medium text-muted transition hover:border-accent/40 hover:text-foreground"
                >
                  {t.goToStore}
                </a>
              </div>
            </div>
          </div>
        ) : (
          <div className="relative flex min-h-[200px] flex-col justify-center p-6 sm:p-8">
            <p className="text-[11px] font-semibold uppercase tracking-[0.2em] text-muted">
              {t.dealOfDay}
            </p>
            <p className="mt-4 font-display text-2xl text-foreground">{t.noDeal}</p>
            <p className="mt-2 max-w-md text-sm text-muted">{t.noDealHint}</p>
          </div>
        )}
      </motion.section>
    </div>
  );
}
