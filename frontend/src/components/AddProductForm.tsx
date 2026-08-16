"use client";

import { FormEvent, useState } from "react";
import { motion } from "framer-motion";
import { api } from "@/lib/api";
import { useLanguage } from "./LanguageProvider";

type Props = {
  userId: string;
  onCreated: () => void;
};

export function AddProductForm({ userId, onCreated }: Props) {
  const { t } = useLanguage();
  const [url, setUrl] = useState("");
  const [target, setTarget] = useState("");
  const [title, setTitle] = useState("");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function onSubmit(e: FormEvent) {
    e.preventDefault();
    setError(null);
    setLoading(true);
    try {
      const item = await api.createItem({
        userId,
        productUrl: url.trim(),
        targetPrice: Number(target.replace(",", ".")),
        title: title.trim() || undefined,
      });

      setUrl("");
      setTarget("");
      setTitle("");
      onCreated();

      if (!item.imageUrl) {
        try {
          await api.checkPrice(item.id);
          onCreated();
        } catch {
          /* item already saved */
        }
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : t.addFailed);
    } finally {
      setLoading(false);
    }
  }

  return (
    <motion.form
      initial={{ opacity: 0, y: 8 }}
      animate={{ opacity: 1, y: 0 }}
      onSubmit={onSubmit}
      className="glass rounded-2xl p-5 shadow-glass dark:shadow-glass-dark"
    >
      <h2 className="font-display text-xl text-foreground">{t.addProduct}</h2>
      <p className="mt-1 text-sm text-muted">{t.addProductHint}</p>

      <div className="mt-4 grid gap-3 sm:grid-cols-2">
        <label className="sm:col-span-2">
          <span className="mb-1 block text-xs text-muted">{t.link}</span>
          <input
            required
            type="url"
            value={url}
            onChange={(e) => setUrl(e.target.value)}
            placeholder="https://..."
            className="field-input"
          />
        </label>
        <label>
          <span className="mb-1 block text-xs text-muted">{t.targetPrice}</span>
          <input
            required
            inputMode="decimal"
            value={target}
            onChange={(e) => setTarget(e.target.value)}
            placeholder="900"
            className="field-input"
          />
        </label>
        <label>
          <span className="mb-1 block text-xs text-muted">{t.titleOptional}</span>
          <input
            value={title}
            onChange={(e) => setTitle(e.target.value)}
            placeholder={t.titlePlaceholder}
            className="field-input"
          />
        </label>
      </div>

      {error && <p className="mt-3 text-sm text-rise">{error}</p>}

      <button
        type="submit"
        disabled={loading}
        className="mt-4 rounded-xl bg-accent px-4 py-2.5 text-sm font-semibold text-white transition hover:opacity-90 disabled:opacity-60 dark:text-[#0b0f14]"
      >
        {loading ? t.fetchingPrice : t.add}
      </button>
    </motion.form>
  );
}
