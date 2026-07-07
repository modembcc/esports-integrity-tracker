import { useEffect, useState, useCallback } from "react";
import { Link2, Trash2, ExternalLink } from "lucide-react";

type Match = {
  id: number;
  team1: string;
  team2: string;
  status: string;
  scheduledTime: string;
};

type MarketLink = {
  id: number;
  matchId: number;
  team1: string;
  team2: string;
  polymarketSlug: string;
  outcomeNames: string[];
  gameStartTimeUtc: string | null;
};

const API = import.meta.env.VITE_API_URL;

export default function LinkMarkets() {
  const [matches, setMatches] = useState<Match[]>([]);
  const [links, setLinks] = useState<MarketLink[]>([]);
  const [matchId, setMatchId] = useState<string>("");
  const [slug, setSlug] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [feedback, setFeedback] = useState<{ ok: boolean; msg: string } | null>(
    null,
  );

  const load = useCallback(async () => {
    const [m, l] = await Promise.all([
      fetch(`${API}/api/matches`).then((r) => r.json()),
      fetch(`${API}/api/marketlinks`).then((r) => r.json()),
    ]);
    setMatches(m);
    setLinks(l);
  }, []);

  useEffect(() => {
    load().catch(() =>
      setFeedback({
        ok: false,
        msg: "couldn't load data — is the API running?",
      }),
    );
  }, [load]);

  const linkedIds = new Set(links.map((l) => l.matchId));
  const unlinked = matches.filter((m) => !linkedIds.has(m.id));

  const submit = async () => {
    if (!matchId || !slug.trim()) {
      setFeedback({ ok: false, msg: "pick a match and enter a slug." });
      return;
    }
    setSubmitting(true);
    setFeedback(null);
    try {
      const r = await fetch(`${API}/api/marketlinks`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          matchId: Number(matchId),
          slug: cleanSlug(slug),
        }),
      });
      const body = await r.json().catch(() => null);
      if (!r.ok) {
        setFeedback({ ok: false, msg: body?.message ?? `HTTP ${r.status}` });
        return;
      }
      setFeedback({
        ok: true,
        msg: `linked — outcomes: ${body.outcomeNames?.join(" / ") ?? "?"}. prices arrive on the next sync cycle (≤15 min).`,
      });
      setSlug("");
      setMatchId("");
      await load();
    } catch {
      setFeedback({ ok: false, msg: "request failed — network or CORS?" });
    } finally {
      setSubmitting(false);
    }
  };

  const remove = async (id: number) => {
    if (
      !confirm(
        "Delete this link? Its price snapshots stay in the DB but nothing will display them.",
      )
    )
      return;
    await fetch(`${API}/api/marketlinks/${id}`, { method: "DELETE" });
    await load();
  };

  return (
    <div style={{ display: "grid", gap: 24, maxWidth: 720 }}>
      <section>
        <h2
          style={{
            display: "inline-flex",
            alignItems: "center",
            gap: 8,
            marginBottom: 12,
          }}
        >
          <Link2 size={18} color="var(--amber)" />
          Link a Polymarket Market
        </h2>

        <div
          style={{
            background: "var(--panel)",
            border: "1px solid var(--border)",
            borderRadius: 8,
            padding: 18,
            display: "grid",
            gap: 12,
          }}
        >
          <label
            className="mono"
            style={{
              fontSize: 11,
              color: "var(--muted)",
              display: "grid",
              gap: 6,
            }}
          >
            MATCH (unlinked only)
            <select
              value={matchId}
              onChange={(e) => setMatchId(e.target.value)}
              style={inputStyle}
            >
              <option value="">— select a match —</option>
              {unlinked.map((m) => (
                <option key={m.id} value={m.id}>
                  #{m.id} · {m.team1} vs {m.team2} ·{" "}
                  {new Date(m.scheduledTime).toLocaleDateString()} ({m.status})
                </option>
              ))}
            </select>
          </label>

          <label
            className="mono"
            style={{
              fontSize: 11,
              color: "var(--muted)",
              display: "grid",
              gap: 6,
            }}
          >
            POLYMARKET SLUG OR URL
            <input
              value={slug}
              onChange={(e) => setSlug(e.target.value)}
              placeholder="lol-hle1-g2-2026-07-05 — or paste the full polymarket.com URL"
              style={inputStyle}
              onKeyDown={(e) => e.key === "Enter" && submit()}
            />
          </label>

          <button
            onClick={submit}
            disabled={submitting}
            style={{
              justifySelf: "start",
              background: "var(--amber)",
              color: "var(--bg)",
              border: "none",
              borderRadius: 6,
              padding: "8px 16px",
              fontSize: 12,
              fontWeight: 500,
              cursor: submitting ? "default" : "pointer",
              opacity: submitting ? 0.6 : 1,
            }}
          >
            {submitting ? "RESOLVING…" : "LINK MARKET"}
          </button>

          {feedback && (
            <p
              className="mono"
              style={{
                fontSize: 12,
                margin: 0,
                color: feedback.ok ? "#5FBF77" : "var(--red)",
              }}
            >
              {feedback.msg}
            </p>
          )}
        </div>
      </section>

      <section>
        <h2 style={{ fontSize: 16, marginBottom: 12 }}>
          Existing Links ({links.length})
        </h2>
        {links.length === 0 ? (
          <p className="mono" style={{ fontSize: 12, color: "var(--muted)" }}>
            no links yet.
          </p>
        ) : (
          <div style={{ display: "grid", gap: 8 }}>
            {links.map((l) => (
              <div
                key={l.id}
                style={{
                  background: "var(--panel)",
                  border: "1px solid var(--border)",
                  borderRadius: 8,
                  padding: "12px 16px",
                  display: "flex",
                  justifyContent: "space-between",
                  alignItems: "center",
                  gap: 12,
                }}
              >
                <span
                  style={{
                    display: "flex",
                    flexDirection: "column",
                    gap: 2,
                    minWidth: 0,
                  }}
                >
                  <span style={{ fontSize: 14 }}>
                    {l.team1} vs {l.team2}
                  </span>
                  <a
                    href={`https://polymarket.com/market/${l.polymarketSlug}`}
                    target="_blank"
                    rel="noreferrer"
                    className="mono"
                    style={{
                      fontSize: 11,
                      color: "var(--muted)",
                      display: "inline-flex",
                      alignItems: "center",
                      gap: 4,
                      overflow: "hidden",
                      textOverflow: "ellipsis",
                      whiteSpace: "nowrap",
                    }}
                  >
                    {l.polymarketSlug} <ExternalLink size={10} />
                  </a>
                </span>
                <button
                  onClick={() => remove(l.id)}
                  title="delete link"
                  style={{
                    background: "transparent",
                    border: "1px solid var(--border)",
                    borderRadius: 6,
                    color: "var(--muted)",
                    padding: 6,
                    cursor: "pointer",
                    display: "inline-flex",
                    flexShrink: 0,
                  }}
                >
                  <Trash2 size={14} />
                </button>
              </div>
            ))}
          </div>
        )}
      </section>
    </div>
  );
}

// Accept a full polymarket.com URL and extract the slug — pasting the
// whole URL from the browser is the natural user behavior.
function cleanSlug(input: string): string {
  const trimmed = input.trim();
  try {
    const url = new URL(trimmed);
    const parts = url.pathname.split("/").filter(Boolean);
    return parts[parts.length - 1] ?? trimmed;
  } catch {
    return trimmed; // not a URL: treat as bare slug
  }
}

const inputStyle: React.CSSProperties = {
  background: "var(--bg)",
  border: "1px solid var(--border)",
  borderRadius: 6,
  color: "var(--text)",
  padding: "8px 10px",
  fontSize: 13,
  fontFamily: "var(--font-mono)",
};
