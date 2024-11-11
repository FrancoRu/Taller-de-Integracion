import { useRouteError } from "react-router-dom";

interface ErrorDetails {
  statusText?: string;
  message: string;
}

export default function ErrorPage() {
  const error = useRouteError() as ErrorDetails | { message: string };

  return (
    <div id="error-page">
      <h1>Oops!</h1>
      <p>Sorry, an unexpected error has occurred.</p>
      <p>
        <i>
          {(error as ErrorDetails).statusText ||
            (error as ErrorDetails).message}
        </i>
      </p>
    </div>
  );
}
