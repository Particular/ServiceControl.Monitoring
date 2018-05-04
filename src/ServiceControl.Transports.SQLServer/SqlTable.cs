namespace ServiceControl.Transports.SQLServer
{
    using System;
    using System.Linq;

    class SqlTable
    {
        SqlTable(string name, string schema, string catalog)
        {
            QuotedName = SqlNameHelper.Quote(name);
            QuotedSchema = SqlNameHelper.Quote(schema);
            QuotedCatalog = SqlNameHelper.Quote(catalog);
        }

        public string QuotedName { get; }
        public string UnquotedName => SqlNameHelper.Unquote(QuotedName);

        public string QuotedSchema { get; }
        public string UnquotedSchema => SqlNameHelper.Unquote(QuotedSchema);

        public string QuotedCatalog { get; }
        public string UnquotedCatalog => SqlNameHelper.Unquote(QuotedCatalog);

        public override string ToString()
        {
            return QuotedCatalog != null ? $"{QuotedCatalog}.{QuotedSchema}.{QuotedName}" : $"{QuotedSchema}.{QuotedName}";
        }

        public static bool TryParse(string address, int pluginVersion, out SqlTable sqlTable)
        {
            if (pluginVersion == 1)
            {
                sqlTable = new SqlTable(address.Split('@')[0], "dbo", null);
                return true;
            }

            if (pluginVersion == 2)
            {
                var parts = address.Split('@').ToArray();

                if (!parts[0].StartsWith("[") || !parts[0].EndsWith("]"))
                {
                    parts[0] = $"[{parts[0]}]";
                }

                sqlTable = new SqlTable(parts[0], parts[1], parts[2]);
                return true;
            }

            sqlTable = null;
            return false;
        }

        protected bool Equals(SqlTable other)
        {
            return String.Equals(QuotedName, other.QuotedName) && String.Equals(QuotedSchema, other.QuotedSchema) && String.Equals(QuotedCatalog, other.QuotedCatalog);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((SqlTable) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (QuotedName != null ? QuotedName.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (QuotedSchema != null ? QuotedSchema.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (QuotedCatalog != null ? QuotedCatalog.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}