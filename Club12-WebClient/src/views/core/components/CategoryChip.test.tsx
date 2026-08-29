import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { TournamentCategory } from '@/modules/core/enum/tournament/tournamentCategory';
import CategoryChip from '@/views/core/components/CategoryChip';

describe('CategoryChip', () => {
  it('renders the masculine label', () => {
    render(<CategoryChip category={TournamentCategory.Masculine} />);

    expect(screen.getByText('Masculino')).toBeInTheDocument();
  });

  it('renders the feminine label', () => {
    render(<CategoryChip category={TournamentCategory.Feminine} />);

    expect(screen.getByText('Femenino')).toBeInTheDocument();
  });
});
