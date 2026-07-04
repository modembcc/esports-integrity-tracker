import {
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
  ReferenceDot,
} from "recharts";

type OddsPoint = { time: string; odds: number; anomaly?: boolean };

export default function OddsChart({ data }: { data: OddsPoint[] }) {
  return (
    <div
      style={{
        background: "var(--panel)",
        border: "1px solid var(--border)",
        borderRadius: 8,
        padding: 16,
      }}
    >
      <h3 style={{ fontSize: 14, color: "var(--muted)", marginBottom: 12 }}>
        Odds Over Time
      </h3>
      <ResponsiveContainer width="100%" height={260}>
        <LineChart data={data}>
          <CartesianGrid stroke="var(--border)" strokeDasharray="3 3" />
          <XAxis dataKey="time" stroke="var(--muted)" fontSize={11} />
          <YAxis stroke="var(--muted)" fontSize={11} />
          <Tooltip
            contentStyle={{
              background: "var(--bg)",
              border: "1px solid var(--border)",
            }}
          />
          <Line
            type="monotone"
            dataKey="odds"
            stroke="var(--amber)"
            strokeWidth={2}
            dot={false}
          />
          {data
            .filter((d) => d.anomaly)
            .map((d, i) => (
              <ReferenceDot
                key={i}
                x={d.time}
                y={d.odds}
                r={5}
                fill="var(--red)"
                stroke="none"
              />
            ))}
        </LineChart>
      </ResponsiveContainer>
    </div>
  );
}
