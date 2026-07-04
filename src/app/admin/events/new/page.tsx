"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { PageShell } from "@/components/PageShell";
import { Input } from "@/components/Input";
import { Textarea } from "@/components/Textarea";
import { Button } from "@/components/Button";

type Venue = { id: string; name: string; city: string };

export default function NewEventPage() {
  const router = useRouter();
  const [venues, setVenues] = useState<Venue[]>([]);
  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [venueId, setVenueId] = useState("");
  const [startAt, setStartAt] = useState("");
  const [endAt, setEndAt] = useState("");
  const [ticketName, setTicketName] = useState("General Admission");
  const [ticketPrice, setTicketPrice] = useState("49.99");
  const [ticketQty, setTicketQty] = useState("100");
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    fetch("/api/venues")
      .then((res) => res.json())
      .then((data) => {
        setVenues(data.venues);
        if (data.venues[0]) setVenueId(data.venues[0].id);
      });
  }, []);

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setLoading(true);
    setError("");

    const eventRes = await fetch("/api/events", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        title,
        description,
        venueId,
        startAt: new Date(startAt).toISOString(),
        endAt: new Date(endAt).toISOString(),
        status: "PUBLISHED",
      }),
    });
    const eventData = await eventRes.json();
    if (!eventRes.ok) {
      setError(eventData.error ?? "Failed to create event.");
      setLoading(false);
      return;
    }

    const ticketRes = await fetch(`/api/events/${eventData.event.id}/ticket-types`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        name: ticketName,
        price: Math.round(Number(ticketPrice) * 100),
        quantity: Number(ticketQty),
      }),
    });
    const ticketData = await ticketRes.json();
    setLoading(false);

    if (!ticketRes.ok) {
      setError(ticketData.error ?? "Event created but ticket setup failed.");
      return;
    }

    router.push("/admin");
  }

  return (
    <PageShell title="Create event" subtitle="Publish a new experience with ticket inventory.">
      <form onSubmit={onSubmit} className="mx-auto max-w-2xl space-y-4 rounded-2xl border border-white/10 bg-white/5 p-6">
        <Input label="Title" value={title} onChange={(e) => setTitle(e.target.value)} required />
        <Textarea label="Description" value={description} onChange={(e) => setDescription(e.target.value)} rows={4} required />
        <label className="block space-y-1 text-sm">
          <span className="text-zinc-300">Venue</span>
          <select
            value={venueId}
            onChange={(e) => setVenueId(e.target.value)}
            className="w-full rounded-xl border border-white/10 bg-black/30 px-3 py-2 text-white"
            required
          >
            {venues.map((venue) => (
              <option key={venue.id} value={venue.id}>
                {venue.name} · {venue.city}
              </option>
            ))}
          </select>
        </label>
        <div className="grid gap-4 md:grid-cols-2">
          <Input label="Start" type="datetime-local" value={startAt} onChange={(e) => setStartAt(e.target.value)} required />
          <Input label="End" type="datetime-local" value={endAt} onChange={(e) => setEndAt(e.target.value)} required />
        </div>
        <div className="rounded-xl border border-amber-400/20 bg-amber-400/5 p-4">
          <p className="mb-3 text-sm font-medium text-amber-300">Initial ticket type</p>
          <div className="grid gap-4 md:grid-cols-3">
            <Input label="Name" value={ticketName} onChange={(e) => setTicketName(e.target.value)} required />
            <Input label="Price (USD)" type="number" step="0.01" value={ticketPrice} onChange={(e) => setTicketPrice(e.target.value)} required />
            <Input label="Quantity" type="number" value={ticketQty} onChange={(e) => setTicketQty(e.target.value)} required />
          </div>
        </div>
        {error && <p className="text-sm text-red-400">{error}</p>}
        <Button type="submit" disabled={loading} className="w-full">
          {loading ? "Creating..." : "Publish event"}
        </Button>
      </form>
    </PageShell>
  );
}
