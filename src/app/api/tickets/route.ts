import { db } from "@/lib/db";
import { jsonOk } from "@/lib/api-response";
import { requireSession } from "@/lib/auth";
import { NextRequest } from "next/server";

export async function GET(request: NextRequest) {
  const session = await requireSession(request);
  if (session instanceof Response) return session;

  const tickets = await db.ticket.findMany({
    where: { order: { userId: session.id } },
    include: {
      ticketType: { include: { event: { include: { venue: true } } } },
      order: true,
    },
    orderBy: { createdAt: "desc" },
  });

  return jsonOk({ tickets });
}
