import { describe, expect, it } from 'vitest';
import theme, { CANCEL_BUTTON_COLOR } from './theme';

describe('theme color tokens', () => {
  it('primary.main resolves to the original brand hex #FD6B00', () => {
    expect(theme.palette.primary.main).toBe('#FD6B00');
  });

  it('CANCEL_BUTTON_COLOR resolves to the original SweetAlert cancel hex #d33', () => {
    expect(CANCEL_BUTTON_COLOR).toBe('#d33');
  });

  it('error.main remains MUI default #d32f2f, unaffected by the new cancel token', () => {
    expect(theme.palette.error.main).toBe('#d32f2f');
  });
});
