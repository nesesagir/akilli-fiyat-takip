"use client";

import { useEffect, useState } from "react";
import { AnimatePresence, motion } from "framer-motion";
import { api } from "@/lib/api";
import type { PriceHistoryPointDto, TrackedItemDto } from "@/lib/types";
import { formatMoney } from "@/lib/format";
import { PriceChart } from "./PriceChart";
import { Skeleton } from "./Skeleton";
import { useLanguage } from "./LanguageProvider";

type Props = {
  item: TrackedItemDto | null;
  onClose: () => void;
  onChecked: () => void;
  onDeleted: () => void;
};

export function ItemDetailModal({ item, onClose, onChecked, onDeleted }: Props) {
  const { t } = useLanguage();
  const [history, setHistory] = useState<PriceHistoryPointDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [checking, setChecking] = useState(false);
  const [confirmDelete, setConfirmDelete] = useState(false);
  const [deleting, setDeleting] = useState(false);

  useEffect(() => {
    if (!item) return;
    setConfirmDelete(false);
    let cancelled = false;
    setLoading(true);
    api
      .getHistory(item.id, 30)
      .then((data) => {
        if (!cancelled) setHistory(data);
      })
      .catch(() => {
        if (!cancelled) setHistory([]);
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });
    return () => {
      cancelled = true;
    };
  }, [item]);

  async function checkNow() {
    if (!item) return;
    setChecking(true);
    try {
      await api.checkPrice(item.id);
      const data = await api.getHistory(item.id, 30);
      setHistory(data);
      onChecked();
    } finally {
      setChecking(false);
    }
  }

  async function deleteItem() {
    if (!item) return;
    setDeleting(true);
    try {
      await api.deactivateItem(item.id);
      onDeleted();
      onClose();
    } finally {
      setDeleting(false);
      setConfirmDelete(false);
    }
  }

  return (
    <AnimatePresence>
      {item && (
        <motion.div
          className="fixed inset-0 z-50 flex items-end justify-center bg-black/40 p-4 sm:items-center"
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          exit={{ opacity: 0 }}
          onClick={onClose}
        >
          <motion.div
            initial={{ y: 40, opacity: 0, scale: 0.98 }}
            animate={{ y: 0, opacity: 1, scale: 1 }}
            exit={{ y: 24, opacity: 0, scale: 0.98 }}
            transition={{ type: "spring", stiffness: 320, damping: 28 }}
            onClick={(e) => e.stopPropagation()}
            className="glass max-h-[90vh] w-full max-w-2xl overflow-y-auto rounded-3xl p-5 shadow-glass dark:shadow-glass-dark sm:p-6"
          >
            <div className="flex items-start justify-between gap-4">
              <div className="min-w-0">
                <p className="text-xs uppercase tracking-[0.16em] text-muted">
                  {item.storeName ?? t.product}
                </p>
                <h2 className="mt-1 font-display text-2xl text-foreground">
                  {item.title}
                </h2>
                <p className="mt-2 text-sm text-muted">
                  {formatMoney(item.currentPrice, item.currency)} · {t.target.toLowerCase()}{" "}
                  {formatMoney(item.targetPrice, item.currency)}
                </p>
              </div>
              <button
                type="button"
                onClick={onClose}
                className="shrink-0 rounded-lg border border-border px-3 py-1.5 text-sm text-muted transition hover:bg-accentsoft"
              >
                {t.close}
              </button>
            </div>

            <div className="mt-5">
              <div className="mb-3 flex items-center justify-between">
                <h3 className="text-sm font-medium text-foreground">{t.last30Days}</h3>
                <button
                  type="button"
                  onClick={checkNow}
                  disabled={checking}
                  className="rounded-lg bg-accentsoft px-3 py-1.5 text-xs font-semibold text-accent transition hover:opacity-90"
                >
                  {checking ? t.checking : t.checkPrice}
                </button>
              </div>
              {loading ? (
                <Skeleton className="h-64" />
              ) : (
                <PriceChart points={history} currency={item.currency} />
              )}
            </div>

            <div className="mt-5 flex flex-wrap items-center justify-between gap-3">
              <a
                href={item.productUrl}
                target="_blank"
                rel="noreferrer"
                className="inline-flex text-sm font-medium text-accent underline-offset-4 hover:underline"
              >
                {t.goToProduct}
              </a>

              {!confirmDelete ? (
                <button
                  type="button"
                  onClick={() => setConfirmDelete(true)}
                  className="rounded-xl border border-rise/30 px-3 py-2 text-sm font-medium text-rise transition hover:bg-rise/10"
                >
                  {t.removeTracking}
                </button>
              ) : (
                <motion.div
                  initial={{ opacity: 0, y: 6 }}
                  animate={{ opacity: 1, y: 0 }}
                  className="flex flex-wrap items-center gap-2"
                >
                  <span className="text-sm text-muted">{t.confirmDelete}</span>
                  <button
                    type="button"
                    disabled={deleting}
                    onClick={deleteItem}
                    className="rounded-xl bg-rise px-3 py-2 text-sm font-semibold text-white transition hover:opacity-90 disabled:opacity-60"
                  >
                    {deleting ? t.deleting : t.confirmDeleteYes}
                  </button>
                  <button
                    type="button"
                    disabled={deleting}
                    onClick={() => setConfirmDelete(false)}
                    className="rounded-xl border border-border px-3 py-2 text-sm text-muted transition hover:bg-accentsoft"
                  >
                    {t.cancel}
                  </button>
                </motion.div>
              )}
            </div>
          </motion.div>
        </motion.div>
      )}
    </AnimatePresence>
  );
}
