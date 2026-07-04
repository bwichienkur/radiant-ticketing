import { db } from "@/lib/db";
import { jsonError, jsonOk, handleApiError } from "@/lib/api-response";
import { requireRole } from "@/lib/auth";
import { eventSchema } from "@/lib/validators";
import { NextRequest } from "next/server";
import { Role } from "@/generated/prisma/client";

type Params = { params: Promise<{ id: string }> };

export async function GET(_request: NextRequest, { params }: Params) {
  const { id } = await params;
  const event = await db.event.findUnique({
    where: { id },
    include: {
      venue: true,
      ticketTypes: true,
      organizer: { select: { id: true, name: true } },
    },
  });

  if (!event) return jsonError("Event not found", 404);
  return jsonOk({ event });
}

export async function PATCH(request: NextRequest, { params }: Params) {
  try {
    const session = await requireRole(request, [Role.ADMIN, Role.ORGANIZER]);
    if (session instanceof Response) return session;

    const { id } = await params;
    const existing = await db.event.findUnique({ where: { id } });
    if (!existing) return jsonError("Event not found", 404);
    if (session.role !== Role.ADMIN && existing.organizerId !== session.id) {
      return jsonError("Forbidden", 403);
    }

    const body = eventSchema.partial().parse(await request.json());
    const event = await db.event.update({
      where: { id },
      data: {
        ...body,
        imageUrl: body.imageUrl === "" ? null : body.imageUrl,
        startAt: body.startAt ? new Date(body.startAt) : undefined,
        endAt: body.endAt ? new Date(body.endAt) : undefined,
      },
      include: { venue: true, ticketTypes: true },
    });

    return jsonOk({ event });
  } catch (error) {
    return handleApiError(error);
  }
}

export async function DELETE(request: NextRequest, { params }: Params) {
  const session = await requireRole(request, [Role.ADMIN, Role.ORGANIZER]);
  if (session instanceof Response) return session;

  const { id } = await params;
  const existing = await db.event.findUnique({ where: { id } });
  if (!existing) return jsonError("Event not found", 404);
  if (session.role !== Role.ADMIN && existing.organizerId !== session.id) {
    return jsonError("Forbidden", 403);
  }

  await db.event.delete({ where: { id } });
  return jsonOk({ success: true });
}
