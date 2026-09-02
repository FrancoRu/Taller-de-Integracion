import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { describe, expect, it, vi } from 'vitest';
import MobileNavItems from '@/views/home/NavMenu/mobile';

describe('MobileNavItems', () => {
  it('links to /torneos — the tournaments listing page exists and is otherwise unreachable from any nav', () => {
    render(
      <MemoryRouter>
        <MobileNavItems onCloseDrawer={vi.fn()} />
      </MemoryRouter>
    );

    expect(screen.getByRole('link', { name: 'Torneos' })).toHaveAttribute(
      'href',
      '/torneos'
    );
  });
});
