import { db } from "@/lib/db";
import { jsonError, jsonOk, handleApiError } from "@/lib/api-response";
import { requireRole } from "@/lib/auth";
import { ticketTypeSchema } from "@/lib/validators";
import { NextRequest } from "next/server";
import { Role } from "@/generated/prisma/client";

type Params = { params: Promise<{ id: string }> };

export async function POST(request: NextRequest, { params }: Params) {
  try {
    const session = await requireRole(request, [Role.ADMIN, Role.ORGANIZER]);
    if (session instanceof Response) return session;

    const { id: eventId } = await params;
    const event = await db.event.findUnique({ where: { id: eventId } });
    if (!event) return jsonError("Event not found", 404);
    if (session.role !== Role.ADMIN && event.organizerId !== session.id) {
      return jsonError("Forbidden", 403);
    }

    const body = ticketTypeSchema.parse(await request.json());
    const ticketType = await db.ticketType.create({
      data: { ...body, eventId },
    });

    return jsonOk({ ticketType }, 201);
  } catch (error) {
    return handleApiError(error);
  }
}
