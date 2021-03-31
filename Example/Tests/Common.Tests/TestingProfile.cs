namespace Common.Tests
{
    public enum TestingProfile
    {
        /// <summary>
        /// Specify that the testing profile must support unit testing mocking the database
        /// </summary>
        UnitTesting = 0,

        /// <summary>
        /// In memory SQLite database is used to support live testing in Dev environments
        /// </summary>
        E2ELive = 1,
        
        /// <summary>
        /// End to end testing targeting real database for CI (using LocalDB) or Automation testing
        /// </summary>
        E2E = 2
    }
}
