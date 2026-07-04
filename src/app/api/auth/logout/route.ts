import { clearAuthCookie } from "@/lib/auth";
import { jsonOk } from "@/lib/api-response";

export async function POST() {
  await clearAuthCookie();
  return jsonOk({ success: true });
}
