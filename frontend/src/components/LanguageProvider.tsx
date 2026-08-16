"use client";

import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from "react";
import { getStoredLanguage, setStoredLanguage } from "@/lib/format";
import { normalizeLang, uiCopy, type AppLang, type UiCopy } from "@/lib/i18n";

type LanguageContextValue = {
  lang: AppLang;
  t: UiCopy;
  setLang: (lang: AppLang) => void;
};

const LanguageContext = createContext<LanguageContextValue | null>(null);

export function LanguageProvider({ children }: { children: ReactNode }) {
  const [lang, setLangState] = useState<AppLang>("tr");

  useEffect(() => {
    setLangState(normalizeLang(getStoredLanguage()));
  }, []);

  const setLang = useCallback((next: AppLang) => {
    const value = normalizeLang(next);
    setLangState(value);
    setStoredLanguage(value);
  }, []);

  const value = useMemo(
    () => ({ lang, t: uiCopy(lang), setLang }),
    [lang, setLang]
  );

  return (
    <LanguageContext.Provider value={value}>{children}</LanguageContext.Provider>
  );
}

export function useLanguage() {
  const ctx = useContext(LanguageContext);
  if (!ctx) {
    throw new Error("useLanguage must be used inside LanguageProvider");
  }
  return ctx;
}
