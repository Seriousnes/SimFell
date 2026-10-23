import { useState, type DragEvent } from "react";
import type { AplVocabulary } from "../api";
import { Highlight } from "./Highlight";
import { useCheck, useDebounced } from "./hooks";
import {
  actionTitle,
  isDefaultList,
  isListAction,
  listActionName,
  runWarning,
  selectionName,
  isRunAction,
  type Action,
  type ActionList,
} from "./model";
import { CodeField } from "./CodeField";
import { ConditionField } from "./ConditionField";
import styles from "./ActionItem.module.scss";
import { BinIcon, ChevronIcon, DownIcon, GripIcon, UpIcon } from "./Icons";

/** The action being dragged: its list and its place in it. */
export interface DragFrom {
  list: string;
  index: number;
}

interface Props {
  vocabulary: AplVocabulary;
  lists: ActionList[];
  list: ActionList;
  action: Action;
  index: number;
  open: boolean;
  dragFrom: DragFrom | null;
  setDragFrom: (from: DragFrom | null) => void;
  onToggle: () => void;
  onMove: (from: number, to: number) => void;
  onRemove: () => void;
  onChange: (patch: Partial<Action>) => void;
}

export function ActionItem(props: Props) {
  const { vocabulary: v, list, action, index, open, dragFrom, setDragFrom, onToggle, onMove, onRemove } = props;
  const [dragging, setDragging] = useState(false);
  const [drop, setDrop] = useState<"" | "DropBefore" | "DropAfter">("");

  // An action moves within its own list. Dropping on an action puts the dragged one in its place: above it when dragged
  // up, below it when dragged down.
  const dragOver = (event: DragEvent) => {
    if (dragFrom?.list !== list.name || dragFrom.index === index) return;
    event.preventDefault();
    setDrop(dragFrom.index < index ? "DropAfter" : "DropBefore");
  };
  const dropHere = (event: DragEvent) => {
    event.preventDefault();
    setDrop("");
    const from = dragFrom;
    setDragFrom(null);
    if (from?.list === list.name) onMove(from.index, index);
  };

  return (
    <li
      className={[styles.Action, open && styles.Open, dragging && styles.Dragging, drop && styles[drop]]
        .filter(Boolean)
        .join(" ")}
      onDragOver={dragOver}
      onDragLeave={() => setDrop("")}
      onDrop={dropHere}
    >
      <div className={styles.Head}>
        <span
          className={styles.Grip}
          title="Drag to reorder"
          draggable
          onDragStart={(event) => {
            setDragFrom({ list: list.name, index });
            event.dataTransfer.effectAllowed = "move";
            event.dataTransfer.setDragImage(event.currentTarget.closest("li")!, 20, 20);
            setDragging(true);
          }}
          onDragEnd={() => {
            setDragFrom(null);
            setDragging(false);
          }}
        >
          <GripIcon />
        </span>
        <button
          type="button"
          className={styles.Toggle}
          aria-expanded={open}
          title={open ? "Collapse" : "Expand"}
          onClick={onToggle}
        >
          <ChevronIcon className={styles.Chevron} />
          <span className={styles.Number}>{index + 1}</span>
          <span className={styles.Ability}>{actionTitle(v, action)}</span>
        </button>
        <button
          type="button"
          className={styles.IconButton}
          title="Move up"
          aria-label="Move up"
          disabled={index === 0}
          onClick={() => onMove(index, index - 1)}
        >
          <UpIcon />
        </button>
        <button
          type="button"
          className={styles.IconButton}
          title="Move down"
          aria-label="Move down"
          disabled={index === list.actions.length - 1}
          onClick={() => onMove(index, index + 1)}
        >
          <DownIcon />
        </button>
        <button
          type="button"
          className={`${styles.IconButton} ${styles.Remove}`}
          title="Delete action"
          aria-label="Delete action"
          onClick={onRemove}
        >
          <BinIcon />
        </button>
      </div>
      <Summary {...props} />
      {open && <ActionDetail {...props} />}
    </li>
  );
}

function Clause({ hero, keyword, label, text }: { hero: string; keyword: string; label?: string; text: string }) {
  const result = useCheck(hero, text);
  return (
    <div className={styles.Clause}>
      <span className={styles.Keyword}>{keyword}</span> {label != null && `${label} `}
      {result?.text === text ? <Highlight text={text} check={result.check} /> : text}
    </div>
  );
}

function Summary({ vocabulary: v, lists, action }: Props) {
  const condition = useDebounced(action.condition ?? "", 300);
  const targetExpression = useDebounced(action.targetExpression ?? "", 300);
  const warning = isRunAction(v, action) ? runWarning(v, lists, action.actionList) : null;
  return (
    <div className={styles.Summary}>
      {targetExpression.trim() && (
        <Clause
          hero={v.hero}
          keyword="target"
          label={(selectionName(v, action.targetSelection ?? "First")?.label ?? "").toLowerCase()}
          text={targetExpression}
        />
      )}
      {condition.trim() && <Clause hero={v.hero} keyword="if" text={condition} />}
      {warning && <p className={styles.FieldWarning}>{warning}</p>}
    </div>
  );
}

function ActionDetail({ vocabulary: v, lists, list, action, onChange }: Props) {
  const id = `apl-${action.key}`;
  const listAction = isListAction(v, action);
  // The field for other arguments shows when the action had them as its details opened.
  const [hasArguments] = useState(() => !!action.arguments);
  const otherLists = lists.filter((l) => l.name !== list.name && !isDefaultList(v, l));
  const knownAbility = v.abilities.some((a) => a.slug === action.ability);
  const knownList = otherLists.some((l) => l.name === action.actionList);
  const listHint =
    action.ability === v.listActions[0]
      ? "The actions after this one do not run, even when nothing in the named list is cast."
      : "When nothing in the named list is cast, the next action in this list runs.";

  const changeType = (type: string) =>
    onChange(
      type
        ? {
            ability: type,
            actionList: listAction ? action.actionList : (otherLists[0]?.name ?? null),
            targetSelection: null,
            targetExpression: null,
          }
        : { ability: "", actionList: null },
    );

  return (
    <div className={styles.Detail}>
      <label htmlFor={`${id}-type`}>Type</label>
      <select id={`${id}-type`} value={listAction ? action.ability : ""} onChange={(e) => changeType(e.target.value)}>
        <option value="">Cast an ability</option>
        {v.listActions.map((name) => (
          <option key={name} value={name}>
            {listActionName(name)}
          </option>
        ))}
      </select>

      {listAction ? (
        <>
          <label htmlFor={`${id}-list`}>List</label>
          <div>
            <select
              id={`${id}-list`}
              value={knownList ? action.actionList! : ""}
              onChange={(e) => onChange({ actionList: e.target.value || null })}
            >
              {!knownList && <option value="">{otherLists.length ? "Pick a list" : "Add an action list first"}</option>}
              {otherLists.map((l) => (
                <option key={l.name} value={l.name}>
                  {l.name}
                </option>
              ))}
            </select>
            <p className={styles.ListHint}>{listHint}</p>
          </div>
        </>
      ) : (
        <>
          <label htmlFor={`${id}-ability`}>Ability</label>
          <select id={`${id}-ability`} value={action.ability} onChange={(e) => onChange({ ability: e.target.value })}>
            {!action.ability && <option value="">Pick an ability</option>}
            {action.ability && !knownAbility && (
              <option value={action.ability}>
                {action.ability} (not a {v.hero} ability)
              </option>
            )}
            {v.abilities.map((a) => (
              <option key={a.slug} value={a.slug}>
                {a.name}
              </option>
            ))}
          </select>
        </>
      )}

      <label>Condition</label>
      <div>
        <ConditionField
          vocabulary={v}
          value={action.condition}
          placeholder={`Always. Example: ${v.resources[0]?.slug ?? "active_enemies"} >= 3 and not buff.${v.auras[0]?.slug ?? "name"}.up`}
          onChange={(condition) => onChange({ condition })}
        />
      </div>

      {!listAction && (
        <>
          <label htmlFor={`${id}-target`}>Target</label>
          <select
            id={`${id}-target`}
            value={action.targetSelection ?? ""}
            onChange={(e) => {
              const targetSelection = v.targetSelections.find((s) => s.selection === e.target.value)?.selection ?? null;
              onChange(targetSelection ? { targetSelection } : { targetSelection, targetExpression: null });
            }}
          >
            <option value="">Current target</option>
            {v.targetSelections.map((s) => (
              <option key={s.selection} value={s.selection}>
                {s.label}
              </option>
            ))}
          </select>
          <label hidden={!action.targetSelection}>Target by</label>
          <div hidden={!action.targetSelection}>
            <CodeField
              vocabulary={v}
              value={action.targetExpression}
              placeholder={`Value for each enemy. Example: debuff.${v.auras[0]?.slug ?? "name"}.remains`}
              onChange={(targetExpression) => onChange({ targetExpression })}
            />
          </div>
        </>
      )}

      {hasArguments && (
        <>
          <label htmlFor={`${id}-arguments`}>Other arguments</label>
          <input
            id={`${id}-arguments`}
            className={styles.Arguments}
            spellCheck={false}
            autoComplete="off"
            value={action.arguments ?? ""}
            onChange={(e) => onChange({ arguments: e.target.value })}
          />
        </>
      )}

      <label htmlFor={`${id}-comment`}>Comment</label>
      <textarea
        id={`${id}-comment`}
        className={styles.Comment}
        rows={2}
        value={action.comment ?? ""}
        onChange={(e) => onChange({ comment: e.target.value })}
      />
    </div>
  );
}
