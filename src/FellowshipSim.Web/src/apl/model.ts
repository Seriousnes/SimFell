// The action-list editor's model: one list per action list, each with its actions in priority order. The server parses
// and writes the action-list text (api/apl), so the editor and the engine read the same grammar.

import type {
  AplDocumentAction,
  AplTargetSelection,
  AplDocumentError,
  AplExpressionCheck,
  AplVocabulary,
} from "../api";
import { apl } from "../client";

export interface Action {
  key: number;
  ability: string;
  condition: string | null;
  targetSelection: AplTargetSelection | null;
  targetExpression: string | null;
  comment: string | null;
  arguments: string | null;
  actionList: string | null;
}

/** An action list: the default list first, then the others. */
export interface ActionList {
  name: string;
  actions: Action[];
}

let nextKey = 1;

export function newAction(fields: Partial<Action> = {}): Action {
  return {
    key: nextKey++,
    ability: "",
    condition: null,
    targetSelection: null,
    targetExpression: null,
    comment: null,
    arguments: null,
    actionList: null,
    ...fields,
  };
}

// ---- Expressions -------------------------------------------------------------------------

const checks = new Map<string, Promise<AplExpressionCheck>>(); // `${hero}\n${text}` -> check result

export function checkExpression(hero: string, text: string): Promise<AplExpressionCheck> {
  const cacheKey = `${hero}\n${text}`;
  let check = checks.get(cacheKey);
  if (!check) {
    check = apl.check(hero, { text });
    checks.set(cacheKey, check);
  }
  return check;
}

export function problemsOf(check: AplExpressionCheck | null): string[] {
  return [...new Set((check?.tokens ?? []).map((t) => t.problem).filter((p): p is string => !!p))];
}

/** True when the text ends with a value, so the next value needs 'and' or 'or' in front of it. */
export function endsWithValue(text: string) {
  const trimmed = text.trimEnd();
  return trimmed !== "" && !/([&|!(<>=+\-*%,:]|\b(and|or|not))$/i.test(trimmed);
}

// ---- Actions -----------------------------------------------------------------------------

export function abilityName(v: AplVocabulary, slug: string) {
  return v.abilities.find((a) => a.slug === slug)?.name ?? slug;
}

export function selectionName(v: AplVocabulary, selection: string) {
  return v.targetSelections.find((s) => s.selection === selection);
}

/** run_action_list and call_action_list run another list. */
export function isListAction(v: AplVocabulary, action: Action) {
  return v.listActions.includes(action.ability);
}

/** "run_action_list" -> "Run action list". */
export function listActionName(name: string) {
  const words = name.replaceAll("_", " ");
  return words[0].toUpperCase() + words.slice(1);
}

export function actionTitle(v: AplVocabulary, action: Action) {
  if (isListAction(v, action)) return `${listActionName(action.ability)}: ${action.actionList || "pick a list"}`;
  return abilityName(v, action.ability) || "Pick an ability";
}

export function isDefaultList(v: AplVocabulary, list: ActionList) {
  return list.name === v.defaultList;
}

export function listTitle(v: AplVocabulary, list: ActionList) {
  return isDefaultList(v, list) ? "Default list" : `Action list ${list.name}`;
}

export function isRunAction(v: AplVocabulary, action: Action) {
  return action.ability === v.listActions[0];
}

/**
 * True when the list casts on every decision: it has a repeatable ability with no condition or target selection, or a
 * run_action_list with no condition naming a list that casts on every decision.
 */
function alwaysCasts(v: AplVocabulary, lists: ActionList[], name: string | null, seen = new Set<string>()): boolean {
  const list = lists.find((l) => l.name === name);
  if (!list || name == null || seen.has(name)) return false;
  seen.add(name);
  return list.actions.some((action) => {
    if (action.condition?.trim()) return false;
    if (isRunAction(v, action)) return alwaysCasts(v, lists, action.actionList, seen);
    return v.repeatableAbilities.includes(action.ability) && !action.targetSelection;
  });
}

/** The warning for a list a run_action_list names, when the list can cast nothing. */
export function runWarning(v: AplVocabulary, lists: ActionList[], name: string | null) {
  if (!name || alwaysCasts(v, lists, name)) return null;
  const example = v.repeatableAbilities[0];
  return (
    `When nothing in ${name} is ready, the hero casts nothing and the actions after this one do not run. ` +
    `End ${name} with an action that is always ready${example ? `, such as ${abilityName(v, example)} with no condition` : ""}.`
  );
}

/** The warning for a list named by a run_action_list that can cast nothing. */
export function listWarning(v: AplVocabulary, lists: ActionList[], list: ActionList) {
  const runBy = lists.some((l) => l.actions.some((a) => isRunAction(v, a) && a.actionList === list.name));
  if (!runBy || alwaysCasts(v, lists, list.name)) return null;
  const example = v.repeatableAbilities[0];
  return (
    `A Run action list action names this list. When nothing here is ready, the hero casts nothing. ` +
    `End it with an action that is always ready${example ? `, such as ${abilityName(v, example)} with no condition` : ""}.`
  );
}

/** A list name is one identifier: letters, digits and '_', and none of the words 'and', 'or' and 'not'. */
export function listNameProblem(lists: ActionList[], name: string, current: string | null = null) {
  if (!/^[A-Za-z_][A-Za-z0-9_]*$/.test(name))
    return "Name the list with letters, digits and '_', starting with a letter.";
  if (/^(and|or|not)$/i.test(name)) return `\`${name}\` is an operator.`;
  if (name !== current && lists.some((l) => l.name === name)) return `There is already a list named ${name}.`;
  return null;
}

// ---- Text --------------------------------------------------------------------------------

/** Every action in list order, with its list's name. */
export function documentActions(lists: ActionList[]): AplDocumentAction[] {
  return lists.flatMap((list) => list.actions.map(({ key: _key, ...fields }) => ({ ...fields, list: list.name })));
}

export function errorText(v: AplVocabulary, lists: ActionList[], errors: AplDocumentError[]) {
  const labels = lists.flatMap((list) => list.actions.map((_, i) => `${listTitle(v, list)} action ${i + 1}`));
  return errors.map((e) => `${labels[e.action]}: ${e.message}`).join(" ");
}

/** The default list first, then the other lists in order of their first action. */
export function listsOf(v: AplVocabulary, actions: AplDocumentAction[]): ActionList[] {
  const lists: ActionList[] = [{ name: v.defaultList, actions: [] }];
  for (const { list: name, ...fields } of actions) {
    let list = lists.find((l) => l.name === name);
    if (!list) lists.push((list = { name, actions: [] }));
    list.actions.push(newAction(fields));
  }
  return lists;
}
