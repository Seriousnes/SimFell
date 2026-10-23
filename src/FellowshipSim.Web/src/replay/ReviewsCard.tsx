import type { ReviewSummary } from "../api";
import { Card } from "../Card";
import styles from "./ReviewsCard.module.scss";

function ratioClass(ratio: number, good: number, warn: number) {
  const off = Math.abs(1 - ratio);
  return off <= good ? styles.Good : off <= warn ? styles.Warn : styles.Bad;
}

export function ReviewsCard({ reviews }: { reviews: ReviewSummary[] }) {
  return (
    <Card title="Reviews">
      <div>
        {reviews.length === 0 ? (
          <p className={styles.Hint}>No reviews yet.</p>
        ) : (
          <div className={styles.TableWrap}>
            <table className={styles.Table}>
              <thead>
                <tr>
                  <th>Player</th>
                  <th>Hero</th>
                  <th>Fight</th>
                  <th>Logged</th>
                  <th className={styles.Num} title="Log hits the engine reproduced">
                    Hits matched
                  </th>
                  <th
                    className={styles.Num}
                    title="Exact replay (the log decides crits, procs and targets): engine damage as a share of the log's"
                  >
                    Replay vs log
                  </th>
                  <th
                    className={styles.Num}
                    title="Free dice (only the casts follow the log; the engine rolls its own crits and procs): the engine's average over many runs as a share of the log"
                  >
                    Free dice vs log
                  </th>
                  <th
                    className={styles.Num}
                    title="Share of free-dice runs that dealt less than the log: near 50% means the log is a typical run; near 0% the engine is too high, near 100% too low"
                  >
                    Log's place
                  </th>
                  <th className={styles.Num} title="Replay steps with no issues">
                    Clean steps
                  </th>
                  <th
                    className={styles.Num}
                    title="Casts the engine would have refused but were forced to follow the log"
                  >
                    Forced casts
                  </th>
                </tr>
              </thead>
              <tbody>
                {reviews.map((r) => (
                  <ReviewRow key={r.id} review={r} />
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </Card>
  );
}

function ReviewRow({ review: r }: { review: ReviewSummary }) {
  const hits = r.logHits ? r.matchedHits / r.logHits : 0;
  const damage = r.logDamage ? r.engineDamage / r.logDamage : 0;
  const date = r.fightDate ? new Date(r.fightDate).toLocaleDateString() : "";
  const fd = r.freeDice;
  const fdDamage = fd ? fd.engineMean / fd.logDamage : 0;
  // Where the log lands among the free-dice runs: near 50% is a typical run, near 0% or 100% means the engine is off.
  const fdPlace = fd ? fd.logPercentile : 0;
  const placeClass = !fd
    ? ""
    : fdPlace >= 0.1 && fdPlace <= 0.9
      ? styles.Good
      : fdPlace >= 0.03 && fdPlace <= 0.97
        ? styles.Warn
        : styles.Bad;
  return (
    <tr>
      <td>
        <a href={`/reviews/${encodeURIComponent(r.htmlFile)}`} target="_blank" rel="noopener">
          {r.playerName}
        </a>
      </td>
      <td>{r.hero}</td>
      <td>
        {r.fightName}{" "}
        <span className={styles.Meta}>
          {r.reportCode} #{r.fightId}
        </span>
      </td>
      <td>{date}</td>
      <td className={`${styles.Num} ${ratioClass(hits, 0.02, 0.05)}`}>{(hits * 100).toFixed(1)}%</td>
      <td className={`${styles.Num} ${ratioClass(damage, 0.03, 0.08)}`}>{(damage * 100).toFixed(1)}%</td>
      {fd ? (
        <>
          <td
            className={`${styles.Num} ${ratioClass(fdDamage, 0.02, 0.05)}`}
            title={`${fd.runs} runs, engine 5-95%: ${Math.round(fd.engine5).toLocaleString()} - ${Math.round(fd.engine95).toLocaleString()}`}
          >
            <a href={`/reviews/${encodeURIComponent(r.id)}.freedice.html`} target="_blank" rel="noopener">
              {(fdDamage * 100).toFixed(1)}%
            </a>
          </td>
          <td className={`${styles.Num} ${placeClass}`}>{Math.round(fdPlace * 100)}%</td>
        </>
      ) : (
        <>
          <td className={styles.Num}>-</td>
          <td className={styles.Num}>-</td>
        </>
      )}
      <td className={styles.Num}>
        {r.passedSteps}/{r.totalSteps}
      </td>
      <td className={styles.Num}>{r.forcedCasts}</td>
    </tr>
  );
}
