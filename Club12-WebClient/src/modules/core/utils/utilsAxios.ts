import axios, { AxiosError, AxiosResponse } from "axios";
import jsCookie from "js-cookie";
import envVariables from "../constants/envVariables";

const TOKEN_KEY: string = "Club12_SignInToken";
type headersContent = {
  "Content-Type": string;
  Authorization?: string;
};

type ConfigOverride = {
  headers?: headersContent;
};

const statusCodeHandlers: Record<
  number,
  ((response: AxiosResponse) => void)[]
> = {};

export const tokenIsSet = (): boolean => !!jsCookie.get(TOKEN_KEY);

export const registerToken = (newToken: string, expirationDate: Date) => {
  jsCookie.set(TOKEN_KEY, newToken, {
    expires: expirationDate,
    sameSite: "lax",
    path: "/",
  });
};

export const unregisterToken = (): void => {
  jsCookie.remove(TOKEN_KEY, {
    path: "/",
  });
};

export const getRegisteredToken = (): string | undefined =>
  jsCookie.get(TOKEN_KEY);

const buildResponse = (axiosResult: AxiosResponse): any => {
  if (axiosResult.status === 200) {
    return axiosResult.data || {};
  }
  return {};
};

const getDefaultHeaders = (): headersContent => {
  const headers: headersContent = {
    "Content-Type": "application/json; charset=utf-8",
  };

  if (tokenIsSet()) {
    const token = jsCookie.get(TOKEN_KEY);
    headers.Authorization = `Bearer ${token}`;
  }
  return headers;
};

const getHeaders = (configOverride?: ConfigOverride): headersContent => {
  let headers: headersContent = getDefaultHeaders();

  if (configOverride && configOverride.headers) {
    headers = {
      ...headers,
      ...configOverride.headers,
    };
  }

  return headers;
};

export const buildEndpoint = (resource: string): string => {
  let resourceFinal = resource;
  // if (!resourceFinal.startsWith("/")) {
  //   resourceFinal = `/${resourceFinal}`;
  // }
  return encodeURI(`${envVariables.apiUrl}/${resourceFinal}`);
};

const sendRequest = async (
  method: string,
  resource: string,
  configOverride = {},
  body = null
) => {
  const headers = getHeaders(configOverride);
  const url = buildEndpoint(resource);
  let response: AxiosResponse | null = null;
  try {
    const result = await axios.request({
      method,
      url,
      headers,
      data: body,
    });
    return result;
  } catch (error: any) {
    throw new AxiosError(error);
    // response = error.response
  } finally {
    if (response !== null) {
      const statusCode: number = (response as AxiosResponse).status;
      const codeHandlers = statusCodeHandlers[statusCode] || [];
      codeHandlers.forEach((handler) => handler(response as AxiosResponse));
    }
  }
};

/**
 * Sends a POST HTTP request.
 *
 * @param {string} resource API resource.
 * @param {any} body request body.
 * @returns
 */

export const sendPost = async (
  resource: string,
  body: any,
  configOverride?: ConfigOverride
) => {
  // TODO call sendRequest
  // const headers = getHeaders(configOverride)
  // const url = buildEndpoint(resource)
  const result = await sendRequest("POST", resource, configOverride, body); //axios.post(url, body, { headers })

  return result;
};

export const sendPut = async (
  resource: string,
  body: any,
  configOverride?: ConfigOverride
) => {
  // TODO call sendRequest
  const headers = getHeaders(configOverride);
  const url = buildEndpoint(resource);

  const result = await axios.put(url, body, { headers });

  return buildResponse(result);
};

export const sendGet = async (resource: string) => sendRequest("GET", resource);

export const sendDelete = async (
  resource: string,
  configOverride?: ConfigOverride
) => {
  // TODO call sendRequest
  const headers = getHeaders(configOverride);
  const url = buildEndpoint(resource);

  const result = await axios.delete(url, { headers });

  return buildResponse(result);
};

export const downloadfile = async (
  resource: string,
  fileNameWithExtension: string
) => {
  // TODO call sendRequest?
  // https://stackoverflow.com/a/53230807
  const headers = getHeaders();
  const url = buildEndpoint(resource);

  const result = await axios.get(url, { headers, responseType: "blob" });

  const fileUrl = window.URL.createObjectURL(new Blob([result.data]));
  const link = document.createElement("a");
  link.href = fileUrl;
  link.setAttribute("download", fileNameWithExtension); // or any other extension
  document.body.appendChild(link);
  link.click();

  link.remove();
};

export const onStatusCode = (statusCode: number, callback: () => any) => {
  if (statusCodeHandlers[statusCode]) {
    statusCodeHandlers[statusCode].push(callback);
  } else {
    statusCodeHandlers[statusCode] = [callback];
  }
};

export const onUnauthorized = (callback: () => any) => {
  onStatusCode(401, callback);
};
