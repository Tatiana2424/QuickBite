import axios from "axios";
import { apiBaseUrl } from "../config/env";
import { toApiError } from "./apiErrors";

const apiClientOptions = {
  baseURL: apiBaseUrl,
  timeout: 10000,
  headers: {
    "X-QuickBite-Client": "quickbite-web"
  }
};

export const anonymousApiClient = axios.create(apiClientOptions);
export const apiClient = axios.create(apiClientOptions);

let accessTokenProvider: (() => Promise<string | null> | string | null) | undefined;
let unauthorizedHandler: (() => void) | undefined;

export function configureApiAuth(options: {
  getAccessToken: () => Promise<string | null> | string | null;
  onUnauthorized: () => void;
}) {
  accessTokenProvider = options.getAccessToken;
  unauthorizedHandler = options.onUnauthorized;
}

apiClient.interceptors.request.use(async (config) => {
  const accessToken = await accessTokenProvider?.();

  if (accessToken) {
    config.headers.Authorization = `Bearer ${accessToken}`;
  }

  return config;
});

function normalizeApiError(error: unknown) {
  return Promise.reject(toApiError(error));
}

anonymousApiClient.interceptors.response.use((response) => response, normalizeApiError);

apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    const apiError = toApiError(error);

    if (apiError.kind === "unauthorized") {
      unauthorizedHandler?.();
    }

    return Promise.reject(apiError);
  }
);
