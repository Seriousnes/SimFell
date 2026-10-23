// Inline icons drawn in the text colour.

import styles from "./Icons.module.scss";

/** The chevron of an expandable row, with the class that turns it when the row is open. */
export function ChevronIcon({ className }: { className: string }) {
  return (
    <svg className={`${styles.Icon} ${className}`} viewBox="0 0 16 16" aria-hidden="true">
      <path
        d="M6 3.5 10.5 8 6 12.5"
        fill="none"
        stroke="currentColor"
        strokeWidth="1.75"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  );
}

export function BinIcon() {
  return (
    <svg className={styles.Icon} viewBox="0 0 16 16" aria-hidden="true">
      <path
        d="M2.5 4.5h11M6.5 4.5V3a1 1 0 0 1 1-1h1a1 1 0 0 1 1 1v1.5M4 4.5l.7 8.6a1 1 0 0 0 1 .9h4.6a1 1 0 0 0 1-.9l.7-8.6M6.75 7v4.5M9.25 7v4.5"
        fill="none"
        stroke="currentColor"
        strokeWidth="1.3"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  );
}

export function UpIcon() {
  return (
    <svg className={styles.Icon} viewBox="0 0 16 16" aria-hidden="true">
      <path
        d="M8 13V3M4 7l4-4 4 4"
        fill="none"
        stroke="currentColor"
        strokeWidth="1.5"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  );
}

export function DownIcon() {
  return (
    <svg className={styles.Icon} viewBox="0 0 16 16" aria-hidden="true">
      <path
        d="M8 3v10M4 9l4 4 4-4"
        fill="none"
        stroke="currentColor"
        strokeWidth="1.5"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  );
}

export function GripIcon() {
  return (
    <svg className={styles.Icon} viewBox="0 0 16 16" aria-hidden="true">
      <g fill="currentColor">
        <circle cx="6" cy="3.5" r="1.2" />
        <circle cx="10" cy="3.5" r="1.2" />
        <circle cx="6" cy="8" r="1.2" />
        <circle cx="10" cy="8" r="1.2" />
        <circle cx="6" cy="12.5" r="1.2" />
        <circle cx="10" cy="12.5" r="1.2" />
      </g>
    </svg>
  );
}
