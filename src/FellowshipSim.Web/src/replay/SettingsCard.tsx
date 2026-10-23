import { useEffect, useRef, useState, type FormEvent } from "react";
import { failure, replay } from "../client";
import { Card } from "../Card";
import { message, Message, noMessage, type MessageState } from "../Message";
import styles from "./SettingsCard.module.scss";

interface Props {
  hidden: boolean;
  /** The file the credentials are saved in. */
  settingsPath: string;
  /** Each change focuses the client ID field. */
  focusRequest: number;
  onSaved: () => void;
}

export function SettingsCard({ hidden, settingsPath, focusRequest, onSaved }: Props) {
  const [clientId, setClientId] = useState("");
  const [clientSecret, setClientSecret] = useState("");
  const [status, setStatus] = useState<MessageState>(noMessage);
  const clientIdInput = useRef<HTMLInputElement>(null);

  useEffect(() => {
    if (focusRequest > 0) clientIdInput.current?.focus();
  }, [focusRequest]);

  async function save(event: FormEvent) {
    event.preventDefault();
    setStatus(message("Checking..."));
    try {
      await replay.saveCredentials({ clientId, clientSecret });
      setStatus(message("Saved.", "ok"));
      setClientSecret("");
      setTimeout(onSaved, 800);
    } catch (error) {
      setStatus(message(failure(error), "error"));
    }
  }

  return (
    <Card title="FellowshipLogs API access" hidden={hidden}>
      <p className={styles.Hint}>
        Fights are downloaded from{" "}
        <a href="https://www.fellowshiplogs.com" target="_blank" rel="noopener">
          FellowshipLogs
        </a>{" "}
        with your own API client. Create one in your FellowshipLogs account, then paste its client ID and secret here.
        They are saved only on this computer (<code>{settingsPath}</code>).
      </p>
      <form className={styles.Form} onSubmit={save}>
        <input
          ref={clientIdInput}
          placeholder="Client ID"
          autoComplete="off"
          required
          value={clientId}
          onChange={(e) => setClientId(e.target.value)}
        />
        <input
          placeholder="Client secret"
          type="password"
          autoComplete="off"
          required
          value={clientSecret}
          onChange={(e) => setClientSecret(e.target.value)}
        />
        <button type="submit">Save</button>
      </form>
      <Message state={status} />
    </Card>
  );
}
