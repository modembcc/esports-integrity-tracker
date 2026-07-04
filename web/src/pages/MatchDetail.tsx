import { useParams } from "react-router-dom";
import { useEffect, useState } from "react";
import OddsChart from "../components/OddsChart";
import ConfidenceMeter from "../components/ConfidenceMeter";

export default function MatchDetail() {
  const { id } = useParams();
  const [odds, setOdds] = useState<
    { time: string; odds: number; anomaly?: boolean }[]
  >([]);
  const [score, setScore] = useState(0);

  useEffect(() => {
    fetch(`${import.meta.env.VITE_API_URL}/matches/${id}/odds`)
      .then((r) => r.json())
      .then((data) => {
        setOdds(data.points ?? mockOdds);
        setScore(data.suspicionScore ?? 62);
      })
      .catch(() => {
        setOdds(mockOdds);
        setScore(62);
      });
  }, [id]);

  return (
    <div style={{ display: "grid", gap: 16 }}>
      <h2>Match #{id}</h2>
      <OddsChart data={odds} />
      <ConfidenceMeter score={score} />
    </div>
  );
}

const mockOdds = [
  { time: "10:00", odds: 1.8 },
  { time: "11:00", odds: 1.75 },
  { time: "12:00", odds: 2.4, anomaly: true },
  { time: "13:00", odds: 2.1 },
];
