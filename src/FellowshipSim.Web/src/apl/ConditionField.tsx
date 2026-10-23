// A condition edited as rows (Builder) or as text (Text). Both write action-list text; text with a part the builder has no
// row for (arithmetic) is edited as text. The server reads text into rows (check.condition); the builder writes them back
// as text.

import { Fragment, useState, type ReactNode } from "react";
import type { AplConditionItem, AplName, AplVocabulary } from "../api";
import { CodeField } from "./CodeField";
import styles from "./ConditionField.module.scss";
import { useCheck, useDebounced } from "./hooks";
import { BinIcon } from "./Icons";

/** A builder row, with the kind picked on a row with no name yet. */
type ConditionRow = Omit<AplConditionItem, "items"> & { items: ConditionRow[] | null; kind?: string };

const comparisons = [">=", ">", "=", "!=", "<=", "<"];

interface Kind {
  id: string;
  label: string;
  names: AplName[];
  properties?: string[];
}

/** The kinds of value a row reads: each scope of the vocabulary, a resource, and a named fight value. */
function conditionKinds(v: AplVocabulary): Kind[] {
  const labels: Record<string, string> = { buff: "Buff", debuff: "Debuff on target", cooldown: "Cooldown" };
  return [
    ...Object.keys(v.scopes).map((scope) => ({
      id: scope,
      label: labels[scope] ?? scope,
      names: v.scopeNames[scope],
      properties: v.scopes[scope],
    })),
    { id: "resource", label: "Resource", names: v.resources },
    { id: "named", label: "Fight value", names: v.named.map((n) => ({ slug: n, name: n })) },
  ];
}

interface PathParts {
  kind: string;
  name: string;
  property?: string;
}

/** "buff.ice_blitz.up" -> { kind: "buff", name: "ice_blitz", property: "up" }; a resource or a named value has no property. */
function splitPath(v: AplVocabulary, path: string): PathParts {
  const parts = path.split(".");
  if (parts.length >= 3 && v.scopes[parts[0]])
    return { kind: parts[0], name: parts[1], property: parts.slice(2).join(".") };
  const resource = path.startsWith(v.resourcePrefix) ? path.slice(v.resourcePrefix.length) : path;
  if (v.resources.some((r) => r.slug === resource)) return { kind: "resource", name: resource };
  return { kind: "named", name: path };
}

function joinPath(v: AplVocabulary, { kind, name, property }: PathParts) {
  return v.scopes[kind] ? `${kind}.${name}.${property}` : name;
}

/** A row with a kind and no name yet, and a group with no such row, are in the builder only: the text has what is picked. */
function isComplete(item: ConditionRow): boolean {
  return item.items ? item.items.some(isComplete) : !!item.path;
}

/**
 * Rows as text: each join as its word, a group in parentheses, 'not' before a value or a group. 'not' binds to one value,
 * so a negated comparison is in parentheses.
 */
function conditionText(items: ConditionRow[]): string {
  return items
    .filter(isComplete)
    .map((item, i) => {
      const value = item.items
        ? `(${conditionText(item.items)})`
        : item.comparison
          ? `${item.path} ${item.comparison} ${item.value ?? 0}`
          : item.path!;
      const negated = !item.not ? value : item.comparison ? `not (${value})` : `not ${value}`;
      return i === 0 ? negated : ` ${item.join ?? "and"} ${negated}`;
    })
    .join("");
}

/** The rows of the group at `path` (indexes of groups from the top). */
function groupAt(rows: ConditionRow[], path: number[]): ConditionRow[] {
  return path.reduce((items, index) => items[index].items!, rows);
}

function storedMode() {
  try {
    return localStorage.getItem("apl-condition-mode") ?? "builder";
  } catch {
    return "builder";
  }
}

interface Props {
  vocabulary: AplVocabulary;
  value: string | null;
  placeholder: string;
  onChange: (text: string) => void;
}

export function ConditionField({ vocabulary: v, value, placeholder, onChange }: Props) {
  const text = value ?? "";
  const [mode, setMode] = useState(storedMode);
  // The rows the builder last wrote, with their text: they hold rows the text has no part for yet.
  const [built, setBuilt] = useState<{ text: string; rows: ConditionRow[] } | null>(null);
  // The join and 'not' picked on each group's empty row, by the group's path.
  const [placeholders, setPlaceholders] = useState<Map<string, { join: string; not: boolean }>>(new Map());
  const read = useCheck(v.hero, useDebounced(text, 200));
  // The builder's own text has its rows; other text is read into rows by the server. null: the text has no rows.
  const rows = built?.text === text ? built.rows : !text.trim() ? [] : read ? (read.check?.condition ?? null) : null;

  const building = mode === "builder" && rows != null;

  const pickMode = (next: string) => {
    setMode(next);
    try {
      localStorage.setItem("apl-condition-mode", next);
    } catch {
      // The choice lasts until the page reloads.
    }
  };

  /** Applies a change to a copy of the rows, writes them as the condition's text, and shows them again. */
  const commit = (change: (next: ConditionRow[]) => void) => {
    const next = structuredClone(rows ?? []);
    change(next);
    const written = conditionText(next);
    setBuilt({ text: written, rows: next });
    onChange(written);
  };

  const kinds = conditionKinds(v);

  const valueRow = (item: ConditionRow, group: number[], index: number, depth: number) => {
    const update = (patch: Partial<ConditionRow>) =>
      commit((next) => {
        const target = groupAt(next, group)[index];
        Object.assign(target, patch);
        if (target.path) delete target.kind;
      });
    const path = item.path ? splitPath(v, item.path) : { kind: item.kind ?? kinds[0].id, name: "" };
    const kind = kinds.find((k) => k.id === path.kind)!;
    const known = kind.names.some((n) => n.slug === path.name);
    const properties =
      kind.properties && path.property != null && !kind.properties.includes(path.property)
        ? [path.property, ...kind.properties]
        : kind.properties;
    return (
      <div key={`${group.join(".")}:${index}`} className={styles.Row}>
        {rowStart(
          item,
          index,
          depth,
          (join) => update({ join }),
          () => update({ not: !item.not }),
        )}
        {/* The selects wrap under each other, so a narrow row keeps its join, 'not' and group rails in place. */}
        <div className={styles.Fields}>
          {/* A new kind clears the name: the row waits for one. */}
          <select
            aria-label="Kind"
            className={styles.Kind}
            value={path.kind}
            onChange={(e) => update({ kind: e.target.value, path: null, comparison: null, value: null })}
          >
            {kinds.map((k) => (
              <option key={k.id} value={k.id}>
                {k.label}
              </option>
            ))}
          </select>
          {!item.path ? (
            <select
              aria-label="Name"
              className={styles.Name}
              value=""
              onChange={(e) =>
                e.target.value &&
                update({ path: joinPath(v, { kind: kind.id, name: e.target.value, property: kind.properties?.[0] }) })
              }
            >
              <option value="" />
              {nameOptions(kind)}
            </select>
          ) : (
            <>
              <select
                aria-label="Name"
                className={styles.Name}
                value={path.name}
                onChange={(e) => update({ path: joinPath(v, { ...path, name: e.target.value }) })}
              >
                {!known && (
                  <option value={path.name}>
                    {path.name} (not a {v.hero} name)
                  </option>
                )}
                {nameOptions(kind)}
              </select>
              {properties && (
                <select
                  aria-label="Property"
                  className={styles.Property}
                  value={path.property}
                  onChange={(e) => update({ path: joinPath(v, { ...path, property: e.target.value }) })}
                >
                  {properties.map((p) => (
                    <option key={p} value={p}>
                      {p}
                    </option>
                  ))}
                </select>
              )}
              <select
                aria-label="Comparison"
                className={styles.Comparison}
                value={item.comparison ?? ""}
                onChange={(e) => {
                  const comparison = e.target.value || null;
                  update({ comparison, value: comparison ? (item.value ?? 0) : null });
                }}
              >
                <option value="" />
                {comparisons.map((c) => (
                  <option key={c} value={c}>
                    {c}
                  </option>
                ))}
              </select>
              {item.comparison && (
                <NumberField
                  key={item.value ?? 0}
                  value={item.value ?? 0}
                  onChange={(number) => update({ value: number })}
                />
              )}
            </>
          )}
        </div>
        {deleteButton("Delete condition", () => commit((next) => groupAt(next, group).splice(index, 1)))}
      </div>
    );
  };

  // Each group ends with an empty row. Picking its kind makes it a row (or a group), and a new empty row follows.
  const placeholderRow = (items: ConditionRow[], group: number[], depth: number) => {
    const key = group.join(".");
    const state = placeholders.get(key) ?? { join: "and", not: false };
    const setState = (patch: Partial<typeof state>) =>
      setPlaceholders((all) => new Map(all).set(key, { ...state, ...patch }));
    return (
      <div key={`${key}:placeholder`} className={`${styles.Row} ${styles.Placeholder}`}>
        {rowStart(
          state,
          items.length,
          depth,
          (join) => setState({ join }),
          () => setState({ not: !state.not }),
        )}
        <select
          aria-label="Kind"
          className={styles.Kind}
          value=""
          onChange={(e) => {
            const picked = e.target.value;
            if (!picked) return;
            const fields = { join: state.join, not: state.not };
            setPlaceholders((all) => {
              const next = new Map(all);
              next.delete(key);
              return next;
            });
            commit((next) =>
              groupAt(next, group).push(
                picked === "group"
                  ? { ...fields, path: null, comparison: null, value: null, items: [] }
                  : { ...fields, kind: picked, path: null, comparison: null, value: null, items: null },
              ),
            );
          }}
        >
          <option value="" />
          {kinds.map((k) => (
            <option key={k.id} value={k.id}>
              {k.label}
            </option>
          ))}
          <option value="group">Group ( )</option>
        </select>
      </div>
    );
  };

  const renderItems = (items: ConditionRow[], group: number[], depth: number): ReactNode[] => [
    ...items.flatMap((item, index) => {
      if (!item.items) return [valueRow(item, group, index, depth)];
      const inner = [...group, index];
      const update = (patch: Partial<ConditionRow>) =>
        commit((next) => Object.assign(groupAt(next, group)[index], patch));
      return [
        <div key={`${inner.join(".")}:open`} className={styles.Row}>
          {rowStart(
            item,
            index,
            depth,
            (join) => update({ join }),
            () => update({ not: !item.not }),
          )}
          <span className={styles.Paren}>(</span>
          {deleteButton("Delete group", () => commit((next) => groupAt(next, group).splice(index, 1)))}
        </div>,
        ...renderItems(item.items, inner, depth + 1),
        <div key={`${inner.join(".")}:close`} className={styles.Row}>
          <Indent depth={depth} />
          <span className={styles.Lead} />
          <span className={styles.Paren}>)</span>
        </div>,
      ];
    }),
    placeholderRow(items, group, depth),
  ];

  return (
    <div className={styles.Condition}>
      <div className={styles.Segmented} role="group" aria-label="Condition editor">
        <button
          type="button"
          aria-pressed={building}
          disabled={rows == null}
          title={rows == null ? "Arithmetic is edited as text" : ""}
          onClick={() => pickMode("builder")}
        >
          Builder
        </button>
        <button type="button" aria-pressed={!building} onClick={() => pickMode("text")}>
          Text
        </button>
      </div>
      <div className={styles.Builder} hidden={!building}>
        {rows != null && renderItems(rows, [], 0)}
      </div>
      <CodeField vocabulary={v} value={value} placeholder={placeholder} hidden={building} onChange={onChange} />
    </div>
  );
}

/** The rails of the groups the row is in, its join (or 'if' on the first row) and its 'not'. */
function rowStart(
  item: { join?: string | null; not: boolean },
  index: number,
  depth: number,
  setJoin: (join: string) => void,
  toggleNot: () => void,
) {
  return (
    <>
      <Indent depth={depth} />
      {index === 0 ? (
        <span className={styles.If}>{depth === 0 ? "if" : ""}</span>
      ) : (
        <select
          aria-label="Join"
          className={styles.Join}
          value={item.join ?? "and"}
          onChange={(e) => setJoin(e.target.value)}
        >
          <option value="and">and</option>
          <option value="or">or</option>
        </select>
      )}
      <button type="button" className={styles.Not} aria-pressed={!!item.not} onClick={toggleNot}>
        not
      </button>
    </>
  );
}

/** Aliases share a name ("Winter Orbs" is worb and winter_orbs), so a shared name shows its slug. */
function nameOptions(kind: Kind) {
  return kind.names.map((n) => (
    <option key={n.slug} value={n.slug}>
      {kind.names.filter((other) => other.name === n.name).length > 1 ? `${n.name} (${n.slug})` : n.name}
    </option>
  ));
}

function deleteButton(label: string, onClick: () => void) {
  return (
    <button type="button" className={styles.Remove} title={label} aria-label={label} onClick={onClick}>
      <BinIcon />
    </button>
  );
}

/** A row in a group starts under its group's '(': a space as wide as a join and a 'not', then the group's rail. */
function Indent({ depth }: { depth: number }) {
  return (
    <>
      {Array.from({ length: depth }, (_, d) => (
        <Fragment key={d}>
          <span className={styles.Lead} />
          <span className={styles.Rail} />
        </Fragment>
      ))}
    </>
  );
}

/** The number a comparison reads; the condition takes it on leaving the field. */
function NumberField({ value, onChange }: { value: number; onChange: (value: number) => void }) {
  const [draft, setDraft] = useState(String(value));
  return (
    <input
      type="number"
      step="any"
      className={styles.Value}
      aria-label="Value"
      value={draft}
      onChange={(e) => setDraft(e.target.value)}
      onBlur={() => onChange(draft === "" ? 0 : Number(draft))}
      onKeyDown={(e) => e.key === "Enter" && onChange(draft === "" ? 0 : Number(draft))}
    />
  );
}
