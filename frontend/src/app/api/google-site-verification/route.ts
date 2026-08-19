import { NextResponse } from "next/server";

export function GET() {
  return new NextResponse(
    "google-site-verification: google7fe81590779c0a3b.html\n",
    {
      status: 200,
      headers: {
        "Content-Type": "text/plain; charset=utf-8",
        "Cache-Control": "no-store",
      },
    }
  );
}
