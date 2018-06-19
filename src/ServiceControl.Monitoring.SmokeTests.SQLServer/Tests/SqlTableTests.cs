namespace ServiceControl.Monitoring.SmokeTests.SQLServer.Tests
{
    using NUnit.Framework;
    using Transports.SQLServer;

    public class SqlTableTests
    {
        [Test]
        public void When_no_schema_dbo_is_used_instead()
        {
            var sqlTable = SqlTable.Parse("Endpoint");

            Assert.AreEqual("Endpoint", sqlTable.UnquotedName);
            Assert.AreEqual("dbo", sqlTable.UnquotedSchema);
        }

        [Test]
        public void When_no_catlog_specified_the_value_in_sqlTable_is_null()
        {
            var sqlTable = SqlTable.Parse("Endpoint@[some-schema]");

            Assert.AreEqual("Endpoint", sqlTable.UnquotedName);
            Assert.AreEqual(null, sqlTable.UnquotedCatalog);
        }

        [TestCase("Endpoint@[s]@[c]", "[Endpoint]", "[s]", "[c]")]
        [TestCase("Endpo]int@[schema--x]@[D234F]", "[Endpo]]int]", "[schema--x]", "[D234F]")]
        [TestCase("[Quoted]@[x]@[z]", "[Quoted]", "[x]", "[z]")]
        public void Endptoint_name_schema_and_catalog_are_parsed_from_address_string_representation(string address, string endpoint, string schema, string catalog)
        {
            var sqlTable = SqlTable.Parse(address);

            Assert.AreEqual(endpoint, sqlTable.QuotedName);
            Assert.AreEqual(schema, sqlTable.QuotedSchema);
            Assert.AreEqual(catalog, sqlTable.QuotedCatalog);
        }
    }
}