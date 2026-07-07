using Xunit;

namespace EsportsTracker.Tests.Integration;

// Both API test classes spin up their own WebApplicationFactory, and each one
// runs Program.cs's db.Database.Migrate() against the SAME shared Postgres
// database. Left to xUnit's default parallel-by-class execution, two migration
// runs race against a fresh DB and one fails with "relation already exists".
// This collection forces them to run sequentially against each other.
[CollectionDefinition("Api", DisableParallelization = true)]
public class ApiCollection { }
