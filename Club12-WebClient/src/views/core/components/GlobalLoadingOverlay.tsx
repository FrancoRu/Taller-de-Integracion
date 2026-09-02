import { useSyncExternalStore } from 'react';
import BlockingOverlay from '@/views/core/components/BlockingOverlay';
import {
  getActiveRequestCount,
  getBlockingMessage,
  subscribeToRequestActivity,
} from '@/modules/core/utils/requestActivity';

/**
 * Mounted once at the app root. Blocks the whole screen with a spinner for
 * as long as any mutating request (save, upload, delete — anything that can
 * take a moment) is in flight, so no screen has to wire its own submitting
 * state just to stop the user from clicking twice or navigating away
 * mid-save. Subscribes to the axios-level request-activity store instead of
 * tracking anything itself. A screen can add a contextual message for a
 * specific operation via `runWithBlockingMessage`.
 */
export default function GlobalLoadingOverlay() {
  const activeCount = useSyncExternalStore(
    subscribeToRequestActivity,
    getActiveRequestCount,
    getActiveRequestCount
  );
  const message = useSyncExternalStore(
    subscribeToRequestActivity,
    getBlockingMessage,
    getBlockingMessage
  );

  return (
    <BlockingOverlay
      open={activeCount > 0 || message !== null}
      message={message ?? undefined}
    />
  );
}
