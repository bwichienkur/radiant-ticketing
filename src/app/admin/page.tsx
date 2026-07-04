"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { PageShell } from "@/components/PageShell";
import { Button } from "@/components/Button";
import { formatCurrency, formatDate } from "@/lib/utils";

type Stats = {
  events: number;
  orders: number;
  tickets: number;
  revenueCents: number;
};

export default function AdminPage() {
  const [stats, setStats] = useState<Stats | null>(null);
  const [recentOrders, setRecentOrders] = useState<
    {
      id: string;
      totalCents: number;
      createdAt: string;
      user: { name: string };
      items: { ticketType: { event: { title: string } } }[];
    }[]
  >([]);
  const [events, setEvents] = useState<
    { id: string; title: string; status: string; startAt: string }[]
  >([]);
  const [error, setError] = useState("");

  useEffect(() => {
    Promise.all([
      fetch("/api/admin/stats").then((res) => res.json()),
      fetch("/api/events?mine=true").then((res) => res.json()),
    ])
      .then(([statsData, eventsData]) => {
        if (statsData.error) throw new Error(statsData.error);
        setStats(statsData.stats);
        setRecentOrders(statsData.recentOrders);
        setEvents(eventsData.events ?? []);
      })
      .catch((err) => setError(err.message));
  }, []);

  return (
    <PageShell
      title="Organizer dashboard"
      subtitle="Manage events, monitor sales, and track ticket activity."
    >
      {error && <p className="mb-4 text-red-400">{error}</p>}
      <div className="mb-8 flex flex-wrap gap-3">
        <Link href="/admin/events/new">
          <Button>Create event</Button>
        </Link>
        <Link href="/checkin">
          <Button variant="secondary">Open check-in</Button>
        </Link>
      </div>

      {stats && (
        <div className="mb-8 grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
          {[
            ["Events", stats.events],
            ["Orders", stats.orders],
            ["Tickets sold", stats.tickets],
            ["Revenue", formatCurrency(stats.revenueCents)],
          ].map(([label, value]) => (
            <div key={label} className="rounded-2xl border border-white/10 bg-white/5 p-4">
              <p className="text-sm text-zinc-400">{label}</p>
              <p className="mt-2 text-2xl font-semibold">{value}</p>
            </div>
          ))}
        </div>
      )}

      <div className="grid gap-8 lg:grid-cols-2">
        <section>
          <h2 className="mb-4 text-lg font-medium">Your events</h2>
          <div className="space-y-3">
            {events.map((event) => (
              <div
                key={event.id}
                className="flex items-center justify-between rounded-xl border border-white/10 bg-white/5 p-4"
              >
                <div>
                  <p className="font-medium">{event.title}</p>
                  <p className="text-sm text-zinc-400">
                    {formatDate(event.startAt)} · {event.status}
                  </p>
                </div>
                <Link href={`/admin/events/${event.id}`} className="text-sm text-amber-400">
                  Manage
                </Link>
              </div>
            ))}
            {events.length === 0 && <p className="text-zinc-400">No events created yet.</p>}
          </div>
        </section>

        <section>
          <h2 className="mb-4 text-lg font-medium">Recent orders</h2>
          <div className="space-y-3">
            {recentOrders.map((order) => (
              <div key={order.id} className="rounded-xl border border-white/10 bg-white/5 p-4">
                <div className="flex items-center justify-between">
                  <p className="font-medium">{order.user.name}</p>
                  <p className="text-amber-300">{formatCurrency(order.totalCents)}</p>
                </div>
                <p className="mt-1 text-sm text-zinc-400">
                  {order.items[0]?.ticketType.event.title}
                </p>
                <p className="mt-1 text-xs text-zinc-500">{formatDate(order.createdAt)}</p>
              </div>
            ))}
            {recentOrders.length === 0 && (
              <p className="text-zinc-400">No orders recorded yet.</p>
            )}
          </div>
        </section>
      </div>
    </PageShell>
  );
}
