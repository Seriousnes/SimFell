import { useEffect, useState } from "react";
import type { AplExpressionCheck } from "../api";
import { checkExpression } from "./model";

/** The value, once it has stayed the same for `ms` milliseconds. */
export function useDebounced<T>(value: T, ms: number): T {
  const [settled, setSettled] = useState(value);
  useEffect(() => {
    const timer = setTimeout(() => setSettled(value), ms);
    return () => clearTimeout(timer);
  }, [value, ms]);
  return settled;
}

/** The server's check of `text` for the hero, with the text it was made for (null while it loads). */
export function useCheck(hero: string, text: string): { text: string; check: AplExpressionCheck | null } | null {
  const [result, setResult] = useState<{ text: string; check: AplExpressionCheck | null } | null>(null);
  useEffect(() => {
    if (!text.trim()) return;
    let current = true;
    checkExpression(hero, text)
      .catch(() => null)
      .then((check) => current && setResult({ text, check }));
    return () => {
      current = false;
    };
  }, [hero, text]);
  return text.trim() ? result : null;
}
