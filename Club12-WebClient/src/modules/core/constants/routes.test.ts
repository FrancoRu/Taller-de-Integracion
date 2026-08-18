import { describe, expect, it } from 'vitest';
import routes from './routes';

describe('routes', () => {
  it('tokenInvalido resolves to the invalid-token redirect path', () => {
    expect(routes.tokenInvalido).toBe('/token-invalido');
  });

  it('apiUrl is the relative same-origin path /api (no hardcoded host)', () => {
    expect(routes.apiUrl).toBe('/api');
  });
});
