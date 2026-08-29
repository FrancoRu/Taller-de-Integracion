import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import {
  CardGridSkeleton,
  DetailSkeleton,
  ListSkeleton,
  TableSkeleton,
} from '@/views/core/components/skeletons';

describe('skeletons', () => {
  it('TableSkeleton renders header + body cells for the given size', () => {
    const { container } = render(<TableSkeleton rows={3} columns={2} />);
    // 2 header cells + 3 rows * 2 cells = 8 skeleton elements.
    expect(container.querySelectorAll('.MuiSkeleton-root')).toHaveLength(8);
    expect(screen.getByRole('status')).toHaveAttribute('aria-busy', 'true');
  });

  it('ListSkeleton renders an avatar + two lines per item', () => {
    const { container } = render(<ListSkeleton items={4} />);
    // Each item: 1 circular + 2 text = 3 skeletons.
    expect(container.querySelectorAll('.MuiSkeleton-root')).toHaveLength(12);
  });

  it('CardGridSkeleton renders one card per count', () => {
    const { container } = render(<CardGridSkeleton count={5} />);
    expect(container.querySelectorAll('.MuiSkeleton-root')).toHaveLength(5);
  });

  it('DetailSkeleton renders a title, paragraph lines and a block', () => {
    const { container } = render(<DetailSkeleton />);
    // title (1) + 4 lines + 1 block = 6 skeletons.
    expect(container.querySelectorAll('.MuiSkeleton-root')).toHaveLength(6);
    expect(screen.getByRole('status')).toBeInTheDocument();
  });
});
