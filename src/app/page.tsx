import Link from "next/link";
import { PageShell } from "@/components/PageShell";
import { EventCard } from "@/components/EventCard";
import { db } from "@/lib/db";

export default async function HomePage() {
  const events = await db.event.findMany({
    where: { status: "PUBLISHED" },
    include: { venue: true, ticketTypes: true },
    orderBy: { startAt: "asc" },
    take: 3,
  });

  return (
    <PageShell>
      <section className="mb-12 grid gap-8 lg:grid-cols-[1.2fr_0.8fr] lg:items-center">
        <div>
          <p className="mb-3 text-sm uppercase tracking-[0.2em] text-amber-400">
            Radiant Ticketing Platform
          </p>
          <h1 className="text-4xl font-semibold leading-tight md:text-5xl">
            Discover events. Buy tickets. Check in instantly.
          </h1>
          <p className="mt-4 max-w-xl text-lg text-zinc-300">
            EnhancementHub powers seamless ticketing for festivals, concerts, and live
            experiences with real-time inventory, QR check-in, and organizer dashboards.
          </p>
          <div className="mt-6 flex flex-wrap gap-3">
            <Link
              href="/events"
              className="rounded-full bg-amber-400 px-5 py-2.5 font-medium text-black hover:bg-amber-300"
            >
              Browse events
            </Link>
            <Link
              href="/register"
              className="rounded-full border border-white/15 px-5 py-2.5 text-white hover:border-amber-400/50"
            >
              Create account
            </Link>
          </div>
        </div>
        <div className="rounded-3xl border border-amber-400/20 bg-gradient-to-br from-amber-400/10 to-transparent p-6">
          <h2 className="text-lg font-medium text-amber-300">Platform capabilities</h2>
          <ul className="mt-4 space-y-3 text-sm text-zinc-300">
            <li>Multi-tier ticket types with live inventory</li>
            <li>Secure customer accounts and order history</li>
            <li>Organizer admin dashboard and analytics</li>
            <li>QR-based venue check-in workflow</li>
          </ul>
        </div>
      </section>

      <section>
        <div className="mb-5 flex items-center justify-between">
          <h2 className="text-2xl font-semibold">Featured events</h2>
          <Link href="/events" className="text-sm text-amber-400 hover:text-amber-300">
            View all
          </Link>
        </div>
        <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
          {events.map((event) => (
            <EventCard key={event.id} event={event} />
          ))}
          {events.length === 0 && (
            <p className="text-zinc-400">No published events yet. Check back soon.</p>
          )}
        </div>
      </section>
    </PageShell>
  );
}
