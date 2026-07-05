import { useParams } from "react-router-dom";
import { useEffect, useState } from "react";
import {
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
  ReferenceLine,
} from "recharts";

type OddsResponse = {
  matchId: number;
  team1: string;
  team2: string;
  gameStartTimeUtc: string | null;
  series: { outcome: string; points: { t: number; p: number }[] }[];
  // Shape comes from AnomalyDetectionService — tighten this type
  // once the anomaly result record is final.
  anomalies: Record<string, unknown>[];
};

const SERIES_COLORS = ["var(--amber)", "var(--blue)"];

export default function MatchDetail() {
  const { id } = useParams();
  const [data, setData] = useState<OddsResponse | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    fetch(`${import.meta.env.VITE_API_URL}/api/matches/${id}/odds`)
      .then((r) => {
        if (r.status === 404)
          throw new Error("No odds market linked to this match yet.");
        if (!r.ok) throw new Error(`HTTP ${r.status}`);
        return r.json();
      })
      .then(setData)
      .catch((e) => setError(e.message));
  }, [id]);

  if (error)
    return (
      <p className="mono" style={{ color: "var(--muted)" }}>
        {error}
      </p>
    );
  if (!data)
    return (
      <p className="mono" style={{ color: "var(--muted)" }}>
        loading odds…
      </p>
    );

  // Merge per-outcome series into rows keyed by timestamp.
  // Tokens sample independently, so rows may have gaps — connectNulls handles it.
  const rows = new Map<number, Record<string, number>>();
  for (const s of data.series) {
    for (const pt of s.points) {
      const row = rows.get(pt.t) ?? { t: pt.t };
      row[s.outcome] = pt.p;
      rows.set(pt.t, row);
    }
  }
  const chartData = [...rows.values()].sort(
    (a, b) => (a.t as number) - (b.t as number),
  );
  const outcomeNames = data.series.map((s) => s.outcome);

  return (
    <div style={{ display: "grid", gap: 16 }}>
      <h2>
        {data.team1} <span style={{ color: "var(--muted)" }}>vs</span>{" "}
        {data.team2}
      </h2>

      <div
        style={{
          background: "var(--panel)",
          border: "1px solid var(--border)",
          borderRadius: 8,
          padding: 16,
        }}
      >
        <h3 style={{ fontSize: 14, color: "var(--muted)", marginBottom: 12 }}>
          Implied Win Probability (Polymarket)
        </h3>
        {chartData.length === 0 ? (
          <p className="mono" style={{ fontSize: 12, color: "var(--muted)" }}>
            market linked, no price history yet — waiting on the next sync
            cycle.
          </p>
        ) : (
          <ResponsiveContainer width="100%" height={280}>
            <LineChart data={chartData}>
              <CartesianGrid stroke="var(--border)" strokeDasharray="3 3" />
              <XAxis
                dataKey="t"
                type="number"
                domain={["dataMin", "dataMax"]}
                scale="time"
                tickFormatter={(t) =>
                  new Date(t).toLocaleTimeString(undefined, {
                    hour: "2-digit",
                    minute: "2-digit",
                  })
                }
                stroke="var(--muted)"
                fontSize={11}
              />
              <YAxis
                domain={[0, 1]}
                tickFormatter={(v) => `${Math.round(v * 100)}%`}
                stroke="var(--muted)"
                fontSize={11}
              />
              <Tooltip
                labelFormatter={(t) => new Date(t as number).toLocaleString()}
                formatter={(v: number) => `${(v * 100).toFixed(1)}%`}
                contentStyle={{
                  background: "var(--bg)",
                  border: "1px solid var(--border)",
                }}
              />
              {outcomeNames.map((name, i) => (
                <Line
                  key={name}
                  type="stepAfter"
                  dataKey={name}
                  connectNulls
                  stroke={SERIES_COLORS[i % SERIES_COLORS.length]}
                  strokeWidth={2}
                  dot={false}
                />
              ))}
              {data.gameStartTimeUtc && (
                <ReferenceLine
                  x={new Date(data.gameStartTimeUtc).getTime()}
                  stroke="var(--muted)"
                  strokeDasharray="4 4"
                  label={{ value: "start", fill: "var(--muted)", fontSize: 11 }}
                />
              )}
            </LineChart>
          </ResponsiveContainer>
        )}
      </div>

      {/* Legend */}
      {outcomeNames.length > 0 && (
        <div style={{ display: "flex", gap: 16 }}>
          {outcomeNames.map((name, i) => (
            <span
              key={name}
              className="mono"
              style={{
                fontSize: 12,
                color: "var(--muted)",
                display: "inline-flex",
                alignItems: "center",
                gap: 6,
              }}
            >
              <span
                style={{
                  width: 12,
                  height: 3,
                  background: SERIES_COLORS[i % SERIES_COLORS.length],
                  display: "inline-block",
                }}
              />
              {name}
            </span>
          ))}
        </div>
      )}

      {data.anomalies.length > 0 && (
        <div
          style={{
            background: "var(--panel)",
            border: "1px solid var(--red)",
            borderRadius: 8,
            padding: 16,
          }}
        >
          <h3 style={{ fontSize: 14, color: "var(--red)", marginBottom: 8 }}>
            Flagged Movements ({data.anomalies.length})
          </h3>
          {/* TODO: render fields once the anomaly result shape is final */}
          {data.anomalies.map((a, i) => (
            <p
              key={i}
              className="mono"
              style={{ fontSize: 12, margin: "4px 0" }}
            >
              {JSON.stringify(a)}
            </p>
          ))}
        </div>
      )}
    </div>
  );
}
