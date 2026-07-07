import { describe, expect, it } from "vitest";
import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import Navbar from "./Navbar";

describe("Navbar", () => {
  it("renders links to matches, integrity dashboard, and market linking", () => {
    render(<Navbar />, { wrapper: MemoryRouter });

    expect(screen.getByRole("link", { name: "MATCHES" })).toHaveAttribute("href", "/");
    expect(screen.getByRole("link", { name: "INTEGRITY" })).toHaveAttribute(
      "href",
      "/dashboard",
    );
    expect(screen.getByRole("link", { name: "LINKS" })).toHaveAttribute(
      "href",
      "/admin/links",
    );
  });
});
