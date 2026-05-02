import React from "react";
import ReactDOM from "react-dom/client";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { BrowserRouter } from "react-router-dom";
import { AuthProvider } from "./auth/AuthContext";
import { ErrorBoundary } from "./components/ErrorBoundary";
import { App } from "./App";
import { initializeFrontendMonitoring } from "./monitoring/frontendMonitoring";
import "./styles.css";

initializeFrontendMonitoring();

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: (failureCount, error) => {
        const status = typeof error === "object" && error !== null && "status" in error ? Number(error.status) : undefined;
        return failureCount < 2 && status !== 401 && status !== 404;
      },
      staleTime: 30_000
    },
    mutations: {
      retry: false
    }
  }
});

ReactDOM.createRoot(document.getElementById("root")!).render(
  <React.StrictMode>
    <ErrorBoundary>
      <QueryClientProvider client={queryClient}>
        <AuthProvider>
          <BrowserRouter>
            <App />
          </BrowserRouter>
        </AuthProvider>
      </QueryClientProvider>
    </ErrorBoundary>
  </React.StrictMode>
);
