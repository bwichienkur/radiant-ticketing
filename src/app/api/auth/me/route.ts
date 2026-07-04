import { getSessionFromRequest } from "@/lib/auth";
import { jsonError, jsonOk } from "@/lib/api-response";

export async function GET(request: Request) {
  const session = await getSessionFromRequest(request as never);
  if (!session) {
    return jsonError("Unauthorized", 401);
  }
  return jsonOk({ user: session });
}
