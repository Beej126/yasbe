/*
<Button Content="2. All IncludedFiles" Click="AllFiles_Click" Visibility="Collapsed" />
<Button Content="3. Minus Excluded" Click="Exclude_Click" Visibility="Collapsed" />

    List<FolderNode> IncludedFolders = new List<FolderNode>();
    List<string> ExcludedFiles = new List<string>();
    private void GatherCandidates_Click(object sender, RoutedEventArgs e)
    {
      IncludedFolders.Clear();
      ExcludedFiles.Clear();
      DataTable SelectedFolders = FileSystemNode.GetSelected(MainWindow.GetBlankBackupProfileTable(), IncludedFolders, ExcludedFiles);

      gridFilesWorkingSet.ItemsSource = null;
      datagrid2.ItemsSource = null;
      gridFilesWorkingSet.ItemsSource = IncludedFolders;
      datagrid2.ItemsSource = (from string s in ExcludedFiles select new { FullPath = s }).ToArray(); //ExcludedFiles;
    }

    List<FileInfo> AllFiles = new List<FileInfo>();
    private void AllFiles_Click(object sender, RoutedEventArgs e)
    {
      AllFiles.Clear();

      foreach (FolderNode folder in IncludedFolders)
      {
        DirectoryInfo dir = new DirectoryInfo(folder.FullPath);
        FileInfo[] files = dir.GetFiles("*.*", folder.IsSubSelected ? SearchOption.TopDirectoryOnly : SearchOption.AllDirectories);
        AllFiles.AddRange(files);
      }

      gridFilesWorkingSet.ItemsSource = null;
      gridFilesWorkingSet.ItemsSource = AllFiles; // (from string s in AllFiles select new { fullpath = s }).ToArray();
    }

    private void Exclude_Click(object sender, RoutedEventArgs e)
    {
      Dictionary<string, FileInfo> finallist = new Dictionary<string, FileInfo>(StringComparer.OrdinalIgnoreCase);
      finallist = AllFiles.ToDictionary(fi => fi.FullName);
      foreach (string excludefile in ExcludedFiles) finallist.Remove(excludefile);

      datagrid2.ItemsSource = null;
      datagrid2.ItemsSource = (from v in finallist.Values select v).ToArray();
    }
 * 
 * 
 * 
private DataTable _CurrentGridFilesTable = null;
    public DataTable CurrentGridFilesTable
    {
      get
      {
        return(_CurrentGridFilesTable);
      }
      set
      {
        _CurrentGridFilesTable = value;

        if (dvCurrentMediaSubsetFiles != null) dvCurrentMediaSubsetFiles.Dispose();
        dvCurrentMediaSubsetFiles = new DataView(_CurrentGridFilesTable);
        dvCurrentMediaSubsetFiles.RowFilter = "MediaSubsetNumber = " + MediaSubsetNumberCurrent;
      }
    }
*/