import bcrypt from "bcryptjs";
import jwt from "jsonwebtoken";
import { cookies } from "next/headers";
import { NextRequest } from "next/server";
import { Role } from "@/generated/prisma/client";
import { db } from "@/lib/db";

const JWT_SECRET = process.env.JWT_SECRET ?? "enhancementhub-dev-secret";
const COOKIE_NAME = "enhancementhub_token";

export type SessionUser = {
  id: string;
  email: string;
  name: string;
  role: Role;
};

export type JwtPayload = SessionUser;

export async function hashPassword(password: string) {
  return bcrypt.hash(password, 12);
}

export async function verifyPassword(password: string, hash: string) {
  return bcrypt.compare(password, hash);
}

export function signToken(user: SessionUser) {
  return jwt.sign(user, JWT_SECRET, { expiresIn: "7d" });
}

export function verifyToken(token: string): JwtPayload | null {
  try {
    return jwt.verify(token, JWT_SECRET) as JwtPayload;
  } catch {
    return null;
  }
}

export async function setAuthCookie(token: string) {
  const cookieStore = await cookies();
  cookieStore.set(COOKIE_NAME, token, {
    httpOnly: true,
    sameSite: "lax",
    secure: process.env.NODE_ENV === "production",
    path: "/",
    maxAge: 60 * 60 * 24 * 7,
  });
}

export async function clearAuthCookie() {
  const cookieStore = await cookies();
  cookieStore.delete(COOKIE_NAME);
}

export async function getSession(): Promise<SessionUser | null> {
  const cookieStore = await cookies();
  const token = cookieStore.get(COOKIE_NAME)?.value;
  if (!token) return null;
  return verifyToken(token);
}

export async function getSessionFromRequest(
  request: NextRequest,
): Promise<SessionUser | null> {
  const token = request.cookies.get(COOKIE_NAME)?.value;
  if (!token) return null;
  return verifyToken(token);
}

export async function requireSession(
  request: NextRequest,
): Promise<SessionUser | Response> {
  const session = await getSessionFromRequest(request);
  if (!session) {
    return Response.json({ error: "Unauthorized" }, { status: 401 });
  }
  return session;
}

export async function requireRole(
  request: NextRequest,
  roles: Role[],
): Promise<SessionUser | Response> {
  const session = await requireSession(request);
  if (session instanceof Response) return session;
  if (!roles.includes(session.role)) {
    return Response.json({ error: "Forbidden" }, { status: 403 });
  }
  return session;
}

export async function getUserById(id: string) {
  return db.user.findUnique({
    where: { id },
    select: { id: true, email: true, name: true, role: true },
  });
}
