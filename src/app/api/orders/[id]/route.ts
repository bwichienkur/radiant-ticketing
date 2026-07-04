import { db } from "@/lib/db";
import { jsonError, jsonOk } from "@/lib/api-response";
import { requireSession } from "@/lib/auth";
import { NextRequest } from "next/server";

type Params = { params: Promise<{ id: string }> };

export async function GET(request: NextRequest, { params }: Params) {
  const session = await requireSession(request);
  if (session instanceof Response) return session;

  const { id } = await params;
  const order = await db.order.findUnique({
    where: { id },
    include: {
      items: { include: { ticketType: { include: { event: true } } } },
      tickets: { include: { ticketType: { include: { event: true } } } },
    },
  });

  if (!order) return jsonError("Order not found", 404);
  if (order.userId !== session.id && session.role === "CUSTOMER") {
    return jsonError("Forbidden", 403);
  }

  return jsonOk({ order });
}
