import { ZodError } from "zod";

export function jsonOk<T>(data: T, status = 200) {
  return Response.json(data, { status });
}

export function jsonError(message: string, status = 400) {
  return Response.json({ error: message }, { status });
}

export function handleApiError(error: unknown) {
  if (error instanceof ZodError) {
    return jsonError(error.issues.map((issue) => issue.message).join(", "), 400);
  }
  console.error(error);
  return jsonError("Internal server error", 500);
}
