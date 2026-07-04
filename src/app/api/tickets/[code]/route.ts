import QRCode from "qrcode";
import { db } from "@/lib/db";
import { jsonError, jsonOk } from "@/lib/api-response";
import { requireSession } from "@/lib/auth";
import { NextRequest } from "next/server";

type Params = { params: Promise<{ code: string }> };

export async function GET(request: NextRequest, { params }: Params) {
  const session = await requireSession(request);
  if (session instanceof Response) return session;

  const { code } = await params;
  const ticket = await db.ticket.findUnique({
    where: { code },
    include: {
      ticketType: { include: { event: { include: { venue: true } } } },
      order: true,
    },
  });

  if (!ticket) return jsonError("Ticket not found", 404);
  if (ticket.order.userId !== session.id && !["ADMIN", "ORGANIZER"].includes(session.role)) {
    return jsonError("Forbidden", 403);
  }

  const qrDataUrl = await QRCode.toDataURL(ticket.code, { margin: 1, width: 256 });
  return jsonOk({ ticket, qrDataUrl });
}
