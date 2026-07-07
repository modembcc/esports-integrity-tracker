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
        <nav style={{ display: "flex", gap: 20 }}>
          <Link
            to="/"
            className="mono"
            style={{ fontSize: 12, color: "var(--muted)" }}
          >
            MATCHES
          </Link>
          <Link
            to="/dashboard"
            className="mono"
            style={{ fontSize: 12, color: "var(--muted)" }}
          >
            INTEGRITY
          </Link>
          <Link
            to="/admin/links"
            className="mono"
            style={{ fontSize: 12, color: "var(--muted)" }}
          >
            LINKS
          </Link>
        </nav>
        <span className="mono" style={{ fontSize: 12, color: "var(--muted)" }}>
          esports odds & integrity tracker
        </span>
      </div>
    </header>
  );
}
