# esports-integrity-tracker

Esports match tracker with prediction market odds and integrity anomaly detection. Built with ASP.NET, PostgreSQL, React.

docker run --name esports-postgres -e POSTGRES_PASSWORD=mypassword -e POSTGRES_DB=esports_tracker -p 1234:5432 -d postgres

dotnet ef database update

dotnet run

Invoke-RestMethod -Method Post -Uri "http://localhost:5150/api/marketlinks" `     -ContentType "application/json"`
-Body '{"matchId": 11, "slug": "lol-tsw-tes-2026-07-04"}'

Then restart the API server.
