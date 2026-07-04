"use client";

import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { useEffect, useState } from "react";

type User = {
  id: string;
  email: string;
  name: string;
  role: string;
};

const links = [
  { href: "/events", label: "Events" },
  { href: "/my-tickets", label: "My Tickets" },
];

export function Navbar() {
  const pathname = usePathname();
  const router = useRouter();
  const [user, setUser] = useState<User | null>(null);

  useEffect(() => {
    fetch("/api/auth/me")
      .then((res) => (res.ok ? res.json() : null))
      .then((data) => setUser(data?.user ?? null))
      .catch(() => setUser(null));
  }, [pathname]);

  async function logout() {
    await fetch("/api/auth/logout", { method: "POST" });
    setUser(null);
    router.push("/");
    router.refresh();
  }

  const adminLinks =
    user && ["ADMIN", "ORGANIZER"].includes(user.role)
      ? [
          { href: "/admin", label: "Dashboard" },
          { href: "/checkin", label: "Check-in" },
        ]
      : [];

  return (
    <header className="border-b border-white/10 bg-black/40 backdrop-blur">
      <div className="mx-auto flex max-w-6xl items-center justify-between px-4 py-4">
        <Link href="/" className="text-lg font-semibold tracking-tight text-white">
          Enhancement<span className="text-amber-400">Hub</span>
        </Link>
        <nav className="flex items-center gap-4 text-sm">
          {[...links, ...adminLinks].map((link) => (
            <Link
              key={link.href}
              href={link.href}
              className={`transition hover:text-amber-300 ${
                pathname === link.href ? "text-amber-400" : "text-zinc-300"
              }`}
            >
              {link.label}
            </Link>
          ))}
          {user ? (
            <div className="flex items-center gap-3">
              <span className="hidden text-zinc-400 sm:inline">{user.name}</span>
              <button
                onClick={logout}
                className="rounded-full border border-white/15 px-3 py-1 text-zinc-200 hover:border-amber-400/50 hover:text-amber-300"
              >
                Log out
              </button>
            </div>
          ) : (
            <div className="flex items-center gap-2">
              <Link href="/login" className="text-zinc-300 hover:text-white">
                Log in
              </Link>
              <Link
                href="/register"
                className="rounded-full bg-amber-400 px-3 py-1 font-medium text-black hover:bg-amber-300"
              >
                Sign up
              </Link>
            </div>
          )}
        </nav>
      </div>
    </header>
  );
}
