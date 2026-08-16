export function Skeleton({ className = "" }: { className?: string }) {
  return <div className={`skeleton rounded-xl ${className}`} />;
}

export function DashboardSkeleton() {
  return (
    <div className="grid gap-4 md:grid-cols-6 md:grid-rows-[auto_auto]">
      <Skeleton className="h-40 md:col-span-2" />
      <Skeleton className="h-40 md:col-span-4" />
      <Skeleton className="h-64 md:col-span-2" />
      <Skeleton className="h-64 md:col-span-2" />
      <Skeleton className="h-64 md:col-span-2" />
    </div>
  );
}
