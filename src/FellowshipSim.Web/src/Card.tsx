import type { ReactNode } from "react";
import styles from "./Card.module.scss";

/** A section of the page, with its title. */
export function Card({ title, hidden, children }: { title: string; hidden?: boolean; children: ReactNode }) {
  return (
    <section className={styles.Card} hidden={hidden}>
      <h2 className={styles.Title}>{title}</h2>
      {children}
    </section>
  );
}
