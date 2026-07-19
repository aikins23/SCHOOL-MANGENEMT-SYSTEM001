using System;
using System.Data;
using Microsoft.Data.SqlClient;

class Program
{
    static void Main()
    {
        var connStr = "Server=(localdb)\\mssqllocaldb;Database=Neat_Academy;Trusted_Connection=True;MultipleActiveResultSets=true";
        using var conn = new SqlConnection(connStr);
        conn.Open();
        var schema = conn.GetSchema("Columns", new string[] { null, null, "payment_record", null });
        foreach (DataRow row in schema.Rows)
        {
            Console.WriteLine($"{row["COLUMN_NAME"]} - {row["DATA_TYPE"]}");
        }
    }
}
