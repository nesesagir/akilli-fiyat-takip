import type { Metadata } from "next";
import { DM_Sans, Fraunces } from "next/font/google";
import { ThemeProvider } from "@/components/ThemeProvider";
import {
  SEO_KEYWORDS,
  SITE_DESCRIPTION,
  SITE_NAME,
} from "@/lib/seo";
import "./globals.css";

const sans = DM_Sans({
  subsets: ["latin"],
  variable: "--font-sans",
});

const display = Fraunces({
  subsets: ["latin"],
  variable: "--font-display",
});

export const metadata: Metadata = {
  metadataBase: new URL(
    process.env.NEXT_PUBLIC_SITE_URL?.trim() || "http://localhost:3001"
  ),
  title: {
    default: `${SITE_NAME} | Ürün Fiyat Takip ve Hedef Fiyat Alarmı`,
    template: `%s | ${SITE_NAME}`,
  },
  description: SITE_DESCRIPTION,
  keywords: [...SEO_KEYWORDS],
  authors: [{ name: SITE_NAME }],
  creator: SITE_NAME,
  publisher: SITE_NAME,
  openGraph: {
    type: "website",
    locale: "tr_TR",
    siteName: SITE_NAME,
    title: `${SITE_NAME} | Ürün Fiyat Takip`,
    description: SITE_DESCRIPTION,
  },
  twitter: {
    card: "summary_large_image",
    title: `${SITE_NAME} | Ürün Fiyat Takip`,
    description: SITE_DESCRIPTION,
  },
  robots: {
    index: true,
    follow: true,
    googleBot: {
      index: true,
      follow: true,
      "max-image-preview": "large",
      "max-snippet": -1,
      "max-video-preview": -1,
    },
  },
  category: "shopping",
  alternates: {
    canonical: "/",
  },
  other: {
    "googlebot": "index,follow",
    "revisit-after": "7 days",
  },
};

const jsonLd = {
  "@context": "https://schema.org",
  "@type": "WebApplication",
  name: SITE_NAME,
  alternateName: [
    "Fiyat Takip Paneli",
    "Ürün Fiyat İzleme",
    "Hedef Fiyat Alarmı",
  ],
  applicationCategory: "ShoppingApplication",
  operatingSystem: "Web",
  description: SITE_DESCRIPTION,
  inLanguage: "tr-TR",
  offers: {
    "@type": "Offer",
    price: "0",
    priceCurrency: "TRY",
  },
  keywords: SEO_KEYWORDS.join(", "),
  featureList: [
    "Ürün linkinden otomatik fiyat çekme",
    "Hedef fiyat alarmı",
    "Fiyat geçmişi grafiği",
    "Trendyol Hepsiburada Amazon Temu D&R desteği",
  ],
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="tr" suppressHydrationWarning>
      <head>
        <script
          type="application/ld+json"
          dangerouslySetInnerHTML={{ __html: JSON.stringify(jsonLd) }}
        />
      </head>
      <body className={`${sans.variable} ${display.variable} antialiased`}>
        <ThemeProvider>{children}</ThemeProvider>
      </body>
    </html>
  );
}
