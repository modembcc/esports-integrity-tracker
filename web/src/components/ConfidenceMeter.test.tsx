import { describe, expect, it } from "vitest";
import { render, screen } from "@testing-library/react";
import ConfidenceMeter from "./ConfidenceMeter";

describe("ConfidenceMeter", () => {
  it("renders the score", () => {
    render(<ConfidenceMeter score={42} />);
    expect(screen.getByText("42/100")).toBeInTheDocument();
  });

  it("uses the clean color for a low score", () => {
    render(<ConfidenceMeter score={10} />);
    expect(screen.getByText("10/100")).toHaveStyle({ color: "#5FBF77" });
  });

  it("uses the watch color for a mid-range score", () => {
    render(<ConfidenceMeter score={50} />);
    expect(screen.getByText("50/100")).toHaveStyle({ color: "var(--amber)" });
  });

  it("uses the flagged color for a high score", () => {
    render(<ConfidenceMeter score={85} />);
    expect(screen.getByText("85/100")).toHaveStyle({ color: "var(--red)" });
  });
});
