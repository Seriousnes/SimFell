import type { ReactNode } from "react";
import styles from "./Message.module.scss";

export type MessageKind = "" | "ok" | "error";

export interface MessageState {
  text: ReactNode;
  kind: MessageKind;
}

export const noMessage: MessageState = { text: "", kind: "" };

const kindClass: Record<MessageKind, string> = { "": "", ok: styles.Ok, error: styles.Error };

export function message(text: ReactNode, kind: MessageKind = ""): MessageState {
  return { text, kind };
}

/** A status line under a form: plain, "ok" (green) or "error" (red). */
export function Message({ state }: { state: MessageState }) {
  return <p className={`${styles.Message} ${kindClass[state.kind]}`}>{state.text}</p>;
}
