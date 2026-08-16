"use client";

import { useEffect, useState } from "react";
import { Dashboard } from "@/components/Dashboard";
import { LanguageProvider } from "@/components/LanguageProvider";
import { Onboarding } from "@/components/Onboarding";
import { ScrollToTop } from "@/components/ScrollToTop";
import { SiteFooter } from "@/components/SiteFooter";
import { getStoredUserId } from "@/lib/format";

export default function Home() {
  const [userId, setUserId] = useState<string | null>(null);
  const [ready, setReady] = useState(false);

  useEffect(() => {
    setUserId(getStoredUserId());
    setReady(true);
  }, []);

  if (!ready) {
    return (
      <main className="flex min-h-screen items-center justify-center bg-[#0b0f14] text-slate-500">
        …
      </main>
    );
  }

  return (
    <LanguageProvider>
      <main className="flex min-h-screen flex-col">
        <div className="flex-1">
          {userId ? (
            <Dashboard userId={userId} onReset={() => setUserId(null)} />
          ) : (
            <Onboarding onReady={setUserId} />
          )}
        </div>
        <SiteFooter />
        <ScrollToTop />
      </main>
    </LanguageProvider>
  );
}
