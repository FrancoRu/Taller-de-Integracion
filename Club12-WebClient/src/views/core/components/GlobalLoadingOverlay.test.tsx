import { act, render, screen, waitFor } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import GlobalLoadingOverlay from './GlobalLoadingOverlay';
import { beginRequest, endRequest } from '@/modules/core/utils/requestActivity';

// The Backdrop's exit transition keeps the spinner mounted for a moment after
// `open` flips to false, so "hidden" assertions wait it out via waitFor
// instead of asserting synchronously.
describe('GlobalLoadingOverlay', () => {
  it('is hidden while no mutating request is in flight, and shows once one starts', async () => {
    render(<GlobalLoadingOverlay />);

    expect(screen.queryByRole('progressbar')).not.toBeInTheDocument();

    act(() => beginRequest());
    expect(screen.getByRole('progressbar')).toBeInTheDocument();

    act(() => endRequest());
    await waitFor(() =>
      expect(screen.queryByRole('progressbar')).not.toBeInTheDocument()
    );
  });

  it('stays visible while more than one request overlaps, until the last one ends', async () => {
    render(<GlobalLoadingOverlay />);

    act(() => {
      beginRequest();
      beginRequest();
    });
    expect(screen.getByRole('progressbar')).toBeInTheDocument();

    act(() => endRequest());
    expect(screen.getByRole('progressbar')).toBeInTheDocument();

    act(() => endRequest());
    await waitFor(() =>
      expect(screen.queryByRole('progressbar')).not.toBeInTheDocument()
    );
  });
});
