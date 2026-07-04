import "dotenv/config";
import { PrismaBetterSqlite3 } from "@prisma/adapter-better-sqlite3";
import bcrypt from "bcryptjs";
import { PrismaClient, Role } from "../src/generated/prisma/client";

const adapter = new PrismaBetterSqlite3({
  url: process.env.DATABASE_URL ?? "file:./dev.db",
});
const db = new PrismaClient({ adapter });

async function main() {
  const password = await bcrypt.hash("password123", 12);

  const admin = await db.user.upsert({
    where: { email: "admin@enhancementhub.dev" },
    update: {},
    create: {
      email: "admin@enhancementhub.dev",
      name: "Platform Admin",
      password,
      role: Role.ADMIN,
    },
  });

  const organizer = await db.user.upsert({
    where: { email: "organizer@enhancementhub.dev" },
    update: {},
    create: {
      email: "organizer@enhancementhub.dev",
      name: "Radiant Events",
      password,
      role: Role.ORGANIZER,
    },
  });

  const customer = await db.user.upsert({
    where: { email: "customer@enhancementhub.dev" },
    update: {},
    create: {
      email: "customer@enhancementhub.dev",
      name: "Demo Customer",
      password,
      role: Role.CUSTOMER,
    },
  });

  const venue = await db.venue.upsert({
    where: { id: "seed-venue-radiant-hall" },
    update: {},
    create: {
      id: "seed-venue-radiant-hall",
      name: "Radiant Hall",
      address: "100 Festival Way",
      city: "Austin",
      capacity: 5000,
    },
  });

  const existingEvent = await db.event.findFirst({
    where: { title: "Summer Lights Festival" },
  });

  if (!existingEvent) {
    const startAt = new Date();
    startAt.setDate(startAt.getDate() + 30);
    const endAt = new Date(startAt);
    endAt.setHours(endAt.getHours() + 6);

    await db.event.create({
      data: {
        title: "Summer Lights Festival",
        description:
          "An open-air music and arts festival featuring headline performances, local vendors, and immersive light installations across three stages.",
        startAt,
        endAt,
        status: "PUBLISHED",
        organizerId: organizer.id,
        venueId: venue.id,
        ticketTypes: {
          create: [
            {
              name: "General Admission",
              description: "Festival ground access",
              price: 4999,
              quantity: 300,
            },
            {
              name: "VIP",
              description: "Premium viewing deck and lounge access",
              price: 12999,
              quantity: 75,
            },
          ],
        },
      },
    });
  }

  console.log("Seed complete");
  console.log({ admin: admin.email, organizer: organizer.email, customer: customer.email });
}

main()
  .catch((error) => {
    console.error(error);
    process.exit(1);
  })
  .finally(async () => {
    await db.$disconnect();
  });
