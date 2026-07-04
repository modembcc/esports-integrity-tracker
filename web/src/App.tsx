import { BrowserRouter, Routes, Route } from "react-router-dom";
import Navbar from "./components/Navbar";
import MatchList from "./pages/MatchList";
import MatchDetail from "./pages/MatchDetail";
import "./theme.css";

export default function App() {
  return (
    <BrowserRouter>
      <Navbar />
      <main style={{ maxWidth: 1100, margin: "0 auto", padding: "24px 20px" }}>
        <Routes>
          <Route path="/" element={<MatchList />} />
          <Route path="/matches/:id" element={<MatchDetail />} />
        </Routes>
      </main>
    </BrowserRouter>
  );
}
