using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text.RegularExpressions;
using System.Reflection;
//make sure to keep this clean of any particular UI assembly dependencies so that it can be
//reused across ASP.Net, Windows.Forms and WPF projects

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
/// <summary>
/// Basically a wrapper around SqlCommand.  Main benefit is .Parameters is pre-populated and conveiently exposed as Proc[string] indexer.
/// </summary>
public class Proc : IDisposable
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
{
  #region Wait Cursor stuff
  public delegate IDisposable WaitObjectConstructor();
  static public WaitObjectConstructor NewWaitObject = DummyWaitObjectConstructor;
  static private IDisposable DummyWaitObjectConstructor()
  {
    return (new DummyDisposable());
  }
  private class DummyDisposable : IDisposable { public void Dispose() { } };
  #endregion

  static public string ConnectionString = null;
  public bool TrimAndNull = true;
  private SqlCommand _cmd = null;
  private DataSet _ds = null;

  public DataSet dataSet { get { return(_ds); } set { _ds = value; } }

  public Proc(string ProcName) : this(ProcName, null) { }

  /// <summary>
  /// 
  /// </summary>
  /// <param name="ProcName"></param>
  /// <param name="UserName">pass this in to set "Workstation ID" which can be obtained via T-SQL's HOST_NAME() function... as a handy layup for a simple table audit framework :)</param>
  // I'm not entirely convinced this is the most elegant way to support this versus something more "automatic" in the background
  // the main challenge is maintaining a generically reusable Proc class that doesn't know whether it's running under ASP.Net or WPF
  // so rather than implementing a bunch of dynamic "drilling" to identify where you are and who the current username is
  // i'm thinking this is a nice "good enough" for now to simply pass it in from the outercontext
  public Proc(string ProcName, string UserName)
  {
    if (System.ComponentModel.LicenseManager.UsageMode == System.ComponentModel.LicenseUsageMode.Designtime) return; //this doesn't seem to work??
    if (System.Diagnostics.Process.GetCurrentProcess().ProcessName == "devenv") return;

    Assert.Check(ConnectionString != null, "'Proc.ConnectionString' must be assigned prior to creating Proc instances, System.ComponentModel.LicenseManager.UsageMode: " + System.ComponentModel.LicenseManager.UsageMode.ToString() + ", System.Diagnostics.Process.GetCurrentProcess().ProcessName: " + System.Diagnostics.Process.GetCurrentProcess().ProcessName);

    string logicalConnectionString = ConnectionString + ((UserName != null) ? ";Workstation ID=" + UserName : "");

    if (ProcName.Left(4).ToLower() != "dbo.") ProcName = "dbo." + ProcName;

    //pull cached parms if available
    string parmcachekey = logicalConnectionString + "~" + ProcName;
    bool HasCachedParms = _parmcache.TryGetValue(parmcachekey, out _cmd);
    if (HasCachedParms) _cmd = _cmd.Clone();
    else
    {
      _cmd = new SqlCommand(ProcName);
      _cmd.CommandType = CommandType.StoredProcedure;
    }
    _cmd.Connection = new SqlConnection(logicalConnectionString);
    if (_cmd.Connection.State != ConnectionState.Open) _cmd.Connection.Open();

    //i love this little gem, 
    //this allows us to skip the typical boiler plate parameter datatype definition code blocks and simply assign parm names to values in the client context
    //approach should allow for a parm cache refresh and re-fire, when the server pops an exception on parm datatype mismatch
    if (!HasCachedParms)
    {
      SqlCommandBuilder.DeriveParameters(_cmd); //nugget: automatically assigns all the available parms to this SqlCommand object by querying SQL Server's proc definition metadata

      //strip the dbname off any UDT's... there appears to be a mismatch between the part of microsoft that wrote DeriveParameters and what SQL Server actually wants
      //otherwise you get this friendly error message:
      //The incoming tabular data stream (TDS) remote procedure call (RPC) protocol stream is incorrect. Table-valued parameter 1 ("@MyTable"), row 0, column 0: Data type 0xF3 (user-defined table type) has a non-zero length database name specified.  Database name is not allowed with a table-valued parameter, only schema name and type name are valid.
      foreach (SqlParameter p in _cmd.Parameters) if (p.TypeName != "")
      {
        Match m = UDTParamTypeNameFix.Match(p.TypeName);
        if (m.Success) p.TypeName = m.Groups[2] + "." + m.Groups[3];
      }

      _parmcache.Add(parmcachekey, _cmd.Clone()); //nugget: cache SqlCommand objects to avoid unnecessary SqlCommandBuilder.DeriveParameters() calls
    }
  }
  static private Regex UDTParamTypeNameFix = new Regex(@"(.*?)\.(.*?)\.(.*)", RegexOptions.Compiled);
  static private Dictionary<string, SqlCommand> _parmcache = new Dictionary<string, SqlCommand>(StringComparer.OrdinalIgnoreCase); 

  public SqlParameterCollection Parameters { get { return (_cmd.Parameters); } }

  public void AssignValues(IOrderedDictionary values)
  {
    foreach (string key in values.Keys)
    {
      if (_cmd.Parameters.Contains("@"+key))
        this["@"+key] = values[key];
    }
  }

  public void AssignValues(DataRowView values)
  {
    foreach (DataColumn col in values.Row.Table.Columns)
    {
      if (_cmd.Parameters.Contains("@" + col.ColumnName))
        this["@" + col.ColumnName] = values[col.ColumnName];
      else if (_cmd.Parameters.Contains("@" + col.ColumnName.Replace(" ", ""))) //check for column name match with spaces removed
        this["@" + col.ColumnName.Replace(" ", "")] = values[col.ColumnName];
    }
  }

  public void AssignValues(object[] values)
  {
    for(int i = 0; i < values.Length; i += 2)
    {
      if (_cmd.Parameters.Contains("@" + values[i]))
        this["@" + values[i]] = values[i+1];
    }
  }

  public void Dispose()
  {
    if (_cmd != null)
    {
      _cmd.Connection.Dispose();
      _cmd.Connection = null;
      _cmd.Dispose();
      _cmd = null;
    }

    if (_ds !=null) _ds.Dispose();
    _ds = null;
  }

  public DataTableCollection Tables
  {
    get { 
      Assert.Check(_ds != null, "must execute proc prior to retrieving tables");
      return (_ds.Tables);
    }
  }

  public DataTable Table0
  {
    get
    {
      if (_ds == null) ExecuteDataTable();
      //Assert.Check(_ds != null, "must execute proc prior to retrieving tables");
      return (_ds.Tables[0]);
    }
  }

  public DataRow Row0
  {
    get
    {
      if (_ds == null) ExecuteDataTable();
      //Assert.Check(_ds != null, "must execute proc prior to retrieving tables");
      return (_ds.Tables[0].Rows[0]);
    }
  }


  public DataSet ExecuteDataSet() {
    if (System.ComponentModel.LicenseManager.UsageMode == System.ComponentModel.LicenseUsageMode.Designtime) return(null);

    using (IDisposable obj = NewWaitObject())
    using (SqlDataAdapter da = new SqlDataAdapter(_cmd))
    {
      if (_ds == null) _ds = new DataSet();
      if (_cmd.Connection.State != ConnectionState.Open) _cmd.Connection.Open();
      da.MissingSchemaAction = MissingSchemaAction.AddWithKey; //this magical line tells ADO.Net to go to the trouble of bringing back the schema info like DataColumn.MaxLength (which would otherwise always be -1!!)
      //da.FillSchema(_ds, SchemaType.Source);
      da.Fill(_ds);
      foreach (DataTable table in _ds.Tables) foreach (DataColumn column in table.Columns) column.ReadOnly = false;
      return (_ds);
    }
  }

  public DataSet ExecuteDataSet(object label /*actually any object with a .Text propertyName will suffice :)*/)
  {
    ReflectionHelpers.PropertySetter(label, "Success", true);

    try
    {
      return(ExecuteDataSet());
    }
    catch(Exception ex) {
      ReflectionHelpers.PropertySetter(label, "Success", false);

      //if the caller has provided a way to display the error then do so
      if (ReflectionHelpers.PropertySetter(label, "Text", SqlClientHelpers.SqlErrorTextCleaner(ex.Message)))
        return (null);
      //otherwise rethrow so that we can see the bug that caused the exception and fix it
      else 
        throw (ex);
    }
  }

  public DataTable ExecuteDataTable()
  {
    return (ExecuteDataTable(null));
  }

  public NameValueCollection ExecuteNameValueCollection()
  {
    using (DataTable t = ExecuteDataTable())
    {
      return (DataTableToNameValueCollection(t));
    }
  }

  static public NameValueCollection DataTableToNameValueCollection(DataTable t)
  {
    if (t.Rows.Count == 0) return (null);

    NameValueCollection vals = new NameValueCollection(t.Columns.Count - 1);
    foreach (DataColumn col in t.Columns)
    {
      vals[col.ColumnName] = t.Rows[0][col.ColumnName].ToString();
    }
    return (vals);
  }

  public DataTable ExecuteDataTable(object label)
  {
    _ds = ExecuteDataSet(label);
    if (_ds != null) return (_ds.Tables[0]);
    else return (null);
  }

  public Proc ExecuteNonQuery()
  {
    using (IDisposable obj = NewWaitObject())
    {
      _cmd.ExecuteNonQuery();
      return (this);
    }
  }

  public bool ExecuteNonQuery(object label, bool DisplaySuccess)
  {
    try
    {
      ExecuteNonQuery();
      if (DisplaySuccess) ReflectionHelpers.PropertySetter(label, "Text", "Saved Successfully");
      return (true);
    }
    catch (Exception ex)
    {
      if (!ReflectionHelpers.PropertySetter(label, "Text", SqlClientHelpers.SqlErrorTextCleaner(ex.Message)))
        throw (ex);
      return (false);
    }
  }

  public delegate void ExecuteMessageCallback(string Message);

  public bool ExecuteNonQuery(ExecuteMessageCallback callback, String ExecuteMessagePrefix, bool DisplaySuccess)
  {
    try
    {
      ExecuteNonQuery();
      if (DisplaySuccess && callback != null) callback(ExecuteMessagePrefix + "Saved Successfully");
      return (true);
    }
    catch (Exception ex)
    {
      if (callback != null) callback(ExecuteMessagePrefix + SqlClientHelpers.SqlErrorTextCleaner(ex.Message));
      return (false);
    }
  }


  public object this[string key]
  {
    //nulls can get a little tricky to look at here...
    //if TrimAndNull is on, then it'll truncate a blank string and convert it to DBNull

    //this getter here returns SqlValue not "Value" ... which translates SqlString parameters containing DBNull into C# nulls... 
    //this may or may not come in handy... we'll have to see and tweak accordingly
    get
    {
      return(_cmd.Parameters[key].Value);
    }
    set
    {
      if (TrimAndNull && !(value is DataTable) && (value != null) && (value != DBNull.Value)) {
        value = value.ToString().Trim();
        if ((string)value == "") value = null;
      }
      _cmd.Parameters[key].Value = (value == null || value == DBNull.Value) ? DBNull.Value : 
        (_cmd.Parameters[key].SqlDbType == SqlDbType.UniqueIdentifier) ? new Guid(value.ToString()) : value;
    }
  }
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
public static class SqlClientHelpers
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
{
  /*
  public class Column
  {
    public string ColumnName { get; set; }
  }

  public static IEnumerable<Column> Columns(this DataRowView drv)
  {
    return (from c in drv.Row.Table.Columns.Cast<DataColumn>() select new Column() { ColumnName=c.ColumnName });
  }
  */

  public static void ClearRows(object v)
  {
    DataView dv = v as DataView;
    if (dv != null)
    {
      dv.ClearRows();
      dv.Dispose();
    }

    DataRowView drv = v as DataRowView;
    if (drv != null) drv.ClearRow();
  }

  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  //DataRowView extension methods - use this approach to create a wrapper around DataRowView
  //such that callers stay obvlivious to DataRowView specifics and therefore could be implemented by some other business field container w/o lots of rippling changes
  public static bool ColumnsContain(this DataRowView drv, string ColumnName)
  {
    return(drv.Row.Table.Columns.Contains(ColumnName));
  }

  public static void ClearDirtyFlags(this DataRowView drv)
  {
    drv.Row.AcceptChanges();
  }

  static public void ClearRows(this DataView v)
  {
    if (v == null) return;
    DataRowCollection rows = v.Table.Rows;
    while (v.Count > 0) rows.RemoveAt(0);
  }

  static public void ClearRow(this DataRowView v)
  {
    if (v == null) return;
    v.Row.Table.Rows.Remove(v.Row);
  }


  static public void AddRelation(this DataSet ds, string Name, DataColumn Parent, DataColumn Child)
  {
    if (!ds.Relations.Contains(Name))
    {
      ds.Relations.Add(Name, Parent, Child);
    }
  }

  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

  private static string[] _ErrorTranslations = new string[]
  {
    @"Cannot insert the value NULL into column '(\w+)'", "Please fill out the {0} field.",
    @"The DELETE statement conflicted with the REFERENCE constraint.*table \""(dbo\.)?(\w+)\""", "You must remove the associated {1} records before you can delete this.",
    @"Cannot insert duplicate key row in object '(dbo\.)?(\w+)' with unique index '(\w+)'", "{1} already exists (message: {2})."
  };

  public static string SqlErrorTextCleaner(string message)
  {
    for (int i = 0; i < _ErrorTranslations.Length-1; i += 2)
    {
      Regex regex = new Regex(_ErrorTranslations[i]);
      Match m = regex.Match(message);
      if (m.Success) return (String.Format(_ErrorTranslations[i+1], m.Groups[1], m.Groups[2], m.Groups[3], m.Groups[4], m.Groups[5]));
    }
    return (message);
  }

  public static void AddInitialEmptyRows(DataSet ds, StringCollection rootTables)
  {
    //if root table is totally empty, create an initial row so that the grids show up and are ready to add the first entry
    foreach (string r in rootTables)
    {
      DataTable t = ds.Tables[r];
      if (t.Rows.Count == 0) AddNewRowWithPK(t);
    }

    //now walk the relationships and create initial CHILD rows where necessary
    foreach (DataRelation rel in ds.Relations)
    {
      foreach (DataRow ParentRow in rel.ParentTable.Rows)
      {
        if (ParentRow.GetChildRows(rel).Length == 0)
        {
          DataRow ChildRow = AddNewRowWithPK(rel.ChildTable);
          //fill out the foreign-key
          ChildRow[rel.ChildKeyConstraint.Columns[0].ColumnName] = ParentRow[rel.ChildKeyConstraint.RelatedColumns[0].ColumnName];
        }
      }
    }

  }

  public static DataRow AddNewRowWithPK(DataTable t)
  {
    DataRow r = t.NewRow();
    r[t.PrimaryKey[0].ColumnName] = System.Guid.NewGuid();
    t.Rows.Add(r);
    return (r);
  }

  public static DataRow AddNewNestedRow(DataTable t)
  {
    //create new row, assign PK
    DataRow r = AddNewRowWithPK(t);

    //fill this new row's foreign keys to its parents
    foreach (DataRelation rel in t.ParentRelations)
    {
      string col = rel.ChildColumns[0].ColumnName;
      r[col] = t.Rows[0][col];
    }

    //create new empty child rows with their FK's pointing to this new row so any related sub grids display
    foreach (DataRelation rel in t.ChildRelations)
    {
      string col = rel.ParentColumns[0].ColumnName;
      DataRow childrow = AddNewRowWithPK(rel.ChildTable);
      childrow[col] = r[col];
    }

    return (r);
  }

}


