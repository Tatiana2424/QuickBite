import { appConfig } from "../config/env";

type MonitoringContext = Record<string, unknown>;

export function initializeFrontendMonitoring() {
  window.addEventListener("error", (event) => {
    reportFrontendError(event.error instanceof Error ? event.error : new Error(event.message), {
      source: "window.error"
    });
  });

  window.addEventListener("unhandledrejection", (event) => {
    const reason = event.reason instanceof Error ? event.reason : new Error(String(event.reason));
    reportFrontendError(reason, { source: "window.unhandledrejection" });
  });
}

export function reportFrontendError(error: Error, context: MonitoringContext = {}) {
  const payload = {
    message: error.message,
    stack: error.stack,
    environment: appConfig.appEnvironment,
    context
  };

  if (appConfig.monitoringEnabled) {
    console.error("[quickbite.monitoring]", payload);
    return;
  }

  if (appConfig.appEnvironment !== "production") {
    console.warn("[quickbite.monitoring.disabled]", payload);
  }
}
