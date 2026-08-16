/**
 * Named HTTP status codes used to interpret API responses, instead of
 * comparing against raw numeric literals throughout the app.
 */
export const HttpStatus = {
  Ok: 200,
  NoContent: 204,
  BadRequest: 400,
  Unauthorized: 401,
  Forbidden: 403,
  NotFound: 404,
  InternalServerError: 500,
} as const;
