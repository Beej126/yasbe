using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Windows;
using System.IO;
using System.Data;

public class FileSystemNode : DependencyObject
{
  public string FullPath { get; protected set; }
  public string Name { get; protected set; }
  public FileSystemNode Parent { get; protected set; }
  public bool IsAncestorSelected { get { return (Parent != null && (Parent.IsSelected || Parent.IsAncestorSelected)); } } //nugget: i like simplicity of this line, it either immediately stops on the current parent or walks up the tree

  public event Action IsSelectedChanged;

  public bool IsExcluded
  {
    get { return (bool)GetValue(IsExcludedProperty); }
    set { SetValue(IsExcludedProperty, value); }
  }

  public static readonly DependencyProperty IsExcludedProperty =
      DependencyProperty.Register("IsExcluded", typeof(bool), typeof(FileSystemNode), new UIPropertyMetadata(false));

  public bool IsSelected
  {
    get { return (bool)GetValue(IsSelectedProperty); }
    set { SetValue(IsSelectedProperty, value); }
  }
  public static readonly DependencyProperty IsSelectedProperty =
    DependencyProperty.Register("IsSelected", typeof(bool), typeof(FileSystemNode),
    //when IsSelected changes, we have to refresh IsExcluded down the tree because IsExcluded = IsExcluded = (IsSelected && IsAncestorSelected);
    //this is implemented by the children subscribing to the parent's change event and the parent firing that event recursively
    new UIPropertyMetadata(false, propertyChangedCallback: (o, a) => ((FileSystemNode)o).OnIsSelectedChanged()));

  private void OnIsSelectedChanged()
  {
    IsExcluded = (IsSelected && IsAncestorSelected); //first set the current nodes IsExcluded status
    if (IsSelectedChanged != null) IsSelectedChanged(); //then go fire all the Children's and since the Children's Children are wired to this same event it will recurse... pretty cool
  }

  static private void LoadSelected(DataTable t)
  {
    foreach(DataRow r in t.Rows)
    {
      string[] FullPath = r["FullPath"].ToString().Split('\\');
      foreach(string Path in FullPath)
      {

      }
    }
  }


  public FileSystemNode(FileSystemNode Parent, FileSystemInfo fsi) 
  {
    this.Parent = Parent;
    if (Parent != null) Parent.IsSelectedChanged += new Action(OnIsSelectedChanged); //all children subscribe to their parent's IsSelectedChanged to support keeping IsExcluded in sync
    FullPath = fsi.FullName;
    Name = fsi.Name;
    FlatList.Add(this);
  }

  static public FolderNode[] RootDirectories = (from drive in DriveInfo.GetDrives() where drive.IsReady select new FolderNode(null, drive.RootDirectory) { Name = drive.VolumeLabel + " (" + drive.Name + ")" }).ToArray();

  static private List<FileSystemNode> _FlatList = null; //nugget: amazingly, as both a static initializer and a class constructor this was not instantiated before the instance constructors that required it!?!? so had to implement as a lazy getter, wild, really rocks what i thought i understood
  static public List<FileSystemNode> FlatList { get { if (_FlatList == null) _FlatList = new List<FileSystemNode>(); return (_FlatList); } }

  static public DataTable GetSelected(DataTable t)
  {
    var dummy = (from node in FlatList where node.IsSelected select NewDataRow(t, node)).Last();  //this basically just saves us from looping twice, once to filter and once to convert to DataTable
    //nugget: all the approaches out there in Google land are no more glamorous... e.g.: http://msdn.microsoft.com/en-us/library/bb669096.aspx

    return t; //just makes the calling syntax a little more compact
  }

  static private int NewDataRow(DataTable t, FileSystemNode f)
  {
    DataRow r = t.NewRow();
    //just hard code the mapping and don't waste cycles making it generic... we can always reuse this approach when mappings are so simple like this
    r["IsExcluded"] = f.IsExcluded;
    r["FullPath"] = f.FullPath;
    t.Rows.Add(r);
    return (0); //just to make select happy... we're just using linq as a glorified foreach at this point... probably not any faster or better
  }

}



public class FileNode : FileSystemNode
{
  public FileNode(FileSystemNode Parent, FileInfo file) : base(Parent, file) { }
}

public class FolderNode : FileSystemNode
{
  public bool IsFunky { get; private set; }

  public FolderNode(FileSystemNode Parent, DirectoryInfo folder) : base(Parent, folder)
  {
    IsFunky = folder.Attributes.HasFlag(FileAttributes.ReparsePoint);
  }

  static public void GetSelectedFolders(DataTable t, string FieldName, FileSystemNode[] current)
  {
    if (current == null) return;

    foreach (FolderNode f in current.OfType<FolderNode>().ToArray())
    {
      if (f.IsSelected)
      {
        DataRow r = t.NewRow();
        r[FieldName] = f.FullPath;
        t.Rows.Add(r);
      }
      GetSelectedFolders(t, FieldName, f._Children);
    }
  }


  protected FileSystemNode[] _Children = null;
  public FileSystemNode[] Children
  {
    get
    {
      if (_Children != null || IsFunky) return (_Children);
      DirectoryInfo dir = new DirectoryInfo(FullPath);
      _Children = (from subdir in GetSubdirs(dir) select new FolderNode(this, subdir)).Union<FileSystemNode>( //nugget: this is pretty cool
                    (from file in GetFiles(dir) select new FileNode(this, file))
                  ).ToArray();

      return (_Children);
    }
  }

  private DirectoryInfo[] GetSubdirs(DirectoryInfo dir)
  {
    try
    {
      return (dir.GetDirectories());
    }
    catch (UnauthorizedAccessException)
    {
      IsFunky = true;
      return (new DirectoryInfo[0]);
    }
  }


  private FileInfo[] GetFiles(DirectoryInfo dir)
  {
    try
    {
      return (dir.GetFiles());
    }
    catch (UnauthorizedAccessException)
    {
      IsFunky = true;
      return (new FileInfo[0]);
    }
  }

}

