import { describe, expect, it } from 'vitest';
import {
  beginRequest,
  clearBlockingMessage,
  endRequest,
  getActiveRequestCount,
  getBlockingMessage,
  runWithBlockingMessage,
  setBlockingMessage,
  subscribeToRequestActivity,
} from './requestActivity';

describe('requestActivity', () => {
  it('tracks nested begin/end calls and notifies subscribers with the running count', () => {
    const seen: number[] = [];
    const unsubscribe = subscribeToRequestActivity(count => seen.push(count));

    beginRequest();
    beginRequest();
    endRequest();
    endRequest();

    expect(seen).toEqual([1, 2, 1, 0]);
    expect(getActiveRequestCount()).toBe(0);

    unsubscribe();
  });

  it('never goes negative when endRequest is called without a matching begin', () => {
    endRequest();
    expect(getActiveRequestCount()).toBe(0);
  });

  it('stops notifying a listener after it unsubscribes', () => {
    const seen: number[] = [];
    const unsubscribe = subscribeToRequestActivity(count => seen.push(count));
    unsubscribe();

    beginRequest();
    endRequest();

    expect(seen).toEqual([]);
  });
});

describe('requestActivity — blocking message', () => {
  it('has no message until one is set, and the newest set message wins', () => {
    expect(getBlockingMessage()).toBeNull();

    const first = setBlockingMessage('Restaurando…');
    expect(getBlockingMessage()).toBe('Restaurando…');

    const second = setBlockingMessage('Generando…');
    expect(getBlockingMessage()).toBe('Generando…');

    clearBlockingMessage(second);
    expect(getBlockingMessage()).toBe('Restaurando…');

    clearBlockingMessage(first);
    expect(getBlockingMessage()).toBeNull();
  });

  it('tolerates clearing messages out of order', () => {
    const a = setBlockingMessage('A');
    const b = setBlockingMessage('B');

    clearBlockingMessage(a);
    expect(getBlockingMessage()).toBe('B');

    clearBlockingMessage(b);
    expect(getBlockingMessage()).toBeNull();
  });

  it('notifies subscribers when the message changes', () => {
    let calls = 0;
    const unsubscribe = subscribeToRequestActivity(() => {
      calls += 1;
    });

    const id = setBlockingMessage('X');
    clearBlockingMessage(id);

    expect(calls).toBe(2);
    unsubscribe();
  });

  it('runWithBlockingMessage shows the message around the operation and clears it even on throw', async () => {
    await runWithBlockingMessage('Trabajando…', async () => {
      expect(getBlockingMessage()).toBe('Trabajando…');
    });
    expect(getBlockingMessage()).toBeNull();

    await expect(
      runWithBlockingMessage('Fallando…', async () => {
        throw new Error('boom');
      })
    ).rejects.toThrow('boom');
    expect(getBlockingMessage()).toBeNull();
  });
});
