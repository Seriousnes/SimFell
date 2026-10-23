import { useState } from "react";
import type { AplVocabulary } from "../api";
import { ActionItem, type DragFrom } from "./ActionItem";
import { BinIcon } from "./Icons";
import styles from "./ListSection.module.scss";
import { isDefaultList, listWarning, type Action, type ActionList } from "./model";

interface Props {
  vocabulary: AplVocabulary;
  lists: ActionList[];
  list: ActionList;
  open: Set<number>;
  dragFrom: DragFrom | null;
  setDragFrom: (from: DragFrom | null) => void;
  onToggle: (key: number) => void;
  /** Shows the details of every action in the list, or hides them all. */
  onToggleAll: (open: boolean) => void;
  onAddAction: () => void;
  onMove: (from: number, to: number) => void;
  onRemoveAction: (index: number) => void;
  onChangeAction: (key: number, patch: Partial<Action>) => void;
  /** Renames the list; false when the name is refused. */
  onRename: (name: string) => boolean;
  onRemove: () => void;
}

export function ListSection(props: Props) {
  const { vocabulary: v, lists, list, open, onToggle, onAddAction, onMove, onRemoveAction, onChangeAction } = props;
  const warning = listWarning(v, lists, list);
  const allOpen = list.actions.length > 0 && list.actions.every((a) => open.has(a.key));
  return (
    <section className={styles.List}>
      <div className={styles.Head}>
        {isDefaultList(v, list) ? <h3>Default list</h3> : <ListName {...props} />}
        <button
          type="button"
          className={styles.ToggleAll}
          hidden={!list.actions.length}
          onClick={() => props.onToggleAll(!allOpen)}
        >
          {allOpen ? "Collapse all" : "Expand all"}
        </button>
      </div>
      <p className={styles.FieldWarning} hidden={!warning}>
        {warning ?? ""}
      </p>
      <ol className={styles.Actions}>
        {list.actions.map((action, index) => (
          <ActionItem
            key={action.key}
            vocabulary={v}
            lists={lists}
            list={list}
            action={action}
            index={index}
            open={open.has(action.key)}
            dragFrom={props.dragFrom}
            setDragFrom={props.setDragFrom}
            onToggle={() => onToggle(action.key)}
            onMove={onMove}
            onRemove={() => onRemoveAction(index)}
            onChange={(patch) => onChangeAction(action.key, patch)}
          />
        ))}
      </ol>
      <button type="button" className={styles.Secondary} onClick={onAddAction}>
        Add action
      </button>
    </section>
  );
}

function ListName({ list, onRename, onRemove }: Props) {
  const [name, setName] = useState(list.name);
  const id = `apl-list-${list.name}`;
  // The name is taken on leaving the field or pressing Enter, as a change event would.
  const commit = () => {
    if (name.trim() === list.name) {
      setName(list.name);
      return;
    }
    if (!onRename(name.trim())) setName(list.name);
  };
  return (
    <>
      <h3>
        <label htmlFor={id}>Action list</label>
      </h3>
      <input
        id={id}
        className={styles.Name}
        spellCheck={false}
        autoComplete="off"
        value={name}
        onChange={(e) => setName(e.target.value)}
        onBlur={commit}
        onKeyDown={(e) => e.key === "Enter" && commit()}
      />
      <button type="button" className={styles.Remove} title="Delete list" aria-label="Delete list" onClick={onRemove}>
        <BinIcon />
      </button>
    </>
  );
}
