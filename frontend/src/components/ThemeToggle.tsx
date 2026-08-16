"use client";

import { useTheme } from "./ThemeProvider";

export function ThemeToggle() {
  const { theme, toggle } = useTheme();

  return (
    <button
      type="button"
      onClick={toggle}
      className="rounded-xl border border-border bg-panel px-3 py-2 text-sm text-foreground transition hover:bg-accentsoft"
      aria-label="Tema değiştir"
    >
      {theme === "light" ? "Karanlık" : "Aydınlık"}
    </button>
  );
}
