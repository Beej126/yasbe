/*
<Button Content="2. All IncludedFilesTable" Click="AllFiles_Click" Visibility="Collapsed" />
<Button Content="3. Minus Excluded" Click="Exclude_Click" Visibility="Collapsed" />

    List<FolderNode> IncludedFolders = new List<FolderNode>();
    List<string> ExcludedFiles = new List<string>();
    private void GatherCandidates_Click(object sender, RoutedEventArgs e)
    {
      IncludedFolders.Clear();
      ExcludedFiles.Clear();
      DataTable SelectedFoldersTable = FileSystemNode.GetSelected(MainWindow.GetBlankBackupProfileTable(), IncludedFolders, ExcludedFiles);

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


/*  the whole CRC stuff... GUI and code

         <StackPanel Orientation="Horizontal" Visibility="Collapsed" >
        
          <Button Click="DoIt_Click" >
            <StackPanel Orientation="Horizontal">
              <TextBlock Text="Do It" Style="{StaticResource BigButtonText}"  />
              <Image Source="Arrow - Large Right - Green.png" />
            </StackPanel>
          </Button>
        
          <Button Click="Abort_Click" >
            <StackPanel Orientation="Horizontal">
              <TextBlock Text="Abort" Style="{StaticResource BigButtonText}"   />
              <Image Source="Forbidden 02.png" />
            </StackPanel>
          </Button>

          <WrapPanel Orientation="Horizontal" Width="350">
            <WrapPanel.Resources>
              <Style TargetType="Button" BasedOn="{StaticResource {x:Type Button}}">
                <Setter Property="Margin" Value="3 3 0 0" />
              </Style>
            </WrapPanel.Resources>
            <Button Content="Stamp Current Batch as 'Archived'" />
          </WrapPanel>
          
          <Label FontSize="32" Content="Status:" />
          <Label FontSize="32" FontWeight="Bold" Content="Stopped" Name="lblStatus" />
        
        </StackPanel>


 *     private void Abort_Click(object sender, RoutedEventArgs e)
    {
      if (_thread != null) _thread.Abort();
    }


    private Thread _thread = null;
    private void DoIt_Click(object sender, RoutedEventArgs e)
    {
      BackupFile.List.Clear();
      gridFilesWorkingSet.ItemsSource = BackupFile.List; 

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


  
      BackupFile.List.CollectionChanged += (s, a) => { if (a.NewItems != null) gridFilesWorkingSet.ScrollIntoView(a.NewItems[0]); }; //autoscroll the grid attached to this list
 * 
 * 
 * 
 * 
HELPERS.CS
 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO;
using System.Security.Cryptography;
using System.Collections.ObjectModel;

using System.Windows.Controls;

using System.Windows.Markup;
using System.Windows.Data;
using System.Globalization;


namespace YASBE
{
  static class Helpers
  {

    static public string GetMD5HashFromFile(string fileName)
    {
      FileStream file = new FileStream(fileName, FileMode.Open);
      MD5 md5 = new MD5CryptoServiceProvider();
      byte[] retVal = md5.ComputeHash(file);
      file.Close();

      StringBuilder sb = new StringBuilder();
      for (int recordindex = 0; recordindex < retVal.Length; recordindex++)
      {
        sb.Append(retVal[recordindex].ToString("x2"));
      }
      return sb.ToString();
    }

  }

  public class BackupFile
  {
    public string FolderPath { get; set; }
    public string FileName { get; set; }
    public double Size { get; set; }
    public int CRCGenTime { get; set; }
    public string CRC { get; set; }

    public static readonly ObservableCollection<BackupFile> List = new ObservableCollection<BackupFile>();
  }


}

 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
    /// <summary>
    /// </summary>
    /// <param name="NewIncrementalID">Just pass null if you're not trying to change the ActiveIncrementalID</param>
    private void RefreshIncrementalInfo(bool FullDetail = false, int? NewIncrementalID = null)
    {
      DataView IncrementalView = ((DataView)gridIncrementalHistory.ItemsSource);
      DataTable IncrementalTable = (IncrementalView == null) ? null : IncrementalView.Table;

      if (NewIncrementalID != null && NewIncrementalID != ActiveIncrementalID)
        ActiveIncrementalRow = IncrementalTable.Rows.Find(NewIncrementalID);

      using (Proc Incremental_Proc = new Proc(FullDetail?"Incremental_FullDetail":"Incremental_Files"))
      {
        //nugget: "Failed to enable constraints" errors are brutal... here's some tips... look at the DataTable.GetErrors()..Row.RowError's to see what's barking
        //as usual, it's pretty obvious when you figure it out... recordindex was returning a resultset that didn't have a unique column selected to provide the primary key for the logical resultset
        if (cbxBackupProfiles.SelectedValue == null) return;

        if (FullDetail) Incremental_Proc["@BackupProfileID"] = cbxBackupProfiles.SelectedValue;
        Incremental_Proc["@IncrementalID"] = ActiveIncrementalID;
        Incremental_Proc.ExecuteDataTable();

        WorkingFilesTable = Incremental_Proc.Tables[0]; //whichever proc we fire, we get the files back in Table[0]

        if (FullDetail)
        {

          //if we're just firing up and this is our base list of incrementals then assign it directly to the grid
          if (IncrementalTable == null)
          {
            gridIncrementalHistory.ItemsSource = Incremental_Proc.Tables[1].DefaultView;
            if (Incremental_Proc.Tables[1].Rows.Count > 0)
              ActiveIncrementalRow = Incremental_Proc.Tables[1].Rows[0];
          }
          //otherwise merge this info into the grid as an update
          else
          {
            IncrementalTable.Merge(Incremental_Proc.Tables[1], false); //TODO: check that the primary keys come through and line this up to work properly
            RefreshActiveIncrementalRowDependencies();
          }
        }
      }
    }

 */