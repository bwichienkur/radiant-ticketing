import Link from "next/link";
import { formatCurrency, formatDate } from "@/lib/utils";

type EventCardProps = {
  event: {
    id: string;
    title: string;
    description: string;
    startAt: string | Date;
    venue: { name: string; city: string };
    ticketTypes: { price: number; quantity: number; sold: number }[];
  };
};

export function EventCard({ event }: EventCardProps) {
  const lowestPrice = event.ticketTypes.length
    ? Math.min(...event.ticketTypes.map((t) => t.price))
    : null;
  const available = event.ticketTypes.reduce(
    (sum, t) => sum + (t.quantity - t.sold),
    0,
  );

  return (
    <Link
      href={`/events/${event.id}`}
      className="group rounded-2xl border border-white/10 bg-white/5 p-5 transition hover:border-amber-400/40 hover:bg-white/10"
    >
      <div className="mb-3 flex items-start justify-between gap-3">
        <div>
          <h3 className="text-lg font-semibold text-white group-hover:text-amber-300">
            {event.title}
          </h3>
          <p className="mt-1 text-sm text-zinc-400">
            {event.venue.name} · {event.venue.city}
          </p>
        </div>
        {lowestPrice !== null && (
          <span className="rounded-full bg-amber-400/15 px-3 py-1 text-sm font-medium text-amber-300">
            from {formatCurrency(lowestPrice)}
          </span>
        )}
      </div>
      <p className="line-clamp-2 text-sm text-zinc-300">{event.description}</p>
      <div className="mt-4 flex items-center justify-between text-xs text-zinc-500">
        <span>{formatDate(event.startAt)}</span>
        <span>{available} tickets left</span>
      </div>
    </Link>
  );
}
