import { Navbar } from "@/components/Navbar";

export function PageShell({
  children,
  title,
  subtitle,
}: {
  children: React.ReactNode;
  title?: string;
  subtitle?: string;
}) {
  return (
    <div className="min-h-screen bg-[radial-gradient(circle_at_top,_#2a1d05,_#090909_55%)] text-white">
      <Navbar />
      <main className="mx-auto max-w-6xl px-4 py-10">
        {(title || subtitle) && (
          <div className="mb-8">
            {title && <h1 className="text-3xl font-semibold">{title}</h1>}
            {subtitle && <p className="mt-2 text-zinc-400">{subtitle}</p>}
          </div>
        )}
        {children}
      </main>
    </div>
  );
}
