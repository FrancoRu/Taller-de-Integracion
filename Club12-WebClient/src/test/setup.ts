import '@testing-library/jest-dom';

/** jsdom has no ResizeObserver; guarded so a test file's own more specific stub still wins. */
class ResizeObserverStub {
  observe() {}
  unobserve() {}
  disconnect() {}
}

if (!window.ResizeObserver) {
  window.ResizeObserver = ResizeObserverStub as unknown as typeof ResizeObserver;
}
