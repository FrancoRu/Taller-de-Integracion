type Listener = (activeCount: number) => void;

let activeCount = 0;
const listeners = new Set<Listener>();

/**
 * A tiny external store tracking how many mutating HTTP requests (POST/PUT/
 * DELETE — loading/uploading/saving something that can take a moment) are in
 * flight right now. `axiosUtils.sendRequest` is the single choke point every
 * request goes through, so it increments/decrements this instead of each
 * screen tracking its own `submitting` flag — `GlobalLoadingOverlay`
 * subscribes to it once, at the app root, so ANY save/upload blocks the
 * whole screen with a spinner without every call site wiring it manually.
 * GET requests are intentionally excluded: page data already has its own
 * skeleton-loading convention, and blocking the screen on every background
 * fetch would be a jarring regression (see the "no blocking modals for GETs"
 * rule the public pages already follow).
 */
export const beginRequest = (): void => {
  activeCount += 1;
  listeners.forEach(listener => listener(activeCount));
};

export const endRequest = (): void => {
  activeCount = Math.max(0, activeCount - 1);
  listeners.forEach(listener => listener(activeCount));
};

export const getActiveRequestCount = (): number => activeCount;

export const subscribeToRequestActivity = (listener: Listener): (() => void) => {
  listeners.add(listener);
  return () => {
    listeners.delete(listener);
  };
};
