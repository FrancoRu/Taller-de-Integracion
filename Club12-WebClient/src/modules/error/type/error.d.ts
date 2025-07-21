export interface BadRequestResponse {
  type: string;
  title: string;
  status: number;
  detail: string;
  instance: string;
}

interface IErrorContextProp {
  errors: string[] | null;
  setError: (value: AxiosError) => void;
  setMessage: (status: number, message: string[]) => void;
}
