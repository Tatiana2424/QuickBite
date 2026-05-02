import { Component, ErrorInfo, ReactNode } from "react";
import { reportFrontendError } from "../monitoring/frontendMonitoring";

interface ErrorBoundaryState {
  hasError: boolean;
}

export class ErrorBoundary extends Component<{ children: ReactNode }, ErrorBoundaryState> {
  state: ErrorBoundaryState = { hasError: false };

  static getDerivedStateFromError(): ErrorBoundaryState {
    return { hasError: true };
  }

  componentDidCatch(error: Error, info: ErrorInfo) {
    reportFrontendError(error, { componentStack: info.componentStack });
  }

  render() {
    if (this.state.hasError) {
      return (
        <main className="content">
          <section className="panel" role="alert">
            <p className="eyebrow">QuickBite</p>
            <h1>Something went wrong</h1>
            <p className="muted">Refresh the page and try again. If this keeps happening, check the frontend logs.</p>
          </section>
        </main>
      );
    }

    return this.props.children;
  }
}
