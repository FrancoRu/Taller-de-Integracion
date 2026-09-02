type Listener = (activeCount: number) => void;

let activeCount = 0;
const listeners = new Set<Listener>();

// A LIFO stack of contextual overlay messages. Each is pushed with a unique id
// so it can be popped out of order (nested/overlapping operations); the newest
// one wins as the visible message. Empty stack => no message, just the spinner.
const messageStack: { id: number; text: string }[] = [];
let nextMessageId = 1;

const notify = (): void => {
  listeners.forEach(listener => listener(activeCount));
};

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
  notify();
};

export const endRequest = (): void => {
  activeCount = Math.max(0, activeCount - 1);
  notify();
};

export const getActiveRequestCount = (): number => activeCount;

/**
 * Sets a contextual message on the global blocking overlay for the duration of
 * a long operation (e.g. "Restaurando la base de datos. No cierres esta
 * página…"). Returns an id to pass to {@link clearBlockingMessage}. Prefer
 * {@link runWithBlockingMessage}, which pairs the two automatically.
 */
export const setBlockingMessage = (text: string): number => {
  const id = nextMessageId++;
  messageStack.push({ id, text });
  notify();
  return id;
};

export const clearBlockingMessage = (id: number): void => {
  const index = messageStack.findIndex(entry => entry.id === id);
  if (index !== -1) {
    messageStack.splice(index, 1);
    notify();
  }
};

export const getBlockingMessage = (): string | null =>
  messageStack.length > 0 ? messageStack[messageStack.length - 1].text : null;

/**
 * Runs `operation` with `message` shown on the global blocking overlay, always
 * clearing it afterwards (success or throw). The overlay is already visible for
 * any mutating request in flight; this only adds the contextual text.
 */
export const runWithBlockingMessage = async <T>(
  message: string,
  operation: () => Promise<T>
): Promise<T> => {
  const id = setBlockingMessage(message);
  try {
    return await operation();
  } finally {
    clearBlockingMessage(id);
  }
};

export const subscribeToRequestActivity = (listener: Listener): (() => void) => {
  listeners.add(listener);
  return () => {
    listeners.delete(listener);
  };
};
