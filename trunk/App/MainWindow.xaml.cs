using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.Threading;
using Microsoft.Win32;
using System.IO;
using System.Collections.ObjectModel;

using System.Security.Principal;

using System.ComponentModel;

using System.Data;
using System.Data.SqlClient;

namespace YASBE
{
  public partial class MainWindow : Window
  {
    
    public MainWindow()
    {
      using (Proc BackupProfiles_s = new Proc("BackupProfiles_s"))
      {
        DataSet ds = BackupProfiles_s.ExecuteDataSet();
        BackupProfiles = ds.Tables[0];
        _BlankBackupProfileTable = ds.Tables[1];
      }
      
      InitializeComponent();
      BackupFile.List.CollectionChanged += (s, a) => { if (a.NewItems != null) datagrid.ScrollIntoView(a.NewItems[0]); };
      datagrid.AutoGeneratingColumn += new EventHandler<DataGridAutoGeneratingColumnEventArgs>(datagrid_AutoGeneratingColumn);
    }

    public DataTable BackupProfiles { get; private set; }

    static private DataTable _BlankBackupProfileTable = null;

    static public DataTable GetBlankBackupProfileTable()
    {
      return (_BlankBackupProfileTable.Clone());
    }

    private bool _beenhere = false;
    private Style _RightAlignStyle = null;
    void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
      if (_beenhere) return; _beenhere = true;

      _RightAlignStyle = datagrid.FindResource("RightAlignStyle") as Style;


      /*
      bool isAdmin = new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator) ? true : false;

      if (isAdmin)
      {
        MessageBox.Show("you are an administrator");
      }
      else
      {
        MessageBox.Show("You are not an administrator");
      }
       * */

    }

    static public string[] WindowsBurnStagingFolders
    {
      get
      {
        using (RegistryKey cdburning = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\CD Burning\StagingInfo"))
        {
          try
          {
            return(from volumekey in cdburning.GetSubKeyNames() select cdburning.OpenSubKey(volumekey).GetValue("StagingPath").ToString()).Distinct().ToArray<string>(); //ToArray is necessary to immediately execute rather than returning a delayed execution so that we can immediate close and dispose of the RegistryKey 
          }
          finally { cdburning.Close(); }
        }
      }
    }

    void datagrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
    {
      DataGridTextColumn c = (e.Column as DataGridTextColumn);
      if (e.PropertyName == "Size")
      {
        c.Binding.StringFormat = "{0:#,#.00}";
        c.Header = "Size (MegaBytes)";
      }

      if (e.PropertyType.IsNumeric()) c.ElementStyle = _RightAlignStyle;

    }

    private void Abort_Click(object sender, RoutedEventArgs e)
    {
      if (_thread != null) _thread.Abort();
    }


    private Thread _thread = null;
    private void DoIt_Click(object sender, RoutedEventArgs e)
    {
      BackupFile.List.Clear();

      string folder = @"D:\Photos\_Main_Library\1 - Friends\";
      System.IO.DirectoryInfo di = new DirectoryInfo(folder);
      FileInfo[] fileinfos = di.GetFiles("*.*", SearchOption.AllDirectories);

      lblStatus.Content = "Running"; 

      _thread = new Thread(delegate()
      {
        try
        {
          foreach (FileInfo f in fileinfos)
          {

            DateTime begin = DateTime.Now;
            string CRC = Helpers.GetMD5HashFromFile(f.FullName);
            int crctime = (DateTime.Now - begin).Milliseconds;

            //then do the UI oriented updates back on the proper UI thread via our friend Mr. Dispatch
            Dispatcher.Invoke((Action)delegate()
            {
              BackupFile.List.Add(new BackupFile() { FolderPath = f.DirectoryName, FileName = f.Name, Size = f.Length / (1024.0 * 1024.0), CRC = CRC, CRCGenTime = crctime });
            }, null);
          }

        }
        catch (Exception ex) //this exception handler must be included as part of this pattern wherever else it's implemented
        {
          if (!(ex is ThreadAbortException)) MessageBox.Show(ex.Message);
        }
        finally
        {
          Dispatcher.Invoke((Action)delegate() { lblStatus.Content = "Stopped"; }, null); 
        }
      });

      _thread.Start();

    }

    private void OpenStagingFolder_Click(object sender, RoutedEventArgs e)
    {
      System.Diagnostics.Process.Start(cbxDrives.SelectedItem.ToString());
    }

    private void BackupProfileSave_Click(object sender, RoutedEventArgs e)
    {
      DataTable Folders = MainWindow.GetBlankBackupProfileTable();
      FolderNode.GetSelectedFolders(Folders, "FullPath", FileSystemNode.RootDirectories);

      //DataRowView v = cbxBackupProfiles.SelectedItem as DataRowView;
      using (Proc BackupProfile_u = new Proc("BackupProfile_u"))
      {
        BackupProfile_u["@BackupProfileID"] = (int)cbxBackupProfiles.SelectedValue;
        BackupProfile_u["@Folders"] = Folders;
        BackupProfile_u.ExecuteNonQuery();
      }

    }
  }

  public class FileSystemNode : DependencyObject
  {
    public string FullPath { get; protected set; }
    public string Name { get; protected set; }

    public bool IsSelected
    {
      get { return (bool)GetValue(IsSelectedProperty); }
      set { SetValue(IsSelectedProperty, value); }
    }

    public static readonly DependencyProperty IsSelectedProperty =
      DependencyProperty.Register("IsSelected", typeof(bool), typeof(FileSystemNode), new UIPropertyMetadata(false));
        //  new PropertyMetadata(propertyChangedCallback: (obj, args) =>
        //{ (obj as ucToggleButton).btnToggle.Style = args.NewValue as Style; })); 

    protected FileSystemNode() { }
    public FileSystemNode(FileSystemInfo fsi)
    {
      FullPath = fsi.FullName;
      Name = fsi.Name;
    }

    static public FolderNode[] RootDirectories = (from drive in DriveInfo.GetDrives() where drive.IsReady select new FolderNode(drive.RootDirectory) { Name = drive.VolumeLabel + " (" + drive.Name + ")" }).ToArray();

  }

  public class FileNode : FileSystemNode
  {
    public FileNode(FileInfo file) : base(file) { }
  }

  public class FolderNode : FileSystemNode
  {
    public bool IsFunky { get; private set; }

    public FolderNode(DirectoryInfo folder) : base(folder)
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
        _Children = (from subdir in GetSubdirs(dir) select new FolderNode(subdir)).ToArray();
        _Children = _Children.Union((from file in GetFiles(dir) select new FileNode(file)).ToArray()).ToArray();

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

}
