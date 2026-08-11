// Matches exactly what Common.ExceptionHandling's GlobalExceptionHandler
// (backend, A2) writes for every error response across every service in
// this system — one shape, one place in the frontend that knows how to
// read it (see the HTTP error interceptor).
export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  errors?: Record<string, string[]>;
}
