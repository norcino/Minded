using Microsoft.VisualStudio.TestTools.UnitTesting;
using MindedExample.Tests.E2E.Common;

namespace MindedExample.Api.E2ETests
{
    /// <summary>
    /// Assembly-level lifecycle hooks. When the suite runs with the PostgreSQL E2E profile,
    /// drops this run's unique database so no test databases are left behind
    /// (leftovers from crashed runs are swept by <see cref="PostgreSqlTestDatabase"/> on first use).
    /// </summary>
    [TestClass]
    public static class TestAssemblyLifecycle
    {
        [AssemblyCleanup]
        public static void AssemblyCleanup()
        {
            PostgreSqlTestDatabase.DropRunDatabase();
        }
    }
}
