interface ProviderProps {
  children: ReactNode;
}

export interface GenericResponsePagination<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface Filetered {
  pageNumber: number | null;
  pageSize: number | null;
  orderBy: string | null;
  order: Order | null;
}
export enum Order {
  ASC = "asc",
  DESC = "desc",
}

export enum Routes {
  HOME = "/",
  ABOUT_AS = "/quienes-somos",
  RULES = "/reglas",
  COPA_12 = "/copa-12",
  FEMENINO = "/femenino",
  LA_PREVIA = "/la-previa",
  CAMPEONATO = "/campeonato",
}
