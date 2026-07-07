import { afterEach, describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import Dashboard from "./Dashboard";

const flaggedMatches = [
  {
    matchId: 1,
    team1: "T1",
    team2: "Gen.G",
    scheduledTime: "2026-07-05T12:00:00Z",
    anomalies: [
      {
        outcomeName: "T1",
        fromPrice: 0.6,
        toPrice: 0.3,
        shift: 0.3,
        fromTimeUtc: "2026-07-05T08:00:00Z",
        toTimeUtc: "2026-07-05T10:00:00Z",
      },
    ],
    maxShift: 0.3,
  },
  {
    matchId: 2,
    team1: "DRX",
    team2: "KT",
    scheduledTime: "2026-07-06T12:00:00Z",
    anomalies: [
      {
        outcomeName: "DRX",
        fromPrice: 0.55,
        toPrice: 0.45,
        shift: 0.1,
        fromTimeUtc: "2026-07-06T08:00:00Z",
        toTimeUtc: "2026-07-06T09:00:00Z",
      },
    ],
    maxShift: 0.1,
  },
];

function mockDashboardFetch(anomalies: unknown, upsets: unknown, ok = true) {
  vi.stubGlobal(
    "fetch",
    vi.fn((url: string) =>
      Promise.resolve({
        ok,
        json: () => Promise.resolve(url.includes("anomalies") ? anomalies : upsets),
      }),
    ),
  );
}

describe("Dashboard", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("shows an error state when the API call fails", async () => {
    mockDashboardFetch(null, null, false);

    render(<Dashboard />, { wrapper: MemoryRouter });

    expect(await screen.findByText(/couldn't load dashboard data/i)).toBeInTheDocument();
  });

  it("shows empty-state copy when nothing is flagged", async () => {
    mockDashboardFetch([], []);

    render(<Dashboard />, { wrapper: MemoryRouter });

    expect(await screen.findByText(/no anomalies detected/i)).toBeInTheDocument();
    expect(screen.getByText(/no upsets yet/i)).toBeInTheDocument();
  });

  it("lists flagged matches once data loads", async () => {
    mockDashboardFetch(flaggedMatches, []);

    render(<Dashboard />, { wrapper: MemoryRouter });

    expect(await screen.findByText(/Gen\.G/)).toBeInTheDocument();
    expect(screen.getByText(/KT/)).toBeInTheDocument();
  });

  it("filters the flagged list by severity", async () => {
    mockDashboardFetch(flaggedMatches, []);
    const user = userEvent.setup();

    render(<Dashboard />, { wrapper: MemoryRouter });
    await screen.findByText(/Gen\.G/);

    await user.click(screen.getByRole("button", { name: "HIGH" }));

    expect(screen.getByText(/Gen\.G/)).toBeInTheDocument();
    expect(screen.queryByText(/KT/)).not.toBeInTheDocument();
  });
});
