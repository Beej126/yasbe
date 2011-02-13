using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Windows;
using System.IO;
using System.Data;

using System.Collections.Specialized;

using FileSystemNodes = System.Collections.Generic.Dictionary<string, FileSystemNode>; //nugget: type aliasing, very handy, basic inheritance isn'SelectedFolders as clean

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

  // ****** careful what you put inside this method... it gets "back" fired for every decendent (event assignment in FileSystemNode( constructor)... that adds up fast!!!
  private void OnIsSelectedChanged()
  {
    SetIsExcluded();
    if (IsSelectedChanged != null) IsSelectedChanged(); //then go fire all the Children's and since the Children's ***Children are wired to this same method*** (via constructor) it will recurse... pretty cool
  }

  private void SetIsExcluded()
  {
    IsExcluded = IsSelected && IsAncestorSelected && (!IsSubSelected /*this is redundant because filenodes can'SelectedFolders be subselected: || this is FileNode*/); //NUGGET: 2011_02_11 12:56AM NAILED IT!!!! FUCK YEAH... this took me 4EVER
  }

  static private void RefreshIsSubSelected(FileSystemNode node)
  {
    FolderNode folder = node as FolderNode;
    if (node == null) return;

    node.IsSubSelected = folder.Children.Where(n => n.Value.IsSubSelected || n.Value.IsSelected).Count() > 0; //gotsta admit, the linq extensions sure do come in amazingly handy

    node.SetIsExcluded(); //must do this after setting IsSubSelected since it depends on that property

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

    //if the current node doesn'SelectedFolders show up in our tree...
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

  //static public List<FileSystemNode> FlatList = new List<FileSystemNode>(); //nugget: interestingly complex case... since another Static initializer (RootDirectories) fires code that uses this Static variable, you have to put this one first in the source, or else it won'SelectedFolders be initialized yet when called upon

  static public FileSystemNodes RootDirectories = 
    (from drive in DriveInfo.GetDrives() where drive.IsReady
     select new FolderNode(null, drive.RootDirectory) {
       SubPath = drive.Name.TrimEnd('\\'),
       _FancyName = drive.VolumeLabel
     } as FileSystemNode).ToDictionary((n) => n.FullPath, StringComparer.OrdinalIgnoreCase);


  static public DataTable GetSelected(DataTable SelectedFolders, DataTable IncludedFiles = null)
  {
    SelectedFolders.BeginLoadData(); //basically this disables constraints and that's what we want in this particular case
    SelectedFolders.Clear();

    if (IncludedFiles != null)
    {
      IncludedFiles.BeginLoadData();
      IncludedFiles.Clear();
    }

    WalkDownSelected(RootDirectories, SelectedFolders, IncludedFiles);

    return (SelectedFolders); //just makes the calling context simpler if you're only going after gathering the selected folders and not the whole file set
  }

  static private void WalkDownSelected(FileSystemNodes nodelist, DataTable SelectedFolders, DataTable IncludedFiles)
  {
    foreach (FileSystemNode n in nodelist.Values)
    {
      if (IncludedFiles != null)
      {
        //good concepts to keep in mind when understanding the tree walk logic:
        //  folders are the only thing that form the hierarchy (i know, duh)
        //  but that means we're always only *traversing* down folder nodes, never file nodes
        //  yet at each level, we have a list of Children which are both folders and files... which can be either Included or Excluded

        //to gather a list of candidate files...
        //we just need to materialize two sets... and then combine that info to reach our final list:
        // 1) included directores - with either subfolder scanning or not
        // 2) excluded files - because an excluded folder is represented by it's absence in the included list

        //as we walk down each folder node... 
        // for an IncludedFolder, IsSubSelected will tell us whether to subscan or just process the single folder => DirectoryInfo.GetDirectories(folder, IsSubSelected?SearchOption.None:SearchOption.AllDirectories)
        // excluded files are those children which are files and IsExcluded right???

        if (n is FolderNode && (n.IsAncestorSelected || n.IsSelected) && !n.IsExcluded) ScanFolder(n as FolderNode, IncludedFiles);
        else if (n is FileNode && n.IsExcluded) IncludedFiles.Rows.Remove(IncludedFiles.Rows.Find((n as FileNode).FullPath));
      }

      if (n.IsSelected) NewSelectedFolderRow(SelectedFolders, n);

      //IsSubSelected beautifully tells us exactly which Children to further inspect and ignore the rest for an efficient walk down
      //and it "automatically" protects us to only traverse folders because files can have no children and therefore are never subselected
      //TODO: technically speaking IsSubSelected should be on FolderNode only... but then i'd have to cast code like this... so hmmmm... vs just having a flag there never gets set for IncludedFiles... which is actually handy
      if (n.IsSubSelected) WalkDownSelected(((FolderNode)n).Children, SelectedFolders, IncludedFiles);
    }
  }

  static private void ScanFolder(FolderNode folder, DataTable IncludedFiles)
  {
    DirectoryInfo dir = new DirectoryInfo(folder.FullPath);
    FileInfo[] files = dir.GetFiles("*.*", folder.IsSubSelected ? SearchOption.TopDirectoryOnly : SearchOption.AllDirectories);
    foreach (FileInfo file in files)
    {
      DataRow r = IncludedFiles.NewRow();
      r["FullPath"] = file.FullName;
      r["ModifiedDate"] = file.LastWriteTimeUtc;
      r["Size"] = file.Length; //megabytes
      IncludedFiles.Rows.Add(r);
    }
  }

  static private void NewSelectedFolderRow(DataTable t, FileSystemNode f)
  {
    DataRow r = t.NewRow();
    //just hard code the mapping and don'SelectedFolders waste cycles making it generic
    r["IsExcluded"] = f.IsExcluded;
    r["FullPath"] = f.FullPath;
    t.Rows.Add(r);
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
                  ).ToDictionary((n) => n.FullPath, StringComparer.OrdinalIgnoreCase);

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

