import { useState, type FormEvent } from "react";
import type { Report } from "../api";
import { failure, replay } from "../client";
import { Card } from "../Card";
import { message, Message, type MessageState } from "../Message";
import styles from "./ImportCard.module.scss";

function formatDuration(seconds: number) {
  const m = Math.floor(seconds / 60);
  const s = Math.round(seconds % 60);
  return `${m}:${String(s).padStart(2, "0")}`;
}

interface Props {
  message: MessageState;
  setMessage: (message: MessageState) => void;
  /** True while a replay runs: the player buttons are disabled. */
  busy: boolean;
  onEditSettings: () => void;
  onReplay: (reportCode: string, fightId: number, playerId: number) => void;
}

export function ImportCard({ message: status, setMessage, busy, onEditSettings, onReplay }: Props) {
  const [link, setLink] = useState("");
  const [report, setReport] = useState<Report | null>(null);

  async function load(event: FormEvent) {
    event.preventDefault();
    setMessage(message("Loading report..."));
    setReport(null);
    try {
      const loaded = await replay.report(link);
      setMessage(message(`${loaded.title} (${loaded.code}): ${loaded.fights.length} fights. Pick a player to replay.`));
      setReport(loaded);
    } catch (error) {
      setMessage(message(failure(error), "error"));
    }
  }

  return (
    <Card title="Import a fight">
      <form className={styles.Form} onSubmit={load}>
        <input
          placeholder="FellowshipLogs report link or code"
          autoComplete="off"
          required
          value={link}
          onChange={(e) => setLink(e.target.value)}
        />
        <button type="submit">Load report</button>
        <button type="button" className={styles.Secondary} onClick={onEditSettings}>
          API settings
        </button>
      </form>
      <Message state={status} />
      <div>{report && <Fights report={report} busy={busy} onReplay={onReplay} />}</div>
    </Card>
  );
}

function Fights({ report, busy, onReplay }: { report: Report; busy: boolean; onReplay: Props["onReplay"] }) {
  return (
    <div className={styles.Fights}>
      {report.fights
        .filter((f) => f.players.length > 0)
        .map((f) => {
          const selected = f.id === report.selectedFightId;
          return (
            <div key={f.id} className={selected ? `${styles.Fight} ${styles.Selected}` : styles.Fight}>
              <div className={styles.FightHead}>
                <span className={styles.FightName}>{f.name}</span>
                <span className={styles.Meta}>
                  Fight {f.id} · {formatDuration(f.durationSeconds)} · {f.kill ? "kill" : "wipe/ongoing"}
                  {f.downloaded ? " · downloaded" : ""}
                </span>
              </div>
              <div className={styles.Players}>
                {f.players.map((p) => {
                  const highlight = selected && p.id === report.selectedPlayerId;
                  return (
                    <button
                      key={p.id}
                      type="button"
                      className={p.supported ? undefined : styles.Unsupported}
                      disabled={!p.supported || busy}
                      title={p.supported ? `Replay ${p.name}'s ${p.hero}` : `${p.hero} is not supported yet`}
                      style={highlight ? { outline: "2px solid var(--accent)" } : undefined}
                      onClick={() => onReplay(report.code, f.id, p.id)}
                    >
                      {p.name} <span className={styles.Meta}>{p.hero}</span>
                    </button>
                  );
                })}
              </div>
            </div>
          );
        })}
    </div>
  );
}
