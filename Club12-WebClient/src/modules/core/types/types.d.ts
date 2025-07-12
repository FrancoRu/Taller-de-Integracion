import { ReactNode } from 'react';
interface ProviderProps {
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

export enum Order {
  ASC = 'asc',
  DESC = 'desc',
}

export enum Routes {
  HOME = '/',
  ABOUT_AS = '/quienes-somos',
  RULES = '/reglas',
  COPA_12 = '/copa-12',
  FEMENINO = '/femenino',
  LA_PREVIA = '/la-previa',
  CAMPEONATO = '/campeonato',
}

type RequestProps = {
  method: string;
  resource: string;
  configOverride?: object;
  body?: unknown;
  query?: object;
};

type GUID = `${string}-${string}-${string}-${string}-${string}`;
