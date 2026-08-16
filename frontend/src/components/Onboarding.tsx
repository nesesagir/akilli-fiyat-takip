"use client";

import { FormEvent, useState } from "react";
import { motion } from "framer-motion";
import { api } from "@/lib/api";
import { setStoredUser } from "@/lib/format";
import { useLanguage } from "./LanguageProvider";

type Props = {
  onReady: (userId: string) => void;
};

type Mode = "register" | "login";

export function Onboarding({ onReady }: Props) {
  const { t, lang, setLang } = useLanguage();
  const [mode, setMode] = useState<Mode>("register");
  const [firstName, setFirstName] = useState("");
  const [lastName, setLastName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function onSubmit(e: FormEvent) {
    e.preventDefault();
    setLoading(true);
    setError(null);

    const first = firstName.trim();
    const last = lastName.trim();
    const mail = email.trim();
    const pass = password;

    if (!mail) {
      setError(t.needEmail);
      setLoading(false);
      return;
    }
    if (pass.length < 8) {
      setError(t.needPassword);
      setLoading(false);
      return;
    }

    if (mode === "register") {
      if (!first) {
        setError(t.needFirstName);
        setLoading(false);
        return;
      }
      if (!last) {
        setError(t.needLastName);
        setLoading(false);
        return;
      }
    }

    try {
      let user =
        mode === "login"
          ? await api.loginUser(mail, pass)
          : await api.createUser(mail, pass, first, last);

      if (mode === "register" && lang === "en") {
        user = await api.updateUser(user.id, {
          firstName: user.firstName,
          lastName: user.lastName,
          email: user.email,
          preferredCurrency: user.preferredCurrency || "TRY",
          preferredLanguage: "en",
          emailNotificationsEnabled: user.emailNotificationsEnabled,
        });
      }

      setStoredUser(
        user.id,
        user.displayName,
        user.email,
        user.preferredLanguage || lang
      );
      onReady(user.id);
    } catch (err) {
      const raw = err instanceof Error ? err.message : t.registerFailed;
      if (mode === "login") {
        setError(
          raw.includes("Failed to fetch") || raw.includes("NetworkError")
            ? t.connectionFailed
            : t.loginFailed
        );
      } else {
        setError(
          raw.includes("Failed to fetch") || raw.includes("NetworkError")
            ? t.connectionFailed
            : raw.includes("zaten") ||
                raw.includes("Conflict") ||
                raw.includes("409") ||
                raw.includes("already")
              ? t.emailAlreadyUsed
              : raw
        );
      }
    } finally {
      setLoading(false);
    }
  }

  const fieldClass =
    "w-full rounded-none border-0 border-b border-white/20 bg-transparent px-0 py-3 text-lg text-white outline-none placeholder:text-slate-600 focus:border-teal-400";

  return (
    <div className="relative flex min-h-screen w-full flex-col justify-end overflow-hidden sm:justify-center">
      <div
        aria-hidden
        className="absolute inset-0"
        style={{
          background:
            "radial-gradient(ellipse 90% 70% at 50% -20%, rgba(15,118,110,0.18), transparent 55%), linear-gradient(165deg, #0b0f14 0%, #121820 45%, #0b0f14 100%)",
        }}
      />
      <div
        aria-hidden
        className="absolute inset-x-0 bottom-0 h-1/2 opacity-40"
        style={{
          backgroundImage:
            "linear-gradient(rgba(255,255,255,0.04) 1px, transparent 1px), linear-gradient(90deg, rgba(255,255,255,0.04) 1px, transparent 1px)",
          backgroundSize: "48px 48px",
          maskImage: "linear-gradient(to top, black, transparent)",
        }}
      />

      <motion.div
        initial={{ opacity: 0, y: 24 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.5, ease: "easeOut" }}
        className="relative z-10 mx-auto w-full max-w-lg px-6 pb-12 pt-16 sm:pb-0"
      >
        <div className="mb-6 flex gap-2">
          <button
            type="button"
            onClick={() => setLang("en")}
            className={`rounded-full px-3 py-1 text-xs font-medium transition ${
              lang === "en"
                ? "bg-white text-slate-900"
                : "border border-white/20 text-slate-400 hover:text-white"
            }`}
          >
            English
          </button>
          <button
            type="button"
            onClick={() => setLang("tr")}
            className={`rounded-full px-3 py-1 text-xs font-medium transition ${
              lang === "tr"
                ? "bg-white text-slate-900"
                : "border border-white/20 text-slate-400 hover:text-white"
            }`}
          >
            Türkçe
          </button>
        </div>

        <p className="font-[family-name:var(--font-sans)] text-sm font-medium uppercase tracking-[0.22em] text-teal-300/90">
          {t.brand}
        </p>
        <h1 className="mt-4 font-display text-4xl leading-[1.1] text-white sm:text-5xl">
          {t.onboardingHeadline1}
          <br />
          {t.onboardingHeadline2}
        </h1>
        <p className="mt-4 max-w-md text-base leading-relaxed text-slate-400">
          {t.onboardingHint}
        </p>

        <div className="mt-8 flex gap-2">
          <button
            type="button"
            onClick={() => setMode("register")}
            className={`rounded-full px-4 py-1.5 text-sm font-medium transition ${
              mode === "register"
                ? "bg-teal-400/20 text-teal-200"
                : "text-slate-500 hover:text-slate-300"
            }`}
          >
            {t.register}
          </button>
          <button
            type="button"
            onClick={() => setMode("login")}
            className={`rounded-full px-4 py-1.5 text-sm font-medium transition ${
              mode === "login"
                ? "bg-teal-400/20 text-teal-200"
                : "text-slate-500 hover:text-slate-300"
            }`}
          >
            {t.login}
          </button>
        </div>

        <form onSubmit={onSubmit} className="mt-6 space-y-4">
          {mode === "register" && (
            <div className="grid gap-4 sm:grid-cols-2">
              <div>
                <label htmlFor="first" className="mb-1.5 block text-xs text-slate-400">
                  {t.firstName}
                </label>
                <input
                  id="first"
                  required
                  autoComplete="given-name"
                  value={firstName}
                  onChange={(e) => setFirstName(e.target.value)}
                  className={fieldClass}
                />
              </div>
              <div>
                <label htmlFor="last" className="mb-1.5 block text-xs text-slate-400">
                  {t.lastName}
                </label>
                <input
                  id="last"
                  required
                  autoComplete="family-name"
                  value={lastName}
                  onChange={(e) => setLastName(e.target.value)}
                  className={fieldClass}
                />
              </div>
            </div>
          )}
          <div>
            <label htmlFor="email" className="mb-1.5 block text-xs text-slate-400">
              {t.email}
            </label>
            <input
              id="email"
              required
              type="email"
              autoComplete="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              className={fieldClass}
            />
          </div>
          <div>
            <label htmlFor="password" className="mb-1.5 block text-xs text-slate-400">
              {t.password}
            </label>
            <input
              id="password"
              required
              type="password"
              autoComplete={mode === "login" ? "current-password" : "new-password"}
              minLength={8}
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              className={fieldClass}
            />
          </div>

          {error && <p className="text-sm text-red-400">{error}</p>}

          <motion.button
            type="submit"
            disabled={loading}
            whileTap={{ scale: 0.98 }}
            className="mt-4 w-full rounded-full bg-white px-6 py-3.5 text-sm font-semibold text-slate-900 transition hover:bg-teal-50 disabled:opacity-60"
          >
            {loading ? t.saving : mode === "login" ? t.login : t.register}
          </motion.button>
        </form>

        <button
          type="button"
          onClick={() => setMode(mode === "login" ? "register" : "login")}
          className="mt-4 text-sm text-slate-500 transition hover:text-teal-300"
        >
          {mode === "login" ? t.needAccount : t.haveAccount}
        </button>
      </motion.div>
    </div>
  );
}
