import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { describe, expect, it } from 'vitest';
import DesktopNavItems from '@/views/home/NavMenu/desktop';

describe('DesktopNavItems', () => {
  it('links to /torneos — the tournaments listing page exists and is otherwise unreachable from any nav', () => {
    render(
      <MemoryRouter>
        <DesktopNavItems />
      </MemoryRouter>
    );

    expect(screen.getByRole('link', { name: 'Torneos' })).toHaveAttribute(
      'href',
      '/torneos'
    );
  });
});
