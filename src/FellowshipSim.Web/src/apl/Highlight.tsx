import type { ReactNode } from "react";
import type { AplExpressionCheck } from "../api";
import tokens from "./tokens.module.scss";

/** The text with each token coloured by its kind. The character at a parse error is marked. */
export function Highlight({ text, check }: { text: string; check: AplExpressionCheck | null }) {
  const parts: ReactNode[] = [];
  const errorAt = check?.errorStart ?? null;
  let at = 0;
  const plain = (end: number) => {
    if (end <= at) return;
    if (errorAt != null && errorAt >= at && errorAt < end) {
      parts.push(
        text.slice(at, errorAt),
        <span key={`e${errorAt}`} className={tokens.Error}>
          {text[errorAt]}
        </span>,
      );
      at = errorAt + 1;
    }
    parts.push(text.slice(at, end));
    at = end;
  };
  for (const token of check?.tokens ?? []) {
    plain(token.start);
    const error = errorAt != null && errorAt >= token.start && errorAt < token.start + token.length;
    parts.push(
      <span key={token.start} className={error ? `${tokens[token.kind]} ${tokens.Error}` : tokens[token.kind]}>
        {text.slice(token.start, token.start + token.length)}
      </span>,
    );
    at = token.start + token.length;
  }
  plain(text.length);
  if (errorAt != null && errorAt >= text.length)
    parts.push(
      <span key="end" className={tokens.Error}>
        {" "}
      </span>,
    );
  return <>{parts}</>;
}
