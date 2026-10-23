import type { AplTokenKind, AplVocabulary } from "../api";

/** One '.'-separated part of a path: the kind its text is coloured by, its name, and the parts after it. */
export interface CompletionNode {
  children: Map<string, CompletionNode>;
  kind: AplTokenKind | "";
  detail: string;
  leaf: boolean;
}

export interface Completion extends CompletionNode {
  text: string;
}

const trees = new WeakMap<AplVocabulary, CompletionNode>();

/**
 * Every path a condition can read, as a tree of its '.'-separated parts: scope, name, property; a resource, with or
 * without its prefix; a named value; and the words 'and', 'or' and 'not'. Each node has the kind its text is coloured by.
 */
function completionTree(v: AplVocabulary): CompletionNode {
  const cached = trees.get(v);
  if (cached) return cached;
  const root: CompletionNode = { children: new Map(), kind: "", detail: "", leaf: false };
  const add = (parts: string[], kind: AplTokenKind, detail = "") => {
    let node = root;
    parts.forEach((part, i) => {
      if (!node.children.has(part)) node.children.set(part, { children: new Map(), kind, detail: "", leaf: false });
      node = node.children.get(part)!;
      if (i === parts.length - 1) {
        node.leaf = true;
        node.detail = detail;
      }
    });
  };
  const scopeKinds: Record<string, AplTokenKind> = { buff: "Buff", debuff: "Debuff", cooldown: "Cooldown" };
  for (const [scope, properties] of Object.entries(v.scopes)) {
    for (const name of v.scopeNames[scope]) {
      for (const property of properties) add([scope, name.slug, property], scopeKinds[scope] ?? "Named");
      root.children.get(scope)!.children.get(name.slug)!.detail = name.name === name.slug ? "" : name.name;
    }
  }
  for (const resource of v.resources) {
    add([resource.slug], "Resource", resource.name);
    add([...v.resourcePrefix.split(".").filter(Boolean), resource.slug], "Resource", resource.name);
  }
  for (const named of v.named) add(named.split("."), "Named");
  for (const word of ["and", "or", "not"]) add([word], "Operator");
  trees.set(v, root);
  return root;
}

/**
 * The parts after `word`'s last '.', starting with its text after that '.': those starting with it first, then those
 * containing it.
 */
export function completionsFor(v: AplVocabulary, word: string): Completion[] {
  const parts = word.toLowerCase().split(".");
  const partial = parts.pop()!;
  let node: CompletionNode | undefined = completionTree(v);
  for (const part of parts) {
    node = node.children.get(part);
    if (!node) return [];
  }
  const entries = [...node.children].map(([text, child]) => ({ text, ...child }));
  const starting = entries.filter((e) => e.text.startsWith(partial));
  const containing = partial ? entries.filter((e) => !e.text.startsWith(partial) && e.text.includes(partial)) : [];
  const found = [...starting, ...containing];
  // A finished name has nothing to complete.
  if (found.length === 1 && found[0].text === partial && found[0].children.size === 0) return [];
  return found;
}

/** True when the pick has parts after it, so it is written with its '.'. */
export function hasMore(item: CompletionNode) {
  return item.children.size > 0 && !item.leaf;
}
