"use client";

import { useMemo } from "react";
import {
  CartesianGrid,
  Line,
  LineChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts";
import type { PriceHistoryPointDto } from "@/lib/types";
import { formatMoney } from "@/lib/format";
import { dateLocale } from "@/lib/i18n";
import { useLanguage } from "./LanguageProvider";

type Props = {
  points: PriceHistoryPointDto[];
  currency?: string;
};

export function PriceChart({ points, currency = "TRY" }: Props) {
  const { t, lang } = useLanguage();

  const data = useMemo(() => {
    return points.map((p, i) => {
      const prev = i > 0 ? points[i - 1].price : p.price;
      const direction = p.price < prev ? "down" : p.price > prev ? "up" : "flat";
      return {
        ...p,
        label: new Date(p.recordedAtUtc).toLocaleDateString(dateLocale(lang), {
          day: "2-digit",
          month: "short",
        }),
        direction,
      };
    });
  }, [points, lang]);

  if (points.length === 0) {
    return (
      <p className="py-10 text-center text-sm text-muted">{t.noHistory}</p>
    );
  }

  return (
    <div className="h-64 w-full">
      <ResponsiveContainer width="100%" height="100%">
        <LineChart data={data} margin={{ top: 8, right: 8, left: 0, bottom: 0 }}>
          <CartesianGrid stroke="var(--border)" strokeDasharray="3 3" />
          <XAxis
            dataKey="label"
            tick={{ fill: "var(--muted)", fontSize: 12 }}
            axisLine={false}
            tickLine={false}
          />
          <YAxis
            tick={{ fill: "var(--muted)", fontSize: 12 }}
            axisLine={false}
            tickLine={false}
            width={56}
            tickFormatter={(v) => `${v}`}
          />
          <Tooltip
            contentStyle={{
              background: "var(--background)",
              border: "1px solid var(--border)",
              borderRadius: 12,
            }}
            formatter={(value) => [
              formatMoney(typeof value === "number" ? value : Number(value), currency),
              t.price,
            ]}
          />
          <Line
            type="monotone"
            dataKey="price"
            stroke="var(--accent)"
            strokeWidth={2.5}
            dot={(props) => {
              const { cx, cy, payload, index } = props;
              if (cx == null || cy == null) return <g key={index} />;
              const color =
                payload.direction === "down"
                  ? "var(--fall)"
                  : payload.direction === "up"
                    ? "var(--rise)"
                    : "var(--accent)";
              return (
                <circle
                  key={index}
                  cx={cx}
                  cy={cy}
                  r={4}
                  fill={color}
                  stroke="var(--background)"
                  strokeWidth={2}
                />
              );
            }}
            activeDot={{ r: 6 }}
          />
        </LineChart>
      </ResponsiveContainer>
      <div className="mt-3 flex gap-4 text-xs text-muted">
        <span className="inline-flex items-center gap-1.5">
          <i className="h-2.5 w-2.5 rounded-full bg-fall" /> {t.drop}
        </span>
        <span className="inline-flex items-center gap-1.5">
          <i className="h-2.5 w-2.5 rounded-full bg-rise" /> {t.rise}
        </span>
      </div>
    </div>
  );
}
