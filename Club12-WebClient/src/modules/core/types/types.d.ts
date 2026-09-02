import { ReactNode } from 'react';
import { Order } from '@/modules/core/constants/order';
export interface ProviderProps {
  children: ReactNode;
}

export interface GenericResponsePagination<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface Filtered {
  pageNumber?: number;
  pageSize?: number;
  orderBy?: string;
  order?: Order;
}

/**
 * Per-call options for a context GET method. `silent` suppresses the global
 * blocking alert on failure so a public page can render a quiet inline retry
 * state instead — mutations (save/delete) never pass it and keep their alerts.
 * `force` skips the context's "already have it in the local list" cache hit
 * and re-fetches from the server — needed right after a mutation that
 * changes data nested inside the cached item (e.g. deleting a tournament
 * from within a season), which the cache hit would otherwise mask.
 */
export interface FetchOptions {
  silent?: boolean;
  force?: boolean;
}

export type RequestProps = {
  method: string;
  resource: string;
  configOverride?: object;
  body?: unknown;
  query?: object;
};

export type GUID = `${string}-${string}-${string}-${string}-${string}`;
