import { useEffect } from 'react';
import { useLocation } from 'react-router-dom';

/**
 * Scrolls the window back to the top on every route change. A single-page app
 * keeps the previous scroll position across client-side navigations, so opening
 * a detail page (e.g. a news post) from midway down a list would otherwise land
 * the reader in the middle of the new page instead of at its heading. Renders
 * nothing; mount once near the router root.
 */
export default function ScrollToTop() {
  const { pathname } = useLocation();

  useEffect(() => {
    window.scrollTo({ top: 0, left: 0, behavior: 'auto' });
  }, [pathname]);

  return null;
}
