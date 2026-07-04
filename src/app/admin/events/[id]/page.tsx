"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import { PageShell } from "@/components/PageShell";
import { Input } from "@/components/Input";
import { Button } from "@/components/Button";
import { formatCurrency } from "@/lib/utils";

export default function ManageEventPage() {
  const params = useParams<{ id: string }>();
  const [event, setEvent] = useState<{
    id: string;
    title: string;
    status: string;
    ticketTypes: { id: string; name: string; price: number; quantity: number; sold: number }[];
  } | null>(null);
  const [name, setName] = useState("VIP");
  const [price, setPrice] = useState("99.99");
  const [quantity, setQuantity] = useState("50");
  const [message, setMessage] = useState("");

  function loadEvent() {
    fetch(`/api/events/${params.id}`)
      .then((res) => res.json())
      .then((data) => setEvent(data.event));
  }

  useEffect(() => {
    loadEvent();
  }, [params.id]);

  async function addTicketType(e: React.FormEvent) {
    e.preventDefault();
    setMessage("");
    const res = await fetch(`/api/events/${params.id}/ticket-types`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        name,
        price: Math.round(Number(price) * 100),
        quantity: Number(quantity),
      }),
    });
    const data = await res.json();
    if (!res.ok) {
      setMessage(data.error ?? "Failed to add ticket type.");
      return;
    }
    setMessage("Ticket type added.");
    loadEvent();
  }

  if (!event) {
    return (
      <PageShell title="Manage event">
        <p className="text-zinc-400">Loading...</p>
      </PageShell>
    );
  }

  return (
    <PageShell title={event.title} subtitle={`Status: ${event.status}`}>
      <section className="mb-8">
        <h2 className="mb-4 text-lg font-medium">Ticket types</h2>
        <div className="space-y-3">
          {event.ticketTypes.map((ticketType) => (
            <div
              key={ticketType.id}
              className="flex items-center justify-between rounded-xl border border-white/10 bg-white/5 p-4"
            >
              <div>
                <p className="font-medium">{ticketType.name}</p>
                <p className="text-sm text-zinc-400">
                  {ticketType.sold}/{ticketType.quantity} sold
                </p>
              </div>
              <p className="text-amber-300">{formatCurrency(ticketType.price)}</p>
            </div>
          ))}
        </div>
      </section>

      <form onSubmit={addTicketType} className="max-w-xl space-y-4 rounded-2xl border border-white/10 bg-white/5 p-6">
        <h3 className="font-medium">Add ticket type</h3>
        <Input label="Name" value={name} onChange={(e) => setName(e.target.value)} required />
        <div className="grid gap-4 md:grid-cols-2">
          <Input label="Price (USD)" type="number" step="0.01" value={price} onChange={(e) => setPrice(e.target.value)} required />
          <Input label="Quantity" type="number" value={quantity} onChange={(e) => setQuantity(e.target.value)} required />
        </div>
        {message && <p className="text-sm text-amber-300">{message}</p>}
        <Button type="submit">Add ticket type</Button>
      </form>
    </PageShell>
  );
}
