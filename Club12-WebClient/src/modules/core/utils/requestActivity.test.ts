import { describe, expect, it } from 'vitest';
import {
  beginRequest,
  endRequest,
  getActiveRequestCount,
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
