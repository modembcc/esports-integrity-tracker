import { Link } from "react-router-dom";

export default function Navbar() {
  return (
    <header
      style={{
        borderBottom: "1px solid var(--border)",
        background: "var(--panel)",
      }}
    >
      <div
        style={{
          maxWidth: 1100,
          margin: "0 auto",
          padding: "16px 20px",
          display: "flex",
          alignItems: "center",
          justifyContent: "space-between",
        }}
      >
        <Link to="/">
          <h1 style={{ fontSize: 20 }}>
            ODDS<span style={{ color: "var(--amber)" }}>WATCH</span>
          </h1>
        </Link>
        <span className="mono" style={{ fontSize: 12, color: "var(--muted)" }}>
          esports odds & integrity tracker
        </span>
      </div>
    </header>
  );
}
