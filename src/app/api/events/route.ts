import { db } from "@/lib/db";
import { jsonOk, handleApiError } from "@/lib/api-response";
import { requireRole } from "@/lib/auth";
import { eventSchema } from "@/lib/validators";
import { NextRequest } from "next/server";
import { Role } from "@/generated/prisma/client";

export async function GET(request: NextRequest) {
  const { searchParams } = new URL(request.url);
  const status = searchParams.get("status");
  const mine = searchParams.get("mine") === "true";

  let where: Record<string, unknown> = {};
  if (status) where = { status };
  if (mine) {
    const session = await requireRole(request, [Role.ADMIN, Role.ORGANIZER]);
    if (session instanceof Response) return session;
    where = { ...where, organizerId: session.id };
  } else {
    where = { ...where, status: "PUBLISHED" };
  }

  const events = await db.event.findMany({
    where,
    include: {
      venue: true,
      ticketTypes: true,
      organizer: { select: { id: true, name: true } },
    },
    orderBy: { startAt: "asc" },
  });

  return jsonOk({ events });
}

export async function POST(request: NextRequest) {
  try {
    const session = await requireRole(request, [Role.ADMIN, Role.ORGANIZER]);
    if (session instanceof Response) return session;

    const body = eventSchema.parse(await request.json());
    const event = await db.event.create({
      data: {
        title: body.title,
        description: body.description,
        imageUrl: body.imageUrl || null,
        startAt: new Date(body.startAt),
        endAt: new Date(body.endAt),
        venueId: body.venueId,
        status: body.status ?? "DRAFT",
        organizerId: session.id,
      },
      include: { venue: true, ticketTypes: true },
    });

    return jsonOk({ event }, 201);
  } catch (error) {
    return handleApiError(error);
  }
}
