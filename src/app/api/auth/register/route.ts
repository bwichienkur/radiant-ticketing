import { hashPassword, setAuthCookie, signToken } from "@/lib/auth";
import { db } from "@/lib/db";
import { jsonError, jsonOk, handleApiError } from "@/lib/api-response";
import { registerSchema } from "@/lib/validators";

export async function POST(request: Request) {
  try {
    const body = registerSchema.parse(await request.json());
    const existing = await db.user.findUnique({ where: { email: body.email } });
    if (existing) {
      return jsonError("Email already registered", 409);
    }

    const user = await db.user.create({
      data: {
        email: body.email,
        name: body.name,
        password: await hashPassword(body.password),
      },
      select: { id: true, email: true, name: true, role: true },
    });

    const token = signToken(user);
    await setAuthCookie(token);
    return jsonOk({ user }, 201);
  } catch (error) {
    return handleApiError(error);
  }
}
