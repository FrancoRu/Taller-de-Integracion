import '@testing-library/jest-dom';

/**
 * jsdom has no ResizeObserver. Stubbed globally, guarded so a test file's own
 * more specific stub (if any) always wins.
 */
class ResizeObserverStub {
  observe() {}
  unobserve() {}
  disconnect() {}
}

if (!window.ResizeObserver) {
  window.ResizeObserver = ResizeObserverStub as unknown as typeof ResizeObserver;
}
