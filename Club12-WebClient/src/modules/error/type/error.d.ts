export interface BadRequestResponse {
  title?: string;
  detail?: string;
  statusCode?: number;
  status?: number;
  errors?: Record<string, string[]>;
}

interface IErrorContextProp {
  errors: string[] | null;
  setError: (value: AxiosError) => void;
  setMessage: (status: number, message: string[]) => void;
}
