import { z } from "zod";

export const registerSchema = z.object({
  email: z.string().email(),
  name: z.string().min(2).max(100),
  password: z.string().min(8).max(128),
});

export const loginSchema = z.object({
  email: z.string().email(),
  password: z.string().min(1),
});

export const venueSchema = z.object({
  name: z.string().min(2),
  address: z.string().min(2),
  city: z.string().min(2),
  capacity: z.number().int().positive(),
});

export const eventSchema = z.object({
  title: z.string().min(2),
  description: z.string().min(10),
  imageUrl: z.string().url().optional().or(z.literal("")),
  startAt: z.string().datetime(),
  endAt: z.string().datetime(),
  venueId: z.string().min(1),
  status: z.enum(["DRAFT", "PUBLISHED", "CANCELLED", "COMPLETED"]).optional(),
});

export const ticketTypeSchema = z.object({
  name: z.string().min(2),
  description: z.string().optional(),
  price: z.number().int().nonnegative(),
  quantity: z.number().int().positive(),
});

export const orderSchema = z.object({
  items: z
    .array(
      z.object({
        ticketTypeId: z.string().min(1),
        quantity: z.number().int().positive(),
      }),
    )
    .min(1),
});

export const checkinSchema = z.object({
  code: z.string().min(4),
});
