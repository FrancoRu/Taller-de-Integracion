/**
 * Named HTTP status codes used to interpret API responses, instead of
 * comparing against raw numeric literals throughout the app.
 */
export const HttpStatus = {
  Ok: 200,
  Created: 201,
  NoContent: 204,
  BadRequest: 400,
  Unauthorized: 401,
  Forbidden: 403,
  NotFound: 404,
  Conflict: 409,
  InternalServerError: 500,
  ServiceUnavailable: 503,
} as const;
