import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { AlertTriangle, TrendingUp } from "lucide-react";

type Anomaly = {
  outcomeName: string;
  fromPrice: number;
  toPrice: number;
  shift: number;
  fromTimeUtc: string;
  toTimeUtc: string;
};

type FlaggedMatch = {
  matchId: number;
  team1: string;
  team2: string;
  scheduledTime: string;
  anomalies: Anomaly[];
  maxShift: number;
};

type Upset = {
  matchId: number;
  team1: string;
  team2: string;
  winner: string;
  upset: {
    winnerOutcomeName: string;
    winnerPreMatchPrice: number;
    upsetMagnitude: number;
    pricedAtUtc: string;
  };
};

type Severity = "watch" | "high";

const severityOf = (shift: number): Severity =>
  shift > 0.25 ? "high" : "watch";
const severityColor = (s: Severity) =>
  s === "high" ? "var(--red)" : "var(--amber)";

const fmtPct = (p: number) => `${(p * 100).toFixed(0)}%`;
const fmtWindow = (from: string, to: string) => {
  const mins = (new Date(to).getTime() - new Date(from).getTime()) / 60000;
  if (mins < 90) return `${Math.round(mins)}m`;
  if (mins < 48 * 60) return `${(mins / 60).toFixed(1)}h`;
  return `${(mins / 1440).toFixed(1)}d`;
};

export default function Dashboard() {
  const [flagged, setFlagged] = useState<FlaggedMatch[]>([]);
  const [upsets, setUpsets] = useState<Upset[]>([]);
  const [severityFilter, setSeverityFilter] = useState<Severity | "all">("all");
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(false);

  useEffect(() => {
    const base = import.meta.env.VITE_API_URL;
    Promise.all([
      fetch(`${base}/api/matches/anomalies`).then((r) => {
        if (!r.ok) throw new Error();
        return r.json();
      }),
      fetch(`${base}/api/matches/upsets`).then((r) => {
        if (!r.ok) throw new Error();
        return r.json();
      }),
    ])
      .then(([a, u]) => {
        setFlagged(a);
        setUpsets(u);
      })
      .catch(() => setError(true))
      .finally(() => setLoading(false));
  }, []);

  if (loading)
    return (
      <p className="mono" style={{ color: "var(--muted)" }}>
        scanning markets…
      </p>
    );
  if (error)
    return (
      <p className="mono" style={{ color: "var(--red)" }}>
        couldn't load dashboard data — is the API running?
      </p>
    );

  const visible = flagged.filter(
    (f) =>
      severityFilter === "all" || severityOf(f.maxShift) === severityFilter,
  );

  return (
    <div style={{ display: "grid", gap: 24 }}>
      {/* ── Flagged matches ── */}
      <section>
        <div
          style={{
            display: "flex",
            justifyContent: "space-between",
            alignItems: "center",
            marginBottom: 12,
          }}
        >
          <h2 style={{ display: "inline-flex", alignItems: "center", gap: 8 }}>
            <AlertTriangle size={18} color="var(--red)" />
            Flagged Pre-Match Movement
          </h2>
          <div style={{ display: "flex", gap: 6 }}>
            {(["all", "high", "watch"] as const).map((s) => (
              <button
                key={s}
                onClick={() => setSeverityFilter(s)}
                style={{
                  fontSize: 11,
                  padding: "5px 10px",
                  borderRadius: 6,
                  cursor: "pointer",
                  background:
                    severityFilter === s ? "var(--amber)" : "var(--panel)",
                  color: severityFilter === s ? "var(--bg)" : "var(--muted)",
                  border: `1px solid ${severityFilter === s ? "var(--amber)" : "var(--border)"}`,
                }}
              >
                {s.toUpperCase()}
              </button>
            ))}
          </div>
        </div>

        {visible.length === 0 ? (
          <p className="mono" style={{ fontSize: 12, color: "var(--muted)" }}>
            no anomalies detected
            {severityFilter !== "all"
              ? ` at severity "${severityFilter}"`
              : " across monitored markets"}{" "}
            — threshold is a {"15"}-point pre-match deviation.
          </p>
        ) : (
          <div style={{ display: "grid", gap: 10 }}>
            {visible.map((f) => {
              const worst = f.anomalies.reduce((a, b) =>
                b.shift > a.shift ? b : a,
              );
              const sev = severityOf(f.maxShift);
              const rose = worst.toPrice > worst.fromPrice;
              return (
                <Link
                  key={f.matchId}
                  to={`/matches/${f.matchId}`}
                  style={{
                    background: "var(--panel)",
                    border: `1px solid ${severityColor(sev)}`,
                    borderRadius: 8,
                    padding: "14px 18px",
                    display: "flex",
                    justifyContent: "space-between",
                    alignItems: "center",
                    gap: 16,
                  }}
                >
                  <span
                    style={{
                      display: "flex",
                      flexDirection: "column",
                      gap: 4,
                      minWidth: 0,
                    }}
                  >
                    <span
                      style={{
                        overflow: "hidden",
                        textOverflow: "ellipsis",
                        whiteSpace: "nowrap",
                      }}
                    >
                      {f.team1}{" "}
                      <span style={{ color: "var(--muted)" }}>vs</span>{" "}
                      {f.team2}
                    </span>
                    <span
                      className="mono"
                      style={{ fontSize: 11, color: "var(--muted)" }}
                    >
                      {worst.outcomeName} {rose ? "rose" : "fell"}{" "}
                      {fmtPct(worst.fromPrice)} → {fmtPct(worst.toPrice)}
                      {" over "}
                      {fmtWindow(worst.fromTimeUtc, worst.toTimeUtc)}
                      {f.anomalies.length > 1 &&
                        ` · ${f.anomalies.length} signals`}
                    </span>
                  </span>

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
                        fontSize: 16,
                        fontWeight: 500,
                        color: severityColor(sev),
                      }}
                    >
                      {rose ? "+" : "−"}
                      {(worst.shift * 100).toFixed(0)}pts
                    </span>
                    <span
                      className="mono"
                      style={{
                        fontSize: 10,
                        color: severityColor(sev),
                        border: `1px solid ${severityColor(sev)}`,
                        borderRadius: 4,
                        padding: "2px 6px",
                        letterSpacing: 0.5,
                      }}
                    >
                      {sev.toUpperCase()}
                    </span>
                  </span>
                </Link>
              );
            })}
          </div>
        )}
      </section>

      {/* ── Upsets ── */}
      <section>
        <h2
          style={{
            display: "inline-flex",
            alignItems: "center",
            gap: 8,
            marginBottom: 12,
          }}
        >
          <TrendingUp size={18} color="var(--amber)" />
          Upsets
        </h2>
        {upsets.length === 0 ? (
          <p className="mono" style={{ fontSize: 12, color: "var(--muted)" }}>
            no upsets yet — every favorite has held.
          </p>
        ) : (
          <div style={{ display: "grid", gap: 10 }}>
            {upsets.map((u) => (
              <Link
                key={u.matchId}
                to={`/matches/${u.matchId}`}
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
                <span
                  style={{
                    minWidth: 0,
                    overflow: "hidden",
                    textOverflow: "ellipsis",
                    whiteSpace: "nowrap",
                  }}
                >
                  <span style={{ color: "var(--amber)", fontWeight: 600 }}>
                    {u.winner}
                  </span>
                  <span style={{ color: "var(--muted)" }}> beat </span>
                  {u.winner === u.team1 ? u.team2 : u.team1}
                </span>
                <span
                  className="mono"
                  style={{ fontSize: 12, color: "var(--muted)", flexShrink: 0 }}
                >
                  won at {fmtPct(u.upset.winnerPreMatchPrice)} implied
                </span>
              </Link>
            ))}
          </div>
        )}
      </section>
    </div>
  );
}
