"use client";

import { SITE_NAME } from "@/lib/seo";
import { useLanguage } from "./LanguageProvider";

export function SiteFooter() {
  const { t } = useLanguage();
  const year = new Date().getFullYear();

  return (
    <footer className="relative z-10 mt-auto border-t border-border/70 bg-transparent">
      <div className="mx-auto flex max-w-6xl flex-col items-center gap-2 px-4 py-8 text-center sm:px-6 lg:px-8">
        <p className="text-xs font-medium uppercase tracking-[0.18em] text-accent">
          {t.brand}
        </p>
        <p className="flex flex-wrap items-center justify-center gap-x-1.5 gap-y-1 text-sm text-muted">
          <span
            className="inline-flex h-[1.15rem] w-[1.15rem] items-center justify-center rounded-full border border-current text-[0.65rem] font-semibold leading-none"
            aria-hidden
          >
            C
          </span>
          <span className="sr-only">{t.copyright} </span>
          <span>
            {year} {SITE_NAME}. {t.rightsReserved}
          </span>
        </p>
      </div>
    </footer>
  );
}
