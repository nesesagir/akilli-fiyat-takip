import type {
  CreateTrackedItemRequest,
  DashboardSummaryDto,
  PriceHistoryPointDto,
  TrackedItemDto,
  UpdateUserProfileRequest,
  UserDto,
} from "./types";

export const API_BASE =
  process.env.NEXT_PUBLIC_API_URL?.trim() || "http://localhost:5080";

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const res = await fetch(`${API_BASE}${path}`, {
    ...init,
    headers: {
      Accept: "application/json",
      ...(init?.body ? { "Content-Type": "application/json" } : {}),
      ...init?.headers,
    },
    cache: "no-store",
  });

  if (!res.ok) {
    const text = await res.text().catch(() => "");
    throw new Error(text || `API ${res.status}: ${path}`);
  }

  if (res.status === 204) return undefined as T;
  return res.json() as Promise<T>;
}

export const api = {
  createUser: (
    email: string,
    password: string,
    firstName: string,
    lastName: string
  ) =>
    request<UserDto>("/api/users", {
      method: "POST",
      body: JSON.stringify({ email, password, firstName, lastName }),
    }),

  loginUser: (email: string, password: string) =>
    request<UserDto>("/api/users/login", {
      method: "POST",
      body: JSON.stringify({ email, password }),
    }),

  getUser: (id: string) => request<UserDto>(`/api/users/${id}`),

  updateUser: (id: string, body: UpdateUserProfileRequest) =>
    request<UserDto>(`/api/users/${id}`, {
      method: "PATCH",
      body: JSON.stringify(body),
    }),

  getDashboard: (userId: string) =>
    request<DashboardSummaryDto>(`/api/dashboard/${userId}`),

  getItems: (userId: string) =>
    request<TrackedItemDto[]>(`/api/trackeditems/user/${userId}`),

  createItem: (body: CreateTrackedItemRequest) =>
    request<TrackedItemDto>("/api/trackeditems", {
      method: "POST",
      body: JSON.stringify(body),
    }),

  getHistory: (itemId: string, days = 30) =>
    request<PriceHistoryPointDto[]>(
      `/api/trackeditems/${itemId}/history?days=${days}`
    ),

  checkPrice: (itemId: string) =>
    request(`/api/trackeditems/${itemId}/check`, { method: "POST" }),

  deactivateItem: (itemId: string) =>
    request<void>(`/api/trackeditems/${itemId}`, { method: "DELETE" }),
};
