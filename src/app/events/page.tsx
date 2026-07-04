import { PageShell } from "@/components/PageShell";
import { EventCard } from "@/components/EventCard";
import { db } from "@/lib/db";

export default async function EventsPage() {
  const events = await db.event.findMany({
    where: { status: "PUBLISHED" },
    include: { venue: true, ticketTypes: true },
    orderBy: { startAt: "asc" },
  });

  return (
    <PageShell title="Events" subtitle="Browse upcoming experiences and secure your tickets.">
      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
        {events.map((event) => (
          <EventCard key={event.id} event={event} />
        ))}
        {events.length === 0 && (
          <p className="text-zinc-400">No published events are available right now.</p>
        )}
      </div>
    </PageShell>
  );
}
