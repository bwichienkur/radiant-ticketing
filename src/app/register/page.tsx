"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useState } from "react";
import { PageShell } from "@/components/PageShell";
import { Input } from "@/components/Input";
import { Button } from "@/components/Button";

export default function RegisterPage() {
  const router = useRouter();
  const [name, setName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setLoading(true);
    setError("");
    const res = await fetch("/api/auth/register", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ name, email, password }),
    });
    const data = await res.json();
    setLoading(false);
    if (!res.ok) {
      setError(data.error ?? "Registration failed.");
      return;
    }
    router.push("/events");
    router.refresh();
  }

  return (
    <PageShell title="Create account" subtitle="Join EnhancementHub to purchase and manage tickets.">
      <form onSubmit={onSubmit} className="mx-auto max-w-md space-y-4 rounded-2xl border border-white/10 bg-white/5 p-6">
        <Input label="Name" value={name} onChange={(e) => setName(e.target.value)} required />
        <Input label="Email" type="email" value={email} onChange={(e) => setEmail(e.target.value)} required />
        <Input label="Password" type="password" value={password} onChange={(e) => setPassword(e.target.value)} required minLength={8} />
        {error && <p className="text-sm text-red-400">{error}</p>}
        <Button type="submit" disabled={loading} className="w-full">
          {loading ? "Creating account..." : "Sign up"}
        </Button>
        <p className="text-sm text-zinc-400">
          Already registered?{" "}
          <Link href="/login" className="text-amber-400 hover:text-amber-300">
            Log in
          </Link>
        </p>
      </form>
    </PageShell>
  );
}
