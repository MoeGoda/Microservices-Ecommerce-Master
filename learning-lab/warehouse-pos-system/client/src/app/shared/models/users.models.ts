// Mirrors Identity.Application.Features.Users.Queries.GetUsers.UserDto (H).
export interface UserDto {
  id: number;
  userName: string;
  email: string;
  firstName: string;
  lastName: string;
  role: string;
  isActive: boolean;
  createdAt: string;
}

// Mirrors AuthController.CreateUser's request body (RegisterCommand) — this
// reuses the existing F2 Admin-only endpoint rather than adding a new one.
export interface CreateUserRequest {
  userName: string;
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  role: string;
}
