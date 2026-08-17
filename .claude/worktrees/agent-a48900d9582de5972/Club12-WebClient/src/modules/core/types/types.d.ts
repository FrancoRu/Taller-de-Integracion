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

export type RequestProps = {
  method: string;
  resource: string;
  configOverride?: object;
  body?: unknown;
  query?: object;
};

export type GUID = `${string}-${string}-${string}-${string}-${string}`;
