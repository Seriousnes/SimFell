// FellowshipSim.Service's clients. Their types and requests (./api) are generated from the service's OpenAPI document
// when FellowshipSim.Service builds.

import { AplClient, ApiException, ReplayClient } from "./api";

export const apl = new AplClient();
export const replay = new ReplayClient();

/** The message of a failed request: the service's `error` when the response has one, otherwise its status. */
export function failure(error: unknown): string {
  if (error instanceof ApiException) return `Request failed (${error.status})`;
  if (error instanceof Error) return error.message;
  if (typeof error === "object" && error !== null && "error" in error && typeof error.error === "string")
    return error.error;
  return String(error);
}
