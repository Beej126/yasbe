using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Windows;
using System.IO;
using System.Data;

using FileSystemNodes = System.Collections.Generic.Dictionary<string, FileSystemNode>; //nugget: type aliasing, very handy, basic inheritance isn't as clean

public class FileSystemNode : DependencyObject
{
  private string _FancyName = null;
  public string Name { get { return ((_FancyName == null) ? SubPath : _FancyName + " (" + SubPath + ")"); } }
  public string FullPath { get { return (((Parent == null) ? "" : Parent.FullPath) + SubPath + ((this is FolderNode)?"\\":"")); } }
  public string SubPath { get; protected set; }
  public FileSystemNode Parent { get; protected set; }
  public bool IsAncestorSelected { get { return (Parent != null && (Parent.IsSelected || Parent.IsAncestorSelected)); } } //nugget: i like simplicity of this line, it either immediately stops on the current parent or walks up the tree
  public bool IsMissing { get; protected set; }

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
    new UIPropertyMetadata(false, propertyChangedCallback: (o, a) => {
      //ripple the selection both up the tree...
      RefreshIsSubSelected(((FileSystemNode)o).Parent); 
      //and down the tree...
      ((FileSystemNode)o).OnIsSelectedChanged();
    }));

  // ****** careful what you put inside this method... it gets "back" fired for every decendent... that adds up fast!!!
  private void OnIsSelectedChanged()
  {
    IsExcluded = (IsSelected && IsAncestorSelected); //first set the current nodes IsExcluded status
    if (IsSelectedChanged != null) IsSelectedChanged(); //then go fire all the Children's and since the Children's ***Children are wired to this same method*** (via constructor) it will recurse... pretty cool
  }

  static private void RefreshIsSubSelected(FileSystemNode node)
  {
    FolderNode folder = node as FolderNode;
    if (node == null) return;
    node.IsSubSelected = folder.Children.Where(n => n.Value.IsSubSelected || n.Value.IsSelected).Count() > 0; //gotsta admit, the linq extensions sure do come in amazingly handy
    RefreshIsSubSelected(folder.Parent);
  }

  public bool IsSubSelected
  {
    get { return (bool)GetValue(IsSubSelectedProperty); }
    set { SetValue(IsSubSelectedProperty, value); }
  }

  // Using a DependencyProperty as the backing store for IsSubSelected.  This enables animation, styling, binding, etc...
  public static readonly DependencyProperty IsSubSelectedProperty =
      DependencyProperty.Register("IsSubSelected", typeof(bool), typeof(FileSystemNode), new UIPropertyMetadata(false));

  static public void LoadSelectedNodes(DataTable t)
  {
    foreach(DataRow r in t.Rows)
    {
      string[] FullPath = r["FullPath"].ToString().Split('\\').ToArray(); 
      int depthcounter = 1;
      LoadNode(null, FullPath, ref depthcounter);
    }
  }

  static private void LoadNode(FolderNode parentnode, string[] FullPath, ref int depthcounter)
  {
    bool IsFolder = (FullPath.Last() == ""); //folders come with a trailing slash which creates an empty last element as a basic way to differentiate them from files
    FileSystemNodes currentnodelist = (parentnode == null) ? RootDirectories : IsFolder ? parentnode.Children : null /* <= this should never exist, i.e. trying to load a file without having an existing parent's child list to add it to, because the parents get created as we go down the tree */;
    string path = String.Join("\\", FullPath.Take(depthcounter) /*1 based index*/) + (IsFolder ? "\\" : "");

    //if the current node doesn't show up in our tree...
    FileSystemNode node = null;
    if (!currentnodelist.TryGetValue(path, out node))
    {
      //then create it as a missing node (i.e. deleted since we last loaded this backup profile)
      string name = FullPath[depthcounter-1]; //zero based index
      string fullpath = String.Join("\\", FullPath);
      if (IsFolder) node = new FolderNode(parentnode, name); 
      else node = new FileNode(parentnode, name);

      //TODO: the one visual bummer about this is that missing folders are added way at the bottom of the children, under the files
      //  maybe that's sort of a good thing so they stand out even more... but i would've preferred them to be at the very top
      //  since the Children are demand loaded with the filesystem before we can add to the list it's tough to design around w/o making things undesirably messy (e.g. "IsChildrenLoaded" flag, etc)
      if (parentnode != null) parentnode.Children.Add(node.FullPath, node); 
    }

    //when we get to the bottom of the path, we've found our prey!
    if (depthcounter++ == (IsFolder ? FullPath.Length - 1 : FullPath.Length)) //again, as stated above, folders come with an extra empty last element
    {
      node.IsSelected = true;
      return; 
    }

    //otherwise, keep walking the tree
    LoadNode(node as FolderNode, FullPath, ref depthcounter);
  }

  public FileSystemNode(FileSystemNode Parent, FileSystemInfo fsi) : this(Parent, fsi.Name)
  {
    IsMissing = false;
  }

  public FileSystemNode(FileSystemNode Parent, string Name)
  {
    this.Parent = Parent;
    this.SubPath = Name;
    if (Parent != null) Parent.IsSelectedChanged += new Action(OnIsSelectedChanged); //all children subscribe to their parent's IsSelectedChanged to support keeping IsExcluded in sync
    //FlatList.Add(this);
    IsMissing = true; //this constructor fires first when we chain them so the FileSystemInfo based constructor gets final say on this flag
  }

  //static public List<FileSystemNode> FlatList = new List<FileSystemNode>(); //nugget: interestingly complex case... since another Static initializer (RootDirectories) fires code that uses this Static variable, you have to put this one first in the source, or else it won't be initialized yet when called upon

  static public FileSystemNodes RootDirectories = 
    (from drive in DriveInfo.GetDrives() where drive.IsReady
     select new FolderNode(null, drive.RootDirectory) {
       SubPath = drive.Name.TrimEnd('\\'),
       _FancyName = drive.VolumeLabel
     } as FileSystemNode).ToDictionary((n) => n.FullPath, StringComparer.InvariantCultureIgnoreCase);


  static public DataTable GetSelected(DataTable t)
  {
    t.BeginLoadData(); //basically this disables constraints and that's what we want in this particular case
    //(from node in FlatList where node.IsSelected select NewDataRow(t, node)).Last();  //this basically just saves us from looping twice, once to filter and once to convert to DataTable
    //nugget: all the approaches out there in Google land are no more glamorous... e.g.: http://msdn.microsoft.com/en-us/library/bb669096.aspx
    //t.EndLoadData(); //not even going to re-enable the constraints at the end

    WalkDownSelected(RootDirectories, t);

    return t; //just makes the calling syntax a little more compact
  }

  //leverage IsSubSelected to do an efficient walk down tree
  static private void WalkDownSelected(FileSystemNodes nodelist, DataTable t)
  {
    foreach (FileSystemNode n in nodelist.Values)
    {
      if (n.IsSelected) NewDataRow(t, n);
      if (n.IsSubSelected) WalkDownSelected(((FolderNode)n).Children, t);
    }
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
  public FileNode(FileSystemNode Parent, string Name) : base(Parent, Name) { }
}

public class FolderNode : FileSystemNode
{
  public bool IsFunky { get; private set; }

  public FolderNode(FileSystemNode Parent, DirectoryInfo folder) : base(Parent, folder)
  {
    IsFunky = folder.Attributes.HasFlag(FileAttributes.ReparsePoint);
  }

  //for recreating missing nodes... i.e. folders that have been deleted since we last saved the backup profile
  public FolderNode(FileSystemNode Parent, string Name) : base(Parent, Name) {}

  protected FileSystemNodes _Children = null;
  public FileSystemNodes Children
  {
    get
    {
      if (_Children != null || IsFunky) return (_Children);
      DirectoryInfo dir = new DirectoryInfo(FullPath+"\\"); //otherwise a "D:" by itself would load the current working directory of the application running on D: (or wherever)... amazingly annoying non-bug

      _Children = (from subdir in GetSubdirs(dir) select new FolderNode(this, subdir)).Union<FileSystemNode>( //nugget: this is pretty cool
                    (from file in GetFiles(dir) select new FileNode(this, file))
                  ).ToDictionary((n) => n.FullPath, StringComparer.InvariantCultureIgnoreCase);

      return (_Children);
    }
  }

  private DirectoryInfo[] GetSubdirs(DirectoryInfo dir)
  {
    try
    {
      return (dir.GetDirectories());
    }
    catch (DirectoryNotFoundException)
    {
      return (new DirectoryInfo[0]);
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
    catch (DirectoryNotFoundException)
    {
      return (new FileInfo[0]);
    }
    catch (UnauthorizedAccessException)
    {
      IsFunky = true;
      return (new FileInfo[0]);
    }
  }

}

