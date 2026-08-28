import { onStatusCode } from '@/modules/core/utils/axiosUtils';
import { HttpStatus } from '@/modules/core/constants/httpStatus';

/**
 * Global "the database is in maintenance" banner state, flipped by any
 * request that comes back `503 Service Unavailable` (the restore-in-progress
 * gate from `MaintenanceModeMiddleware`). Deliberately a plain module-level
 * subscribable store — not a React context — so it can be wired against
 * `onStatusCode` at import time, the same already-supported extension point
 * `onUnauthorized` uses, with no axios interceptor rewrite.
 */
type Listener = () => void;

let isActive = false;
const listeners = new Set<Listener>();

const notify = (): void => {
  listeners.forEach(listener => listener());
};

/**
 * Marks the maintenance banner active and notifies every subscriber.
 */
export const activateMaintenanceBanner = (): void => {
  isActive = true;
  notify();
};

/**
 * Marks the maintenance banner inactive and notifies every subscriber.
 * Used once maintenance mode is confirmed cleared (e.g. after a manual
 * `DELETE /api/maintenance` escape hatch, or after the banner UI dismisses).
 */
export const dismissMaintenanceBanner = (): void => {
  isActive = false;
  notify();
};

/**
 * Current banner state, suitable as a `useSyncExternalStore` snapshot.
 * @returns {boolean} Whether the maintenance banner is currently active.
 */
export const getMaintenanceBannerSnapshot = (): boolean => isActive;

/**
 * Subscribes to banner state changes.
 * @param {Listener} listener - Called with no arguments whenever the banner flips.
 * @returns {() => void} Unsubscribe function.
 */
export const subscribeMaintenanceBanner = (listener: Listener): (() => void) => {
  listeners.add(listener);
  return () => listeners.delete(listener);
};

onStatusCode(HttpStatus.ServiceUnavailable, () => {
  activateMaintenanceBanner();
});
