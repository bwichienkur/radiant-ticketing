import { requireRole } from "@/lib/auth";
import { db } from "@/lib/db";
import { jsonOk, handleApiError } from "@/lib/api-response";
import { venueSchema } from "@/lib/validators";
import { NextRequest } from "next/server";
import { Role } from "@/generated/prisma/client";

export async function GET() {
  const venues = await db.venue.findMany({ orderBy: { name: "asc" } });
  return jsonOk({ venues });
}

export async function POST(request: NextRequest) {
  try {
    const session = await requireRole(request, [Role.ADMIN, Role.ORGANIZER]);
    if (session instanceof Response) return session;

    const body = venueSchema.parse(await request.json());
    const venue = await db.venue.create({ data: body });
    return jsonOk({ venue }, 201);
  } catch (error) {
    return handleApiError(error);
  }
}
