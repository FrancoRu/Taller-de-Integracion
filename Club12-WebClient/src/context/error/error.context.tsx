import { createContext, useEffect, useState } from "react";
import { AxiosError } from "axios";
import Swal from "sweetalert2";

interface ErrorContextProp {
  errors: string[] | null;
  setError: (value: AxiosError) => void;
  setMessage: (status: number, message: string[]) => void;
}

export const ErrorContext = createContext<ErrorContextProp | undefined>(
  undefined
);

export const ErrorProvider: React.FC<ProviderProps> = ({ children }) => {
  const [errors, setErrors] = useState<string[]>([]);

  const setError = (error: AxiosError) => {
    const axiosError = error.message as unknown as AxiosError;
    const data = axiosError.response?.data ?? "Error in the request";
    const status = axiosError.response?.status ?? 404;

    setErrors((prevErrors) => [...prevErrors, data as string]);
    setMessage(status, [data as string]);
  };

  const setMessage = (status: number, message: string[]) => {
    const stat = status < 400 ? "success" : "error";
    const messages = message.join(", ");
    Swal.fire({
      position: "center",
      icon: stat,
      title: messages,
      showConfirmButton: false,
      timer: 1500,
      color: "black",
    });
    // Swal.fire({
    //   title: "Custom width, padding, color, background.",
    //   width: 600,
    //   padding: "1em",
    //   color: "#716add",
    //   //   background: "#fff url(/images/trees.png)",
    //   backdrop: `
    // 	  rgba(0,0,123,0.4)
    // 	  url("https://media3.giphy.com/media/v1.Y2lkPTc5MGI3NjExZHFqcjVya3FzcnY4N3d6M3U3cGZiMW56aHZ2ZTlrMWV4aGxuanllNiZlcD12MV9pbnRlcm5hbF9naWZfYnlfaWQmY3Q9cw/TfiH9bCZuNDFQYpyD4/giphy.gif")
    // 	  left top
    // 	  no-repeat
    // 	`,
    // });
  };

  useEffect(() => {
    if (errors !== null) {
      const timer = setTimeout(() => {
        setErrors([]);
      }, 5000);
      return () => clearTimeout(timer);
    }
  }, [errors]);

  return (
    <ErrorContext.Provider value={{ errors, setError, setMessage }}>
      {children}
    </ErrorContext.Provider>
  );
};
