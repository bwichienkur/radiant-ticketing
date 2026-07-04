import { db } from "@/lib/db";
import { jsonOk } from "@/lib/api-response";
import { requireRole } from "@/lib/auth";
import { NextRequest } from "next/server";
import { Role } from "@/generated/prisma/client";

export async function GET(request: NextRequest) {
  const session = await requireRole(request, [Role.ADMIN, Role.ORGANIZER]);
  if (session instanceof Response) return session;

  const eventFilter =
    session.role === Role.ORGANIZER ? { organizerId: session.id } : {};

  const [events, orders, tickets, revenue] = await Promise.all([
    db.event.count({ where: eventFilter }),
    db.order.count({
      where: {
        status: "CONFIRMED",
        items: {
          some: {
            ticketType: {
              event: eventFilter,
            },
          },
        },
      },
    }),
    db.ticket.count({
      where: {
        ticketType: { event: eventFilter },
      },
    }),
    db.order.aggregate({
      where: {
        status: "CONFIRMED",
        items: {
          some: {
            ticketType: {
              event: eventFilter,
            },
          },
        },
      },
      _sum: { totalCents: true },
    }),
  ]);

  const recentOrders = await db.order.findMany({
    where: {
      status: "CONFIRMED",
      items: {
        some: {
          ticketType: {
            event: eventFilter,
          },
        },
      },
    },
    include: {
      user: { select: { name: true, email: true } },
      items: { include: { ticketType: { include: { event: true } } } },
    },
    orderBy: { createdAt: "desc" },
    take: 10,
  });

  return jsonOk({
    stats: {
      events,
      orders,
      tickets,
      revenueCents: revenue._sum.totalCents ?? 0,
    },
    recentOrders,
  });
}
