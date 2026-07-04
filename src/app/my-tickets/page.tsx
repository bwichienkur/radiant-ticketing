"use client";

import { useEffect, useState } from "react";
import { PageShell } from "@/components/PageShell";
import { formatCurrency, formatDate } from "@/lib/utils";

type Ticket = {
  id: string;
  code: string;
  status: string;
  checkedInAt: string | null;
  ticketType: {
    name: string;
    event: { title: string; startAt: string; venue: { name: string } };
  };
};

export default function MyTicketsPage() {
  const [tickets, setTickets] = useState<Ticket[]>([]);
  const [qrCodes, setQrCodes] = useState<Record<string, string>>({});
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    fetch("/api/tickets")
      .then((res) => {
        if (!res.ok) throw new Error("Unauthorized");
        return res.json();
      })
      .then(async (data) => {
        setTickets(data.tickets);
        const codes: Record<string, string> = {};
        for (const ticket of data.tickets as Ticket[]) {
          const res = await fetch(`/api/tickets/${ticket.code}`);
          if (res.ok) {
            const payload = await res.json();
            codes[ticket.code] = payload.qrDataUrl;
          }
        }
        setQrCodes(codes);
      })
      .catch(() => setTickets([]))
      .finally(() => setLoading(false));
  }, []);

  return (
    <PageShell title="My tickets" subtitle="Your purchased tickets and QR codes for entry.">
      {loading && <p className="text-zinc-400">Loading tickets...</p>}
      {!loading && tickets.length === 0 && (
        <p className="text-zinc-400">No tickets yet. Browse events to make your first purchase.</p>
      )}
      <div className="grid gap-4 md:grid-cols-2">
        {tickets.map((ticket) => (
          <div key={ticket.id} className="rounded-2xl border border-white/10 bg-white/5 p-5">
            <div className="flex items-start justify-between gap-4">
              <div>
                <p className="text-lg font-medium">{ticket.ticketType.event.title}</p>
                <p className="text-sm text-zinc-400">{ticket.ticketType.name}</p>
                <p className="mt-2 text-sm text-zinc-500">
                  {ticket.ticketType.event.venue.name} · {formatDate(ticket.ticketType.event.startAt)}
                </p>
                <p className="mt-3 font-mono text-sm text-amber-300">{ticket.code}</p>
                <p className="mt-1 text-xs uppercase tracking-wide text-zinc-500">{ticket.status}</p>
              </div>
              {qrCodes[ticket.code] && (
                // eslint-disable-next-line @next/next/no-img-element
                <img
                  src={qrCodes[ticket.code]}
                  alt={`QR code for ${ticket.code}`}
                  width={112}
                  height={112}
                  className="rounded-lg border border-white/10 bg-white p-1"
                />
              )}
            </div>
          </div>
        ))}
      </div>
    </PageShell>
  );
}
