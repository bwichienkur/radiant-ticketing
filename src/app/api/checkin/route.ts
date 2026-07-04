import { db } from "@/lib/db";
import { jsonError, jsonOk, handleApiError } from "@/lib/api-response";
import { requireRole } from "@/lib/auth";
import { checkinSchema } from "@/lib/validators";
import { NextRequest } from "next/server";
import { Role } from "@/generated/prisma/client";

export async function POST(request: NextRequest) {
  try {
    const session = await requireRole(request, [Role.ADMIN, Role.ORGANIZER]);
    if (session instanceof Response) return session;

    const body = checkinSchema.parse(await request.json());
    const ticket = await db.ticket.findUnique({
      where: { code: body.code },
      include: {
        ticketType: { include: { event: true } },
        order: true,
      },
    });

    if (!ticket) return jsonError("Ticket not found", 404);
    if (ticket.status === "CANCELLED") {
      return jsonError("Ticket has been cancelled", 400);
    }
    if (ticket.status === "USED") {
      return jsonError("Ticket already checked in", 409);
    }

    if (session.role === Role.ORGANIZER && ticket.ticketType.event.organizerId !== session.id) {
      return jsonError("Forbidden", 403);
    }

    const updated = await db.ticket.update({
      where: { id: ticket.id },
      data: { status: "USED", checkedInAt: new Date() },
      include: {
        ticketType: { include: { event: true } },
      },
    });

    return jsonOk({ ticket: updated });
  } catch (error) {
    return handleApiError(error);
  }
}
