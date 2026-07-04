import { db } from "@/lib/db";
import { jsonOk, handleApiError } from "@/lib/api-response";
import { requireSession } from "@/lib/auth";
import { orderSchema } from "@/lib/validators";
import { generateTicketCode } from "@/lib/utils";
import { NextRequest } from "next/server";

export async function GET(request: NextRequest) {
  const session = await requireSession(request);
  if (session instanceof Response) return session;

  const orders = await db.order.findMany({
    where: { userId: session.id },
    include: {
      items: { include: { ticketType: { include: { event: true } } } },
      tickets: { include: { ticketType: { include: { event: true } } } },
    },
    orderBy: { createdAt: "desc" },
  });

  return jsonOk({ orders });
}

export async function POST(request: NextRequest) {
  try {
    const session = await requireSession(request);
    if (session instanceof Response) return session;

    const body = orderSchema.parse(await request.json());
    const ticketTypeIds = body.items.map((item) => item.ticketTypeId);
    const ticketTypes = await db.ticketType.findMany({
      where: { id: { in: ticketTypeIds } },
      include: { event: true },
    });

    if (ticketTypes.length !== body.items.length) {
      return Response.json({ error: "Invalid ticket types" }, { status: 400 });
    }

    for (const item of body.items) {
      const ticketType = ticketTypes.find((tt) => tt.id === item.ticketTypeId)!;
      if (ticketType.event.status !== "PUBLISHED") {
        return Response.json(
          { error: `Event "${ticketType.event.title}" is not available` },
          { status: 400 },
        );
      }
      const available = ticketType.quantity - ticketType.sold;
      if (item.quantity > available) {
        return Response.json(
          { error: `Not enough tickets for ${ticketType.name}` },
          { status: 400 },
        );
      }
    }

    const order = await db.$transaction(async (tx) => {
      let totalCents = 0;
      const orderItems = body.items.map((item) => {
        const ticketType = ticketTypes.find((tt) => tt.id === item.ticketTypeId)!;
        totalCents += ticketType.price * item.quantity;
        return {
          ticketTypeId: item.ticketTypeId,
          quantity: item.quantity,
          unitPrice: ticketType.price,
        };
      });

      const created = await tx.order.create({
        data: {
          userId: session.id,
          status: "CONFIRMED",
          totalCents,
          items: { create: orderItems },
        },
        include: { items: true },
      });

      const tickets = [];
      for (const item of body.items) {
        await tx.ticketType.update({
          where: { id: item.ticketTypeId },
          data: { sold: { increment: item.quantity } },
        });

        for (let i = 0; i < item.quantity; i++) {
          tickets.push({
            code: generateTicketCode(),
            orderId: created.id,
            ticketTypeId: item.ticketTypeId,
          });
        }
      }

      await tx.ticket.createMany({ data: tickets });
      return created;
    });

    const fullOrder = await db.order.findUnique({
      where: { id: order.id },
      include: {
        items: { include: { ticketType: { include: { event: true } } } },
        tickets: { include: { ticketType: { include: { event: true } } } },
      },
    });

    return jsonOk({ order: fullOrder }, 201);
  } catch (error) {
    return handleApiError(error);
  }
}
