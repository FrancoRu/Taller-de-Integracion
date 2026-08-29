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
 */
export interface FetchOptions {
  silent?: boolean;
}

export type RequestProps = {
  method: string;
  resource: string;
  configOverride?: object;
  body?: unknown;
  query?: object;
};

export type GUID = `${string}-${string}-${string}-${string}-${string}`;
