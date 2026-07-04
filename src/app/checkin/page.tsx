"use client";

import { useState } from "react";
import { PageShell } from "@/components/PageShell";
import { Input } from "@/components/Input";
import { Button } from "@/components/Button";
import { formatDate } from "@/lib/utils";

export default function CheckinPage() {
  const [code, setCode] = useState("");
  const [result, setResult] = useState<{
    success: boolean;
    message: string;
    eventTitle?: string;
    checkedInAt?: string;
  } | null>(null);
  const [loading, setLoading] = useState(false);

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setLoading(true);
    setResult(null);
    const res = await fetch("/api/checkin", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ code }),
    });
    const data = await res.json();
    setLoading(false);

    if (!res.ok) {
      setResult({ success: false, message: data.error ?? "Check-in failed." });
      return;
    }

    setResult({
      success: true,
      message: "Guest checked in successfully.",
      eventTitle: data.ticket.ticketType.event.title,
      checkedInAt: data.ticket.checkedInAt,
    });
    setCode("");
  }

  return (
    <PageShell
      title="Venue check-in"
      subtitle="Scan or enter a ticket code to validate entry."
    >
      <form onSubmit={onSubmit} className="mx-auto max-w-lg space-y-4 rounded-2xl border border-white/10 bg-white/5 p-6">
        <Input
          label="Ticket code"
          value={code}
          onChange={(e) => setCode(e.target.value.toUpperCase())}
          placeholder="RAD-XXXXXXXXXXXX"
          required
        />
        <Button type="submit" disabled={loading} className="w-full">
          {loading ? "Validating..." : "Check in guest"}
        </Button>
      </form>

      {result && (
        <div
          className={`mx-auto mt-6 max-w-lg rounded-2xl border p-5 ${
            result.success
              ? "border-emerald-400/30 bg-emerald-400/10"
              : "border-red-400/30 bg-red-400/10"
          }`}
        >
          <p className="font-medium">{result.message}</p>
          {result.eventTitle && (
            <p className="mt-2 text-sm text-zinc-300">Event: {result.eventTitle}</p>
          )}
          {result.checkedInAt && (
            <p className="mt-1 text-sm text-zinc-400">
              Checked in at {formatDate(result.checkedInAt)}
            </p>
          )}
        </div>
      )}
    </PageShell>
  );
}
