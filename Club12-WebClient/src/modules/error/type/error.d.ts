export interface BadRequestResponse {
  title: string;
  detail: string;
  statusCode: number;
}

interface IErrorContextProp {
  errors: string[] | null;
  setError: (value: AxiosError) => void;
  setMessage: (status: number, message: string[]) => void;
}
