"use client";

import { useEffect, useState } from "react";
import { useParams, useRouter } from "next/navigation";
import { PageShell } from "@/components/PageShell";
import { Button } from "@/components/Button";
import { formatCurrency, formatDate } from "@/lib/utils";

type TicketType = {
  id: string;
  name: string;
  description: string | null;
  price: number;
  quantity: number;
  sold: number;
};

type Event = {
  id: string;
  title: string;
  description: string;
  startAt: string;
  endAt: string;
  venue: { name: string; address: string; city: string };
  ticketTypes: TicketType[];
};

export default function EventDetailPage() {
  const params = useParams<{ id: string }>();
  const router = useRouter();
  const [event, setEvent] = useState<Event | null>(null);
  const [quantities, setQuantities] = useState<Record<string, number>>({});
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    fetch(`/api/events/${params.id}`)
      .then((res) => res.json())
      .then((data) => setEvent(data.event))
      .finally(() => setLoading(false));
  }, [params.id]);

  async function purchase() {
    setError("");
    setSubmitting(true);
    const items = Object.entries(quantities)
      .filter(([, qty]) => qty > 0)
      .map(([ticketTypeId, quantity]) => ({ ticketTypeId, quantity }));

    if (!items.length) {
      setError("Select at least one ticket.");
      setSubmitting(false);
      return;
    }

    const res = await fetch("/api/orders", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ items }),
    });
    const data = await res.json();
    setSubmitting(false);

    if (!res.ok) {
      setError(data.error ?? "Unable to complete purchase.");
      return;
    }

    router.push("/my-tickets");
  }

  if (loading) {
    return (
      <PageShell title="Loading event...">
        <p className="text-zinc-400">Please wait.</p>
      </PageShell>
    );
  }

  if (!event) {
    return (
      <PageShell title="Event not found">
        <p className="text-zinc-400">This event may have been removed.</p>
      </PageShell>
    );
  }

  return (
    <PageShell title={event.title} subtitle={`${event.venue.name} · ${event.venue.city}`}>
      <div className="grid gap-8 lg:grid-cols-[1.2fr_0.8fr]">
        <div className="space-y-4">
          <p className="text-zinc-300">{event.description}</p>
          <div className="rounded-2xl border border-white/10 bg-white/5 p-4 text-sm text-zinc-300">
            <p>{event.venue.address}</p>
            <p className="mt-2">Starts: {formatDate(event.startAt)}</p>
            <p>Ends: {formatDate(event.endAt)}</p>
          </div>
        </div>

        <div className="space-y-4">
          <h2 className="text-lg font-medium">Select tickets</h2>
          {event.ticketTypes.map((ticketType) => {
            const available = ticketType.quantity - ticketType.sold;
            return (
              <div
                key={ticketType.id}
                className="rounded-2xl border border-white/10 bg-white/5 p-4"
              >
                <div className="flex items-start justify-between gap-3">
                  <div>
                    <p className="font-medium">{ticketType.name}</p>
                    {ticketType.description && (
                      <p className="mt-1 text-sm text-zinc-400">{ticketType.description}</p>
                    )}
                    <p className="mt-2 text-sm text-zinc-500">{available} available</p>
                  </div>
                  <p className="font-medium text-amber-300">
                    {formatCurrency(ticketType.price)}
                  </p>
                </div>
                <input
                  type="number"
                  min={0}
                  max={available}
                  value={quantities[ticketType.id] ?? 0}
                  disabled={available === 0}
                  onChange={(e) =>
                    setQuantities((prev) => ({
                      ...prev,
                      [ticketType.id]: Number(e.target.value),
                    }))
                  }
                  className="mt-3 w-full rounded-xl border border-white/10 bg-black/30 px-3 py-2"
                />
              </div>
            );
          })}
          {error && <p className="text-sm text-red-400">{error}</p>}
          <Button onClick={purchase} disabled={submitting} className="w-full">
            {submitting ? "Processing..." : "Complete purchase"}
          </Button>
        </div>
      </div>
    </PageShell>
  );
}
