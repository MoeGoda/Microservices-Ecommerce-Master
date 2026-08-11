// Mirrors Identity.Application.Models.AuthResponse exactly — property names
// match the JSON the backend actually sends (camelCase, via ASP.NET Core's
// default JSON serialization), not the C# PascalCase names.
export interface AuthResponse {
  token: string;
  expiresAtUtc: string;
  userName: string;
  role: string;
}

export interface LoginRequest {
  userName: string;
  password: string;
}

// The current signed-in user, derived from AuthResponse and kept in
// AuthService — this is what components actually read, so they never need
// to know or care about the raw JWT string.
export interface CurrentUser {
  userName: string;
  role: string;
  expiresAtUtc: string;
}
