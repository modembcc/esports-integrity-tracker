export default function ConfidenceMeter({ score }: { score: number }) {
  // score: 0 (clean) to 100 (highly suspicious)
  const color =
    score > 70 ? "var(--red)" : score > 35 ? "var(--amber)" : "#5FBF77";

  return (
    <div
      style={{
        background: "var(--panel)",
        border: "1px solid var(--border)",
        borderRadius: 8,
        padding: 16,
      }}
    >
      <div
        style={{
          display: "flex",
          justifyContent: "space-between",
          marginBottom: 8,
        }}
      >
        <h3 style={{ fontSize: 14, color: "var(--muted)" }}>
          Crowd Confidence / Suspicion
        </h3>
        <span className="mono" style={{ color, fontWeight: 500 }}>
          {score}/100
        </span>
      </div>
      <div
        style={{
          position: "relative",
          height: 10,
          background: "var(--bg)",
          borderRadius: 6,
          overflow: "hidden",
        }}
      >
        <div
          style={{
            position: "absolute",
            inset: 0,
            width: `${score}%`,
            background: color,
            transition: "width 0.4s ease",
          }}
        />
      </div>
      <div
        className="mono"
        style={{
          display: "flex",
          justifyContent: "space-between",
          fontSize: 10,
          color: "var(--muted)",
          marginTop: 4,
        }}
      >
        <span>CLEAN</span>
        <span>WATCH</span>
        <span>FLAGGED</span>
      </div>
    </div>
  );
}
