"use client";

import { useState } from "react";
import { motion, AnimatePresence } from "framer-motion";
import Image from "next/image";
import type { TrackedItemDto } from "@/lib/types";
import { formatMoney } from "@/lib/format";
import { CircularProgress } from "./CircularProgress";
import { useLanguage } from "./LanguageProvider";

type Props = {
  item: TrackedItemDto;
  onOpen: (item: TrackedItemDto) => void;
  onDelete: (item: TrackedItemDto) => Promise<void>;
};

export function ProductCard({ item, onOpen, onDelete }: Props) {
  const { t } = useLanguage();
  const [confirming, setConfirming] = useState(false);
  const [deleting, setDeleting] = useState(false);
  const progress = item.progressToTargetPercent ?? 0;
  const remaining =
    item.currentPrice != null
      ? Math.max(0, item.currentPrice - item.targetPrice)
      : null;

  async function handleDelete() {
    setDeleting(true);
    try {
      await onDelete(item);
    } finally {
      setDeleting(false);
      setConfirming(false);
    }
  }

  return (
    <motion.div
      layout
      initial={false}
      whileHover={{ y: -10, scale: 1.035 }}
      whileTap={{ scale: 0.985 }}
      transition={{ type: "spring", stiffness: 380, damping: 28 }}
      className="product-card group relative h-full"
    >
      <div
        role="button"
        tabIndex={0}
        onClick={() => !confirming && onOpen(item)}
        onKeyDown={(e) => {
          if (confirming) return;
          if (e.key === "Enter" || e.key === " ") {
            e.preventDefault();
            onOpen(item);
          }
        }}
        className="glass flex h-full w-full cursor-pointer flex-col rounded-2xl p-4 text-left shadow-glass outline-none transition-[box-shadow,border-color] duration-500 ease-out group-hover:border-accent/35 group-hover:shadow-[0_22px_50px_-18px_rgba(15,118,110,0.35)] focus-visible:ring-2 focus-visible:ring-accent dark:shadow-glass-dark dark:group-hover:shadow-[0_22px_50px_-18px_rgba(45,212,191,0.28)]"
      >
        <div className="relative mb-4 aspect-[4/3] overflow-hidden rounded-xl bg-accentsoft transition-[border-radius] duration-500 ease-out group-hover:rounded-2xl">
          {item.imageUrl ? (
            <Image
              src={item.imageUrl}
              alt={item.title}
              fill
              className="object-cover transition duration-700 ease-out group-hover:scale-110"
              sizes="(max-width:768px) 100vw, 33vw"
              unoptimized
              referrerPolicy="no-referrer"
            />
          ) : (
            <div className="flex h-full items-center justify-center text-sm text-muted">
              —
            </div>
          )}
          {!item.isInStock && (
            <span className="absolute left-2 top-2 rounded-md bg-rise/90 px-2 py-1 text-[11px] font-medium text-white">
              {t.outOfStock}
            </span>
          )}

          <button
            type="button"
            aria-label={t.deleteProduct}
            onClick={(e) => {
              e.stopPropagation();
              setConfirming(true);
            }}
            className="absolute right-2 top-2 z-10 rounded-lg border border-border/80 bg-panel/90 px-2.5 py-1.5 text-xs font-medium text-muted opacity-100 shadow-sm backdrop-blur transition hover:border-rise/40 hover:bg-rise/10 hover:text-rise sm:opacity-0 sm:group-hover:opacity-100"
          >
            {t.delete}
          </button>
        </div>

        <div className="mb-1 text-[11px] font-medium uppercase tracking-[0.14em] text-muted">
          {t.store} · {item.storeName || t.unknownStore}
        </div>
        <h3 className="line-clamp-2 min-h-[2.75rem] font-display text-lg leading-snug text-foreground transition-colors duration-300 group-hover:text-accent">
          {item.title}
        </h3>

        <div className="mt-auto flex items-end justify-between gap-3 pt-4">
          <div>
            <div className="text-xs text-muted">{t.current}</div>
            <div className="text-xl font-semibold text-foreground">
              {formatMoney(item.currentPrice, item.currency)}
            </div>
            <div className="text-xs text-muted">
              {t.target} {formatMoney(item.targetPrice, item.currency)}
              {remaining != null && remaining > 0
                ? ` · ${formatMoney(remaining, item.currency)} ${t.remaining.toLowerCase()}`
                : ""}
            </div>
          </div>
          <CircularProgress percent={Number(progress)} />
        </div>
      </div>

      <AnimatePresence>
        {confirming && (
          <motion.div
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            transition={{ duration: 0.2 }}
            className="absolute inset-0 z-20 flex items-center justify-center rounded-2xl bg-black/45 p-4 backdrop-blur-[2px]"
            onClick={(e) => e.stopPropagation()}
          >
            <motion.div
              initial={{ opacity: 0, scale: 0.92, y: 8 }}
              animate={{ opacity: 1, scale: 1, y: 0 }}
              exit={{ opacity: 0, scale: 0.96 }}
              transition={{ type: "spring", stiffness: 400, damping: 26 }}
              className="w-full max-w-[240px] rounded-2xl border border-border bg-panel p-4 shadow-glass dark:shadow-glass-dark"
            >
              <p className="text-center text-sm font-medium text-foreground">
                {t.confirmDelete}
              </p>
              <p className="mt-1 text-center text-xs text-muted">
                {t.confirmDeleteHint}
              </p>
              <div className="mt-4 flex flex-col gap-2">
                <button
                  type="button"
                  disabled={deleting}
                  onClick={handleDelete}
                  className="rounded-xl bg-rise px-3 py-2 text-sm font-semibold text-white transition hover:opacity-90 disabled:opacity-60"
                >
                  {deleting ? t.deleting : t.confirmDeleteYes}
                </button>
                <button
                  type="button"
                  disabled={deleting}
                  onClick={() => setConfirming(false)}
                  className="rounded-xl border border-border px-3 py-2 text-sm text-muted transition hover:bg-accentsoft"
                >
                  {t.cancel}
                </button>
              </div>
            </motion.div>
          </motion.div>
        )}
      </AnimatePresence>
    </motion.div>
  );
}
