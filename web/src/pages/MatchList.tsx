import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { Crown } from "lucide-react";

type MatchStatus = "Scheduled" | "Live" | "Finished" | "Cancelled";

type Match = {
  id: number;
  team1: string;
  team2: string;
  status: MatchStatus;
  scheduledTime: string;
  winner: string | null;
};

const STATUS_FILTERS: (MatchStatus | "All")[] = [
  "All",
  "Scheduled",
  "Live",
  "Finished",
];

export default function MatchList() {
  const [matches, setMatches] = useState<Match[]>([]);
  const [filter, setFilter] = useState<MatchStatus | "All">("All");
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(false);

  useEffect(() => {
    setLoading(true);
    const qs = filter === "All" ? "" : `?status=${filter}`;
    fetch(`${import.meta.env.VITE_API_URL}/api/matches${qs}`)
      .then((r) => {
        if (!r.ok) throw new Error(`HTTP ${r.status}`);
        return r.json();
      })
      .then((data: Match[]) => {
        setMatches(data);
        setError(false);
      })
      .catch(() => setError(true))
      .finally(() => setLoading(false));
  }, [filter]);

  return (
    <div>
      <div
        style={{
          display: "flex",
          justifyContent: "space-between",
          alignItems: "center",
          marginBottom: 16,
        }}
      >
        <h2>Matches</h2>
        <div style={{ display: "flex", gap: 6 }}>
          {STATUS_FILTERS.map((s) => (
            <button
              key={s}
              onClick={() => setFilter(s)}
              className="mono"
              style={{
                fontSize: 11,
                padding: "5px 10px",
                borderRadius: 6,
                cursor: "pointer",
                background: filter === s ? "var(--amber)" : "var(--panel)",
                color: filter === s ? "var(--bg)" : "var(--muted)",
                border: `1px solid ${filter === s ? "var(--amber)" : "var(--border)"}`,
              }}
            >
              {s.toUpperCase()}
            </button>
          ))}
        </div>
      </div>

      {loading && (
        <p className="mono" style={{ color: "var(--muted)" }}>
          loading matches…
        </p>
      )}

      {error && (
        <p className="mono" style={{ color: "var(--red)" }}>
          couldn't reach the API — is it running and is VITE_API_URL set?
        </p>
      )}

      {!loading && !error && matches.length === 0 && (
        <p className="mono" style={{ color: "var(--muted)" }}>
          no matches{filter !== "All" ? ` with status ${filter}` : ""}.
        </p>
      )}

      {!loading && !error && (
        <div style={{ display: "grid", gap: 10 }}>
          {matches.map((m) => (
            <Link
              key={m.id}
              to={`/matches/${m.id}`}
              style={{
                background: "var(--panel)",
                border: "1px solid var(--border)",
                borderRadius: 8,
                padding: "14px 18px",
                display: "flex",
                justifyContent: "space-between",
                alignItems: "center",
                gap: 16,
              }}
            >
              {/* matchup — takes remaining space, truncates if needed */}
              <span
                style={{
                  flex: 1,
                  minWidth: 0,
                  overflow: "hidden",
                  textOverflow: "ellipsis",
                  whiteSpace: "nowrap",
                  textAlign: "left",
                }}
              >
                <TeamName name={m.team1} winner={m.winner} />
                <span style={{ color: "var(--muted)" }}> vs </span>
                <TeamName name={m.team2} winner={m.winner} />
              </span>

              {/* timing + status */}
              <span
                style={{
                  display: "flex",
                  gap: 12,
                  alignItems: "center",
                  flexShrink: 0,
                }}
              >
                <span
                  className="mono"
                  style={{
                    fontSize: 12,
                    color: "var(--muted)",
                    whiteSpace: "nowrap",
                  }}
                >
                  {new Date(m.scheduledTime).toLocaleString(undefined, {
                    month: "short",
                    day: "numeric",
                    hour: "2-digit",
                    minute: "2-digit",
                  })}
                </span>
                <StatusBadge status={m.status} />
              </span>
            </Link>
          ))}
        </div>
      )}
    </div>
  );
}

function StatusBadge({ status }: { status: MatchStatus }) {
  const color =
    status === "Live"
      ? "var(--red)"
      : status === "Scheduled"
        ? "var(--amber)"
        : "var(--muted)";
  return (
    <span
      className="mono"
      style={{
        fontSize: 10,
        color,
        border: `1px solid ${color}`,
        borderRadius: 4,
        padding: "2px 6px",
        letterSpacing: 0.5,
      }}
    >
      {status === "Live" ? "● LIVE" : status.toUpperCase()}
    </span>
  );
}

function TeamName({ name, winner }: { name: string; winner: string | null }) {
  const won = winner !== null && name === winner;

  if (!won) return <span>{name}</span>;

  return (
    <span
      style={{
        display: "inline-flex",
        alignItems: "center",
        gap: 4,
        color: "var(--amber)",
        fontWeight: 600,
        verticalAlign: "bottom",
      }}
    >
      <Crown
        size={13}
        fill="var(--amber)"
        style={{ verticalAlign: "-2px", marginRight: 4 }}
      />
      {name}
    </span>
  );
}
