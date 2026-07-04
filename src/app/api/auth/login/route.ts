import { setAuthCookie, signToken, verifyPassword } from "@/lib/auth";
import { db } from "@/lib/db";
import { jsonError, jsonOk, handleApiError } from "@/lib/api-response";
import { loginSchema } from "@/lib/validators";

export async function POST(request: Request) {
  try {
    const body = loginSchema.parse(await request.json());
    const user = await db.user.findUnique({ where: { email: body.email } });
    if (!user || !(await verifyPassword(body.password, user.password))) {
      return jsonError("Invalid email or password", 401);
    }

    const session = {
      id: user.id,
      email: user.email,
      name: user.name,
      role: user.role,
    };
    const token = signToken(session);
    await setAuthCookie(token);
    return jsonOk({ user: session });
  } catch (error) {
    return handleApiError(error);
  }
}
