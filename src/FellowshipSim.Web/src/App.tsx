// FellowshipSim replay page: import a fight from FellowshipLogs, run a replay review, list the reviews, edit action lists.

import { useCallback, useEffect, useRef, useState } from "react";
import type { ReviewSummary } from "./api";
import { failure, replay } from "./client";
import styles from "./App.module.scss";
import { Card } from "./Card";
import { message, Message, noMessage, type MessageState } from "./Message";
import { SettingsCard } from "./replay/SettingsCard";
import { ImportCard } from "./replay/ImportCard";
import { ReviewsCard } from "./replay/ReviewsCard";
import { ActionListsCard } from "./apl/ActionListsCard";

export function App() {
  const [settingsOpen, setSettingsOpen] = useState(false);
  const [settingsPath, setSettingsPath] = useState("");
  const [settingsFocus, setSettingsFocus] = useState(0);
  const [reportMessage, setReportMessage] = useState<MessageState>(noMessage);
  const [job, setJob] = useState<MessageState | null>(null);
  const [polling, setPolling] = useState(false);
  const [reviews, setReviews] = useState<ReviewSummary[]>([]);
  const timer = useRef<number | null>(null);

  const loadReviews = useCallback(() => {
    replay
      .reviews()
      .then(setReviews)
      .catch(() => {});
  }, []);

  useEffect(() => {
    replay
      .status()
      .then((status) => {
        setSettingsOpen(!status.credentialsConfigured);
        setSettingsPath(status.settingsPath);
      })
      .catch((error) => setReportMessage(message(failure(error), "error")));
    loadReviews();
  }, [loadReviews]);

  useEffect(
    () => () => {
      if (timer.current != null) clearInterval(timer.current);
    },
    [],
  );

  function stopPolling() {
    if (timer.current != null) clearInterval(timer.current);
    timer.current = null;
    setPolling(false);
  }

  async function pollJob(id: string) {
    try {
      const current = await replay.job(id);
      if (current.status === "running") {
        setJob(message(`${current.message}...`));
        return;
      }
      stopPolling();
      if (current.status === "done") {
        const r = current.review!;
        setJob(
          message(
            <>
              Done:{" "}
              <a href={`/reviews/${encodeURIComponent(r.htmlFile)}`} target="_blank" rel="noopener">
                open the review of {r.playerName} in {r.fightName}
              </a>
            </>,
            "ok",
          ),
        );
        loadReviews();
      } else {
        setJob(message(current.error || "The replay failed.", "error"));
      }
    } catch (error) {
      stopPolling();
      setJob(message(failure(error), "error"));
    }
  }

  async function startReview(reportCode: string, fightId: number, playerId: number) {
    if (polling) return;
    setJob(message("Starting..."));
    setPolling(true);
    try {
      const started = await replay.startReview({ reportCode, fightId, playerId });
      timer.current = window.setInterval(() => pollJob(started.id), 1000);
    } catch (error) {
      setJob(message(failure(error), "error"));
      setPolling(false);
    }
  }

  return (
    <>
      <header className={styles.Top}>
        <h1>FellowshipSim</h1>
        <span className={styles.Subtitle}>
          Replay a logged fight through the engine and review how closely it matches
        </span>
      </header>

      <main className={styles.Main}>
        <SettingsCard
          hidden={!settingsOpen}
          settingsPath={settingsPath}
          focusRequest={settingsFocus}
          onSaved={() => setSettingsOpen(false)}
        />

        <ImportCard
          message={reportMessage}
          setMessage={setReportMessage}
          busy={polling}
          onEditSettings={() => {
            setSettingsOpen(true);
            setSettingsFocus((n) => n + 1);
          }}
          onReplay={startReview}
        />

        <Card title="Replay" hidden={job == null}>
          <Message state={job ?? noMessage} />
        </Card>

        <ReviewsCard reviews={reviews} />

        <ActionListsCard />
      </main>
    </>
  );
}
