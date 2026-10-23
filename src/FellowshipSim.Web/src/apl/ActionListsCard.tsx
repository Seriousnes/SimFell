// Visual action-list editor: one section per action list, each with its actions in priority order.

import { useEffect, useRef, useState } from "react";
import type { ActionListEntry, ActionListNotSaved, AplDocumentAction, AplVocabulary } from "../api";
import { Card } from "../Card";
import { apl, failure } from "../client";
import styles from "./ActionListsCard.module.scss";
import { message, Message, noMessage, type MessageState } from "../Message";
import type { DragFrom } from "./ActionItem";
import { ListSection } from "./ListSection";
import {
  documentActions,
  errorText,
  isListAction,
  listNameProblem,
  listsOf,
  listTitle,
  newAction,
  type Action,
  type ActionList,
} from "./model";

/** True when a save failed on the list: its name, or the fields that do not parse. */
function isNotSaved(error: unknown): error is ActionListNotSaved {
  return typeof error === "object" && error !== null && "errors" in error;
}

export function ActionListsCard() {
  const [heroes, setHeroes] = useState<string[]>([]);
  const [vocabulary, setVocabulary] = useState<AplVocabulary | null>(null);
  const [lists, setLists] = useState<ActionList[]>([]);
  const [open, setOpen] = useState<Set<number>>(new Set());
  const [saved, setSaved] = useState<ActionListEntry[]>([]);
  const [selectedSaved, setSelectedSaved] = useState("");
  const [name, setName] = useState("");
  const [newList, setNewList] = useState("");
  const [text, setText] = useState("");
  const [textOpen, setTextOpen] = useState(false);
  const [status, setStatus] = useState<MessageState>(noMessage);
  const [textStatus, setTextStatus] = useState<MessageState>(noMessage);
  const [dragFrom, setDragFrom] = useState<DragFrom | null>(null);
  const nameInput = useRef<HTMLInputElement>(null);
  const newListInput = useRef<HTMLInputElement>(null);

  const fail = (error: unknown) => setStatus(message(failure(error), "error"));

  async function selectHero(hero: string) {
    const v = await apl.vocabulary(hero);
    setVocabulary(v);
    return v;
  }

  async function loadSavedLists(selected = "") {
    setSaved(await apl.lists());
    setSelectedSaved(selected);
  }

  function loadActions(v: AplVocabulary, actions: AplDocumentAction[]) {
    setLists(listsOf(v, actions));
    setOpen(new Set());
  }

  useEffect(() => {
    (async () => {
      try {
        const all = await apl.heroes();
        setHeroes(all);
        const [v, savedLists] = await Promise.all([apl.vocabulary(all[0]), apl.lists()]);
        setVocabulary(v);
        setSaved(savedLists);
        setLists(listsOf(v, []));
      } catch (error) {
        setStatus(message(failure(error), "error"));
      }
    })();
  }, []);

  // The action-list text follows every change to the actions.
  useEffect(() => {
    if (!vocabulary) return;
    let current = true;
    const timer = setTimeout(async () => {
      try {
        const result = await apl.write({ actions: documentActions(lists) });
        if (!current) return;
        if (result.text != null) {
          setText(result.text);
          setTextStatus(noMessage);
        } else {
          setTextStatus(message(errorText(vocabulary, lists, result.errors), "error"));
        }
      } catch (error) {
        if (current) setTextStatus(message(failure(error), "error"));
      }
    }, 250);
    return () => {
      current = false;
      clearTimeout(timer);
    };
  }, [lists, vocabulary]);

  if (!vocabulary) {
    return (
      <Card title="Action lists">
        <Message state={status} />
      </Card>
    );
  }
  const v = vocabulary;

  const updateList = (listName: string, change: (actions: Action[]) => Action[]) =>
    setLists((all) => all.map((l) => (l.name === listName ? { ...l, actions: change(l.actions) } : l)));

  const toggle = (key: number) =>
    setOpen((keys) => {
      const next = new Set(keys);
      if (!next.delete(key)) next.add(key);
      return next;
    });

  const addAction = (listName: string) => {
    const action = newAction();
    updateList(listName, (actions) => [...actions, action]);
    setOpen((keys) => new Set(keys).add(action.key));
  };

  const moveAction = (listName: string, from: number, to: number) =>
    updateList(listName, (actions) => {
      if (to < 0 || to >= actions.length || from === to) return actions;
      const next = [...actions];
      const [moved] = next.splice(from, 1);
      next.splice(to, 0, moved);
      return next;
    });

  const removeAction = (listName: string, index: number) => {
    const key = lists.find((l) => l.name === listName)!.actions[index].key;
    updateList(listName, (actions) => actions.filter((_, i) => i !== index));
    setOpen((keys) => {
      const next = new Set(keys);
      next.delete(key);
      return next;
    });
  };

  const changeAction = (key: number, patch: Partial<Action>) =>
    setLists((all) =>
      all.map((l) => ({ ...l, actions: l.actions.map((a) => (a.key === key ? { ...a, ...patch } : a)) })),
    );

  const renameList = (list: ActionList, renamed: string) => {
    const problem = listNameProblem(lists, renamed, list.name);
    if (problem) {
      setStatus(message(problem, "error"));
      return false;
    }
    setStatus(noMessage);
    setLists((all) =>
      all.map((l) => ({
        name: l.name === list.name ? renamed : l.name,
        actions: l.actions.map((a) =>
          isListAction(v, a) && a.actionList === list.name ? { ...a, actionList: renamed } : a,
        ),
      })),
    );
    return true;
  };

  const removeList = (list: ActionList) => {
    const runBy = lists.find((l) => l.actions.some((a) => isListAction(v, a) && a.actionList === list.name));
    if (runBy) {
      setStatus(
        message(`${listTitle(v, runBy)} has an action naming ${list.name}. Change that action first.`, "error"),
      );
      return;
    }
    setStatus(noMessage);
    setLists((all) => all.filter((l) => l.name !== list.name));
  };

  const addList = () => {
    const listName = newList.trim();
    const problem = listNameProblem(lists, listName);
    if (problem) {
      setStatus(message(problem, "error"));
      newListInput.current?.focus();
      return;
    }
    setStatus(noMessage);
    const action = newAction();
    setLists((all) => [...all, { name: listName, actions: [action] }]);
    setOpen((keys) => new Set(keys).add(action.key));
    setNewList("");
  };

  const loadText = async () => {
    try {
      const result = await apl.parseText({ text });
      loadActions(v, result.actions);
      setStatus(message(`Loaded ${result.actions.length} actions.`, "ok"));
    } catch (error) {
      setTextStatus(message(failure(error), "error"));
    }
  };

  const pickSaved = async (value: string) => {
    setSelectedSaved(value);
    setStatus(noMessage);
    if (!value) {
      setName("");
      loadActions(v, []);
      return;
    }
    const [hero, savedName] = value.split("/");
    try {
      const list = await apl.read(hero, savedName);
      const heroVocabulary = await selectHero(hero);
      setName(savedName);
      if (list.error) {
        setText(list.text);
        setTextOpen(true);
        setStatus(message(`The saved text does not parse: ${list.error}`, "error"));
        return;
      }
      loadActions(heroVocabulary, list.actions ?? []);
    } catch (error) {
      fail(error);
    }
  };

  const save = async () => {
    const listName = name.trim();
    if (!listName) {
      setStatus(message("Name the action list first.", "error"));
      nameInput.current?.focus();
      return;
    }
    try {
      const result = await apl.save(v.hero, listName, { actions: documentActions(lists) });
      setStatus(message(`Saved to ${result.path}.`, "ok"));
      await loadSavedLists(`${v.hero}/${listName}`);
    } catch (error) {
      if (isNotSaved(error)) {
        setStatus(message(`${error.error} ${errorText(v, lists, error.errors)}`.trim(), "error"));
      } else {
        fail(error);
      }
    }
  };

  return (
    <Card title="Action lists">
      <p className={styles.Hint}>
        Each time the hero can act, they cast the first action from the top of the default list with a ready ability and
        a true condition. Run action list and Call action list run the actions of another list.
      </p>
      <div className={styles.Toolbar}>
        <label className={styles.Field}>
          Hero{" "}
          <select value={v.hero} onChange={(e) => selectHero(e.target.value).catch(fail)}>
            {heroes.map((h) => (
              <option key={h}>{h}</option>
            ))}
          </select>
        </label>
        <label className={styles.Field}>
          Saved{" "}
          <select value={selectedSaved} onChange={(e) => pickSaved(e.target.value)}>
            <option value="">New action list</option>
            {saved.map((l) => (
              <option key={`${l.hero}/${l.name}`} value={`${l.hero}/${l.name}`}>
                {l.name} ({l.hero})
              </option>
            ))}
          </select>
        </label>
        <input
          ref={nameInput}
          placeholder="Action list name"
          autoComplete="off"
          value={name}
          onChange={(e) => setName(e.target.value)}
        />
        <button type="button" onClick={save}>
          Save
        </button>
      </div>
      <Message state={status} />
      <div>
        {lists.map((list) => (
          <ListSection
            key={list.name}
            vocabulary={v}
            lists={lists}
            list={list}
            open={open}
            dragFrom={dragFrom}
            setDragFrom={setDragFrom}
            onToggle={toggle}
            onToggleAll={(show) =>
              setOpen((keys) => {
                const next = new Set(keys);
                for (const action of list.actions) {
                  if (show) next.add(action.key);
                  else next.delete(action.key);
                }
                return next;
              })
            }
            onAddAction={() => addAction(list.name)}
            onMove={(from, to) => moveAction(list.name, from, to)}
            onRemoveAction={(index) => removeAction(list.name, index)}
            onChangeAction={changeAction}
            onRename={(renamed) => renameList(list, renamed)}
            onRemove={() => removeList(list)}
          />
        ))}
      </div>
      <div className={styles.AddList}>
        <input
          ref={newListInput}
          placeholder="New action list name (aoe)"
          spellCheck={false}
          autoComplete="off"
          value={newList}
          onChange={(e) => setNewList(e.target.value)}
        />
        <button type="button" className={styles.Secondary} onClick={addList}>
          Add action list
        </button>
      </div>
      <details className={styles.Text} open={textOpen} onToggle={(e) => setTextOpen(e.currentTarget.open)}>
        <summary>Action list text</summary>
        <textarea spellCheck={false} rows={10} value={text} onChange={(e) => setText(e.target.value)} />
        <div className={styles.Row}>
          <button type="button" className={styles.Secondary} onClick={loadText}>
            Load this text into the editor
          </button>
        </div>
        <Message state={textStatus} />
      </details>
    </Card>
  );
}
