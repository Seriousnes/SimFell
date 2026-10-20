// FellowshipSim replay page: import a fight from FellowshipLogs, run a replay review, list the reviews.

const $ = (id) => document.getElementById(id);

async function api(path, options = {}) {
  const response = await fetch(path, {
    headers: { "Content-Type": "application/json" },
    ...options,
  });
  const body = response.status === 204 ? null : await response.json().catch(() => null);
  if (!response.ok) throw new Error(body?.error || body?.title || `Request failed (${response.status})`);
  return body;
}

function setMessage(id, text, kind = "") {
  const el = $(id);
  el.textContent = text;
  el.className = `message ${kind}`;
}

function escapeHtml(text) {
  return String(text ?? "").replace(/[&<>"']/g, (c) => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[c]));
}

function formatDuration(seconds) {
  const m = Math.floor(seconds / 60);
  const s = Math.round(seconds % 60);
  return `${m}:${String(s).padStart(2, "0")}`;
}

// ---- Settings ----------------------------------------------------------------------------

async function loadStatus() {
  const status = await api("/api/status");
  $("settings").hidden = status.credentialsConfigured;
}

$("edit-settings").addEventListener("click", () => {
  $("settings").hidden = false;
  $("client-id").focus();
});

$("settings-form").addEventListener("submit", async (event) => {
  event.preventDefault();
  setMessage("settings-message", "Checking...");
  try {
    await api("/api/settings/fellowshiplogs", {
      method: "PUT",
      body: JSON.stringify({ clientId: $("client-id").value, clientSecret: $("client-secret").value }),
    });
    setMessage("settings-message", "Saved.", "ok");
    $("client-secret").value = "";
    setTimeout(() => ($("settings").hidden = true), 800);
  } catch (error) {
    setMessage("settings-message", error.message, "error");
  }
});

// ---- Report import -------------------------------------------------------------------------

$("report-form").addEventListener("submit", async (event) => {
  event.preventDefault();
  setMessage("report-message", "Loading report...");
  $("report").innerHTML = "";
  try {
    const report = await api(`/api/reports?link=${encodeURIComponent($("report-link").value)}`);
    setMessage("report-message", `${report.title} (${report.code}): ${report.fights.length} fights. Pick a player to replay.`);
    renderReport(report);
  } catch (error) {
    setMessage("report-message", error.message, "error");
  }
});

function renderReport(report) {
  const fights = report.fights
    .filter((f) => f.players.length > 0)
    .map((f) => {
      const selected = f.id === report.selectedFightId;
      const players = f.players
        .map((p) => {
          const highlight = selected && p.id === report.selectedPlayerId ? " style=\"outline: 2px solid var(--accent)\"" : "";
          const title = p.supported ? `Replay ${p.name}'s ${p.hero}` : `${p.hero} is not supported yet`;
          return `<button class="${p.supported ? "" : "unsupported"}" ${p.supported ? "" : "disabled"} title="${escapeHtml(title)}"${highlight}
            data-fight="${f.id}" data-player="${p.id}">${escapeHtml(p.name)} <span class="fight-meta">${escapeHtml(p.hero)}</span></button>`;
        })
        .join("");
      return `<div class="fight${selected ? " selected" : ""}">
          <div class="fight-head">
            <span class="fight-name">${escapeHtml(f.name)}</span>
            <span class="fight-meta">Fight ${f.id} · ${formatDuration(f.durationSeconds)} · ${f.kill ? "kill" : "wipe/ongoing"}${f.downloaded ? " · downloaded" : ""}</span>
          </div>
          <div class="players">${players}</div>
        </div>`;
    })
    .join("");
  $("report").innerHTML = `<div class="fights">${fights}</div>`;
  $("report").querySelectorAll("button[data-fight]").forEach((button) =>
    button.addEventListener("click", () => startReview(report.code, Number(button.dataset.fight), Number(button.dataset.player))));
}

// ---- Replay jobs ---------------------------------------------------------------------------

let polling = null;

async function startReview(reportCode, fightId, playerId) {
  if (polling) return;
  $("job").hidden = false;
  setMessage("job-message", "Starting...");
  setPlayerButtons(false);
  try {
    const job = await api("/api/reviews", { method: "POST", body: JSON.stringify({ reportCode, fightId, playerId }) });
    polling = setInterval(() => pollJob(job.id), 1000);
  } catch (error) {
    setMessage("job-message", error.message, "error");
    setPlayerButtons(true);
  }
}

async function pollJob(id) {
  try {
    const job = await api(`/api/reviews/jobs/${id}`);
    if (job.status === "running") {
      setMessage("job-message", `${job.message}...`);
      return;
    }
    clearInterval(polling);
    polling = null;
    setPlayerButtons(true);
    if (job.status === "done") {
      const r = job.review;
      setMessage("job-message", "");
      $("job-message").innerHTML = `Done: <a href="/reviews/${encodeURIComponent(r.htmlFile)}" target="_blank" rel="noopener">open the review of ${escapeHtml(r.playerName)} in ${escapeHtml(r.fightName)}</a>`;
      $("job-message").className = "message ok";
      loadReviews();
    } else {
      setMessage("job-message", job.error || "The replay failed.", "error");
    }
  } catch (error) {
    clearInterval(polling);
    polling = null;
    setPlayerButtons(true);
    setMessage("job-message", error.message, "error");
  }
}

function setPlayerButtons(enabled) {
  $("report").querySelectorAll("button[data-fight]:not(.unsupported)").forEach((b) => (b.disabled = !enabled));
}

// ---- Reviews -------------------------------------------------------------------------------

function ratioClass(ratio, good, warn) {
  const off = Math.abs(1 - ratio);
  return off <= good ? "good" : off <= warn ? "warn" : "bad";
}

async function loadReviews() {
  const reviews = await api("/api/reviews");
  if (reviews.length === 0) {
    $("reviews").innerHTML = `<p class="hint">No reviews yet.</p>`;
    return;
  }
  const rows = reviews
    .map((r) => {
      const hits = r.logHits ? r.matchedHits / r.logHits : 0;
      const damage = r.logDamage ? r.engineDamage / r.logDamage : 0;
      const date = r.fightDate ? new Date(r.fightDate).toLocaleDateString() : "";
      const fd = r.freeDice;
      const fdDamage = fd ? fd.engineMean / fd.logDamage : 0;
      // Where the log lands among the free-dice runs: near 50% is a typical run, near 0% or 100% means the engine is off.
      const fdPlace = fd ? fd.logPercentile : 0;
      const placeClass = !fd ? "" : fdPlace >= 0.1 && fdPlace <= 0.9 ? "good" : fdPlace >= 0.03 && fdPlace <= 0.97 ? "warn" : "bad";
      const freeDiceCells = fd
        ? `<td class="num ${ratioClass(fdDamage, 0.02, 0.05)}" title="${fd.runs} runs, engine 5-95%: ${Math.round(fd.engine5).toLocaleString()} - ${Math.round(fd.engine95).toLocaleString()}"><a href="/reviews/${encodeURIComponent(r.id)}.freedice.html" target="_blank" rel="noopener">${(fdDamage * 100).toFixed(1)}%</a></td>
          <td class="num ${placeClass}">${Math.round(fdPlace * 100)}%</td>`
        : `<td class="num">-</td><td class="num">-</td>`;
      return `<tr>
          <td><a href="/reviews/${encodeURIComponent(r.htmlFile)}" target="_blank" rel="noopener">${escapeHtml(r.playerName)}</a></td>
          <td>${escapeHtml(r.hero)}</td>
          <td>${escapeHtml(r.fightName)} <span class="fight-meta">${escapeHtml(r.reportCode)} #${r.fightId}</span></td>
          <td>${date}</td>
          <td class="num ${ratioClass(hits, 0.02, 0.05)}">${(hits * 100).toFixed(1)}%</td>
          <td class="num ${ratioClass(damage, 0.03, 0.08)}">${(damage * 100).toFixed(1)}%</td>
          ${freeDiceCells}
          <td class="num">${r.passedSteps}/${r.totalSteps}</td>
          <td class="num">${r.forcedCasts}</td>
        </tr>`;
    })
    .join("");
  $("reviews").innerHTML = `<div class="table-wrap"><table>
      <thead><tr>
        <th>Player</th><th>Hero</th><th>Fight</th><th>Logged</th>
        <th class="num" title="Log hits the engine reproduced">Hits matched</th>
        <th class="num" title="Exact replay (the log decides crits, procs and targets): engine damage as a share of the log's">Replay vs log</th>
        <th class="num" title="Free dice (only the casts follow the log; the engine rolls its own crits and procs): the engine's average over many runs as a share of the log">Free dice vs log</th>
        <th class="num" title="Share of free-dice runs that dealt less than the log: near 50% means the log is a typical run; near 0% the engine is too high, near 100% too low">Log's place</th>
        <th class="num" title="Replay steps with no issues">Clean steps</th>
        <th class="num" title="Casts the engine would have refused but were forced to follow the log">Forced casts</th>
      </tr></thead><tbody>${rows}</tbody></table></div>`;
}

loadStatus().catch((error) => setMessage("report-message", error.message, "error"));
loadReviews().catch(() => {});
