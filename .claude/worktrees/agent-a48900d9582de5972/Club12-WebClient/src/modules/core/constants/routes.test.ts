import { describe, expect, it } from 'vitest';
import routes from './routes';

describe('routes', () => {
  it('tokenInvalido resolves to the invalid-token redirect path', () => {
    expect(routes.tokenInvalido).toBe('/token-invalido');
  });

  it('apiUrl still resolves to a non-empty string (existing entry unaffected)', () => {
    expect(routes.apiUrl).toContain('/api');
  });
});
