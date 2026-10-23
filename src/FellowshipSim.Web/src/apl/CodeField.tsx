import { useLayoutEffect, useRef, useState, type KeyboardEvent } from "react";
import type { AplVocabulary } from "../api";
import { completionsFor, hasMore, type Completion } from "./completion";
import styles from "./CodeField.module.scss";
import { Highlight } from "./Highlight";
import { useCheck, useDebounced } from "./hooks";
import { checkExpression, problemsOf } from "./model";
import tokens from "./tokens.module.scss";

interface Props {
  vocabulary: AplVocabulary;
  value: string | null;
  placeholder: string;
  onChange: (text: string) => void;
  /** Hides the input; its messages still show. */
  hidden?: boolean;
}

interface CompletionList {
  items: Completion[];
  selected: number;
  left: number;
  top: number;
}

/**
 * A multi-line code input with its text coloured underneath, and the completion list under its caret. Typing a name, a
 * '.' or Ctrl+Space opens the list; the arrow keys pick, Enter or Tab writes the pick in place of the part at the caret,
 * and Escape closes it. A pick with parts after it is written with its '.', and the list opens on those parts.
 */
export function CodeField({ vocabulary, value, placeholder, onChange, hidden = false }: Props) {
  const text = value ?? "";
  const field = useRef<HTMLDivElement>(null);
  const input = useRef<HTMLTextAreaElement>(null);
  const measure = useRef<HTMLPreElement>(null);
  const caret = useRef<{ at: number; reopen: boolean } | null>(null);
  const [completion, setCompletion] = useState<CompletionList | null>(null);
  const latest = useCheck(vocabulary.hero, useDebounced(text, 150));
  const check = text.trim() ? (latest?.check ?? null) : null;

  // The caret's place in the field: the text before it, laid out like the highlight layer, ends at the caret.
  const caretPlace = () => {
    const el = input.current!;
    const pre = measure.current!;
    const marker = document.createElement("span");
    marker.textContent = "​";
    pre.replaceChildren(el.value.slice(0, el.selectionStart), marker);
    const place = { left: marker.offsetLeft, top: marker.offsetTop + marker.offsetHeight };
    pre.replaceChildren();
    return place;
  };

  const openCompletion = (always = false) => {
    const el = input.current!;
    const word = /[A-Za-z0-9_.]*$/.exec(el.value.slice(0, el.selectionStart))![0];
    if (document.activeElement !== el || /^\d/.test(word) || (!word && !always)) return setCompletion(null);
    const items = completionsFor(vocabulary, word).slice(0, 50);
    if (!items.length) return setCompletion(null);
    const place = caretPlace();
    setCompletion({
      items,
      selected: 0,
      left: Math.max(0, Math.min(place.left, field.current!.clientWidth - 260)),
      top: place.top + 4,
    });
  };

  // After each render the field fits its text, and a written pick leaves the cursor after it.
  useLayoutEffect(() => {
    const el = input.current!;
    el.style.height = "auto";
    el.style.height = `${el.scrollHeight + 2}px`;
    if (caret.current != null) {
      const { at, reopen } = caret.current;
      caret.current = null;
      el.setSelectionRange(at, at);
      if (reopen) openCompletion();
    }
  });

  const accept = (item: Completion) => {
    const el = input.current!;
    const start = el.selectionStart;
    const partial = /[A-Za-z0-9_]*$/.exec(text.slice(0, start))![0];
    const rest = /^[A-Za-z0-9_]*/.exec(text.slice(start))![0];
    const more = hasMore(item);
    const written = item.text + (more ? "." : "");
    setCompletion(null);
    caret.current = { at: start - partial.length + written.length, reopen: more };
    onChange(text.slice(0, start - partial.length) + written + text.slice(start + rest.length));
  };

  // The editor writes 'and', 'or' and 'not': on leaving the field, '&', '|' and '!' become their words.
  const toWords = async () => {
    setCompletion(null);
    const result = await checkExpression(vocabulary.hero, text).catch(() => null);
    if (input.current?.value !== text || result?.inWords == null || result.inWords === text) return;
    onChange(result.inWords);
  };

  // True when the key is the completion list's.
  const completionKey = (event: KeyboardEvent) => {
    if (event.key === " " && event.ctrlKey) {
      openCompletion(true);
      return true;
    }
    if (!completion) return false;
    const { items, selected } = completion;
    switch (event.key) {
      case "ArrowDown":
      case "ArrowUp":
        setCompletion({
          ...completion,
          selected: (selected + (event.key === "ArrowDown" ? 1 : items.length - 1)) % items.length,
        });
        return true;
      case "Enter":
      case "Tab":
        accept(items[selected]);
        return true;
      case "Escape":
        setCompletion(null);
        return true;
      case "ArrowLeft":
      case "ArrowRight":
      case "Home":
      case "End":
        setCompletion(null);
        return false;
      default:
        return false;
    }
  };

  return (
    <div>
      <div
        ref={field}
        className={check?.error ? `${styles.CodeField} ${styles.Invalid}` : styles.CodeField}
        hidden={hidden}
      >
        <pre aria-hidden="true">{latest?.text === text ? <Highlight text={text} check={latest.check} /> : text}</pre>
        <pre ref={measure} aria-hidden="true" style={{ visibility: "hidden" }} />
        <textarea
          ref={input}
          rows={1}
          spellCheck={false}
          autoComplete="off"
          placeholder={placeholder}
          value={text}
          onChange={(e) => {
            onChange(e.target.value);
            const inputType = (e.nativeEvent as InputEvent).inputType ?? "";
            if (inputType.startsWith("insert") || inputType.startsWith("delete")) {
              caret.current = { at: e.target.selectionStart, reopen: true };
            } else {
              setCompletion(null);
            }
          }}
          onBlur={toWords}
          onClick={() => setCompletion(null)}
          onKeyDown={(e) => {
            if (completionKey(e)) {
              e.preventDefault();
              return;
            }
            // Enter would put a line break in an action line.
            if (e.key === "Enter") e.preventDefault();
          }}
        />
        <ul
          className={styles.Completions}
          role="listbox"
          hidden={!completion}
          style={completion ? { left: completion.left, top: completion.top } : undefined}
        >
          {completion?.items.map((item, i) => (
            <li
              key={item.text}
              role="option"
              aria-selected={i === completion.selected}
              className={i === completion.selected ? styles.Selected : undefined}
              ref={i === completion.selected ? (li) => li?.scrollIntoView({ block: "nearest" }) : undefined}
              onMouseDown={(e) => {
                e.preventDefault();
                accept(item);
              }}
            >
              <span className={item.kind ? tokens[item.kind] : undefined}>
                {item.text}
                {hasMore(item) ? "." : ""}
              </span>
              {item.detail && <span className={styles.CompletionDetail}>{item.detail}</span>}
            </li>
          ))}
        </ul>
      </div>
      <div>
        {check?.error && <p className={styles.FieldError}>{check.error}</p>}
        {text.trim() &&
          problemsOf(check).map((p) => (
            <p key={p} className={styles.FieldWarning}>
              {p}
            </p>
          ))}
      </div>
    </div>
  );
}
