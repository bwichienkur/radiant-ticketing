import { randomBytes } from "crypto";

export function formatCurrency(cents: number) {
  return new Intl.NumberFormat("en-US", {
    style: "currency",
    currency: "USD",
  }).format(cents / 100);
}

export function formatDate(date: Date | string) {
  return new Intl.DateTimeFormat("en-US", {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(new Date(date));
}

export function generateTicketCode() {
  return `RAD-${randomBytes(6).toString("hex").toUpperCase()}`;
}

export function parseJsonBody<T>(body: unknown): T {
  return body as T;
}
