import { NextResponse } from "next/server";

/** Google Search Console HTML file verification. */
export function GET() {
  return new NextResponse(
    "google-site-verification: google7fe81590779c0a3b.html\n",
    {
      status: 200,
      headers: {
        "Content-Type": "text/html; charset=utf-8",
        "Cache-Control": "no-store",
      },
    }
  );
}
