import { AxiosError } from "axios";
import { describe, expect, it } from "vitest";
import { toApiError } from "../src/lib/apiErrors";

describe("toApiError", () => {
  it("maps problem-details validation failures", () => {
    const error = toApiError(
      new AxiosError("Bad request", "400", undefined, undefined, {
        status: 400,
        statusText: "Bad Request",
        headers: {},
        config: {} as never,
        data: {
          title: "Validation failed",
          detail: "Email is required.",
          traceId: "trace-1"
        }
      })
    );

    expect(error.kind).toBe("validation");
    expect(error.message).toBe("Email is required.");
    expect(error.traceId).toBe("trace-1");
  });

  it("maps network failures to transient errors", () => {
    const error = toApiError(new AxiosError("Network Error"));

    expect(error.kind).toBe("transient");
    expect(error.message).toContain("gateway is unreachable");
  });

  it("maps unauthorized responses to session-expired errors", () => {
    const error = toApiError(
      new AxiosError("Unauthorized", "401", undefined, undefined, {
        status: 401,
        statusText: "Unauthorized",
        headers: {},
        config: {} as never,
        data: {}
      })
    );

    expect(error.kind).toBe("unauthorized");
    expect(error.message).toContain("session has expired");
  });
});
