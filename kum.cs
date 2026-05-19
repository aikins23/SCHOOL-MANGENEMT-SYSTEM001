using System;
using System.Data;
using System.Data.OleDb;
using System.Windows.Forms;

namespace kingdom_Preparatory_School_Management_System
{
    /// <summary>
    /// Legacy data access class - maintained for backward compatibility
    /// Consider using StudentRepository and StudentService for new code
    /// </summary>
    [Obsolete("Use Data.StudentRepository and Services.StudentService instead")]
    public class kum
    {
        public DataSet ds = new DataSet();
        public OleDbCommand cmd = new OleDbCommand();
        public OleDbConnection con = new OleDbConnection();
        public OleDbConnection cons = new OleDbConnection();
        public OleDbDataAdapter adp = new OleDbDataAdapter();
        public OleDbDataReader rd = null;
        public string query, query1, query2 = string.Empty;
        public string constr = Properties.Settings.Default.ConnectionString;

        public kum()
        {
            con.ConnectionString = constr;
            cons.ConnectionString = constr;
        }
    }
}
