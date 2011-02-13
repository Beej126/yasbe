﻿using System;
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

using System.Data;
using System.Data.SqlClient;

using System.Collections.Specialized;

namespace YASBE
{
  public partial class MainWindow : Window
  {

   
    public MainWindow()
    {
      InitializeComponent();

      gridFilesWorkingSet.AutoGeneratingColumn += new EventHandler<DataGridAutoGeneratingColumnEventArgs>(WPFHelpers.DataGridRightAlignAutoGeneratedNumericColumns); //nugget: this the most generic way i could figure this so far...see helper comments
      gridIncrementalHistory.AutoGeneratingColumn += new EventHandler<DataGridAutoGeneratingColumnEventArgs>(WPFHelpers.DataGridRightAlignAutoGeneratedNumericColumns);

      LoadBackProfilesList();

      BackupFile.List.CollectionChanged += (s, a) => { if (a.NewItems != null) gridFilesWorkingSet.ScrollIntoView(a.NewItems[0]); }; //autoscroll the grid attached to this list
    }

    //void gridFilesWorkingSet_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
    //{
    //  DataGridTextColumn c = (e.Column as DataGridTextColumn);
    //  if (e.PropertyName == "Size")
    //  {
    //    c.Binding.StringFormat = "{0:#,#.000}";
    //    c.Header = "Size (MegaBytes)";
    //  }
    //}

    private bool _beenhere = false;
    void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
      if (_beenhere) return; _beenhere = true; //this sucks but is actually recommended by an MSDN MVP: http://social.msdn.microsoft.com/Forums/en/wpf/thread/39ce4ebd-75a6-46d5-b303-2e0f89c6eb8d

      //_RightAlignStyle = gridFilesWorkingSet.FindResource("RightAlignStyle") as Style;

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

    private void LoadBackProfilesList()
    {
      //load initial backup profile names drop down list so that a previously selected entry stored in Property.Settings has something to Bind to when the Window first comes up
      using (Proc BackupProfiles_s = new Proc("BackupProfiles_s"))
      {
        BackupProfiles_s["@BackupProfileID"] = YASBE.Properties.Settings.Default.SelectedBackupProfileID;

        BackupProfiles_s.ExecuteDataSet();
        cbxMediaSize.ItemsSource = BackupProfiles_s.dataSet.Tables[1].DefaultView; //i belive this order is what allowed the cbxBackupProfiles Selected row to properly drive the selected cbxMediaSize.MediaSizeID
        cbxBackupProfiles.ItemsSource = BackupProfiles_s.Tables[0].DefaultView; 
        
        SelectedFolders = BackupProfiles_s.Tables[2];

        IncludedFiles = BackupProfiles_s.Tables[3];
        IncludedFiles.PrimaryKey = new DataColumn[] { IncludedFiles.Columns["FullPath"] };

        MediaSubsetFilesCurrent = BackupProfiles_s.Tables[4];

        RefreshIncrementalInfo(true);
      }
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

    private void Abort_Click(object sender, RoutedEventArgs e)
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

    private void OpenStagingFolder_Click(object sender, RoutedEventArgs e)
    {
      System.Diagnostics.Process.Start(cbxBurnFolders.SelectedItem.ToString());
    }

    private void BackupProfileSave_Click(object sender, RoutedEventArgs e)
    {
      //DataRowView v = cbxBackupProfiles.SelectedItem as DataRowView;
      using (Proc BackupProfile_u = new Proc("BackupProfile_u"))
      {
        BackupProfile_u["@BackupProfileID"] = cbxBackupProfiles.SelectedValue;
        BackupProfile_u["@Name"] = ((DataRowView)cbxBackupProfiles.SelectedItem)["Name"];
        BackupProfile_u["@MediaSizeID"] = ((DataRowView)cbxBackupProfiles.SelectedItem)["MediaSizeID"];
        BackupProfile_u["@Folders"] = FileSystemNode.GetSelected(SelectedFolders);
        BackupProfile_u.ExecuteNonQuery();
      }

    }

    private void cbxBackupProfiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      using (Proc BackupProfileFolders_s = new Proc("BackupProfileFolders_s"))
      {
        BackupProfileFolders_s["@BackupProfileID"] = cbxBackupProfiles.SelectedValue;
        FileSystemNode.LoadSelectedNodes(BackupProfileFolders_s.Table0);
      }
    }

    public DataTable IncludedFiles = null;
    public DataTable SelectedFolders = null;
    private void GatherCandidates_Click(object sender, RoutedEventArgs e)
    {
      using (WaitCursorWrapper w = new WaitCursorWrapper())
      {
        FileSystemNode.GetSelected(SelectedFolders, IncludedFiles);
        gridFilesWorkingSet.ItemsSource = IncludedFiles.DefaultView;
      }
    }

    private void CopyToExclusions_Executed(object sender, ExecutedRoutedEventArgs e)
    {
      //txtExclusions.Text += "\r\n" + e.Parameter.ToString();
    }

    private void NewIncremental_Click(object sender, RoutedEventArgs e)
    {
      using (Proc Incremental_i = new Proc("Incremental_i"))
      {
        Incremental_i["@BackupProfileID"] = cbxBackupProfiles.SelectedValue;
        gridIncrementalHistory.ItemsSource = Incremental_i.ExecuteDataTable().DefaultView;
        RefreshIncrementalInfo(true, Convert.ToInt32(Incremental_i["@IncrementalID"])); //we need a full load of the Incremental header info plus the files
      }
    }

    private void MakeActiveIncremental_Click(object sender, RoutedEventArgs e)
    {
      ActiveIncrementalRow = ((DataRowView)gridIncrementalHistory.SelectedItem).Row;
      RefreshIncrementalInfo();//we just need a files list load here
    }

    public DataRow ActiveIncrementalRow
    {
      get { return (DataRow)GetValue(ActiveIncrementalRowProperty); }
      set { SetValue(ActiveIncrementalRowProperty, value); }
    }
    public static readonly DependencyProperty ActiveIncrementalRowProperty =
      DependencyProperty.Register("ActiveIncrementalRow", typeof(DataRow), typeof(MainWindow), new UIPropertyMetadata(null, RefreshActiveIncrementalRowDependencies));//ARRRG, have no idea why it won't take the normal lamda expression wrapper rather than the method
    static private void RefreshActiveIncrementalRowDependencies(DependencyObject o, DependencyPropertyChangedEventArgs args)
    {
      ((MainWindow)o).RefreshActiveIncrementalRowDependencies();
    }
    private void RefreshActiveIncrementalRowDependencies()
    {
      MediaSubsetNumberCurrent = Convert.ToInt16(ActiveIncrementalRow["MaxMediaSubsetNumber"]) + 1;
    }

    public int? ActiveIncrementalID
    {
      get
      {
        return ((ActiveIncrementalRow != null) ? Convert.ToInt16(ActiveIncrementalRow["IncrementalID"]) : (int?)null);
      }
    }

    public int MediaSubsetNumberCurrent
    {
      get { return (int)GetValue(MediaSubsetNumberCurrentProperty); }
      set { SetValue(MediaSubsetNumberCurrentProperty, value); }
    }
    public static readonly DependencyProperty MediaSubsetNumberCurrentProperty =
        DependencyProperty.Register("MediaSubsetNumberCurrent", typeof(int), typeof(MainWindow), new UIPropertyMetadata(0));

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
        //as usual, it's pretty obvious when you figure it out... i was returning a resultset that didn't have a unique column selected to provide the primary key for the logical resultset
        if (FullDetail) Incremental_Proc["@BackupProfileID"] = cbxBackupProfiles.SelectedValue;
        Incremental_Proc["@IncrementalID"] = ActiveIncrementalID;
        Incremental_Proc.ExecuteDataTable();

        gridFilesWorkingSet.ItemsSource = Incremental_Proc.Tables[0].DefaultView; //whichever proc we fire, we get the files back in Table[0]
        HideCompletedMediaSubsets(); 

        if (FullDetail)
        {

          //if we're just firing up and this is our base list of incrementals then assign it directly to the grid
          if (IncrementalTable == null)
          {
            gridIncrementalHistory.ItemsSource = Incremental_Proc.Tables[1].DefaultView;
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

    private void ShowSelectedFolders_Click(object sender, RoutedEventArgs e)
    {
      gridFilesWorkingSet.ItemsSource = SelectedFolders.DefaultView;
    }

    private void ShowIncludedFiles_Click(object sender, RoutedEventArgs e)
    {
      gridFilesWorkingSet.ItemsSource = IncludedFiles.DefaultView;    
    }

    private void ComputeIncremental_Click(object sender, RoutedEventArgs e)
    {
      if (ActiveIncrementalID == null)
      {
        MessageBox.Show("Press [New Incremental Backup] button -OR-\r\nRight mouse the Incremental Backup History grid\r\nto establish the container for these new files",
          "No Incremental Backup Container has been established", MessageBoxButton.OK, MessageBoxImage.Information);
        return;
      }

      try
      {
        using (Proc Files_UploadCompare = new Proc("Files_UploadCompare"))
        {
          Files_UploadCompare["@IncrementalID"] = ActiveIncrementalID;
          Files_UploadCompare["@Files"] = IncludedFiles;
          Files_UploadCompare.ExecuteNonQuery();
        }
        RefreshIncrementalInfo(true); //the disc count into will change so we need a header level refresh
      }
      catch (Exception ex)
      {
        if (ex.Message.Left(9) == "[CONFIRM]")
        {
          MessageBox.Show(ex.Message, "** Warning **", MessageBoxButton.OK, MessageBoxImage.Exclamation);
          chkOverrideExistingMediaSubsets.Visibility = Visibility.Visible;
        }
        else throw;
      }
    }

    public DataTable MediaSubsetFilesCurrent = null;

    private void MediaSubsetCommit_Click(object sender, RoutedEventArgs e)
    {
      MediaSubsetFilesCurrent.Clear();
      foreach (DataRowView drv in GetCurrentMediaSubsetFiles())
      {
        DataRow r = MediaSubsetFilesCurrent.NewRow();
        r["FileArchiveID"] = drv["FileArchiveID"];
        MediaSubsetFilesCurrent.Rows.Add(r);
      }

      using (Proc MediaSubset_Commit = new Proc("MediaSubset_Commit"))
      {
        MediaSubset_Commit["@IncrementalID"] = ActiveIncrementalID;
        MediaSubset_Commit["@MediaSubsetNumber"] = MediaSubsetNumberCurrent;
        MediaSubset_Commit["@Files"] = MediaSubsetFilesCurrent;
        MediaSubset_Commit.ExecuteNonQuery();
        MediaSubsetFilesCurrent.Clear();
        RefreshIncrementalInfo(true); 
      }
    }

    private void HideCompletedMediaSubsets_Click(object sender, RoutedEventArgs e)
    {
      HideCompletedMediaSubsets();
    }

    private DataView HideCompletedMediaSubsets()
    {
      DataView v = (DataView)gridFilesWorkingSet.ItemsSource;
      if (v == null || !v.Table.Columns.Contains("Finalized")) return (null);
      v.RowFilter = (chkHideCompleteMediaSubsets.IsChecked.Value ? "Finalized = 0" : "");
      return (v);
    }

    public long TallyBytes
    {
      get { return (long)GetValue(TallyBytesProperty); }
      set { SetValue(TallyBytesProperty, value); }
    }
    public static readonly DependencyProperty TallyBytesProperty =
        DependencyProperty.Register("TallyBytes", typeof(long), typeof(MainWindow), new UIPropertyMetadata((long)0));

    private void IdentifyNextMediaSubset_Click(object sender, RoutedEventArgs e)
    {
      using (WaitCursorWrapper w = new WaitCursorWrapper())
      {
        chkHideCompleteMediaSubsets.IsChecked = true;
        DataView files = HideCompletedMediaSubsets();
        files.Sort = "FullPath desc, Size desc";

        long maxbytes = /*for testing*/ 20 * 1024 * 1024; //Convert.ToInt64(ActiveIncrementalRow["MediaBytes"]); 
        int MediaSubSetNumber = MediaSubsetNumberCurrent;
        TallyBytes = 0;

        for (int i = 0; i < files.Count; i++)
        {
          //DataGridRow gridrow = WPFHelpers.GetDataGridRow(gridFilesWorkingSet, i);
          long nextsize = Convert.ToInt64(files[i]["Size"]);
          if (TallyBytes + nextsize > maxbytes) break;

          //gridrow.IsSelected = true;
          files[i]["MediaSubsetNumber"] = MediaSubSetNumber;
          TallyBytes += nextsize;
        }
      }
    }

    public DataTable CurrentGridFilesTable = null;

    private DataRowView[] GetCurrentMediaSubsetFiles()
    {
      using (DataView v = new DataView(((DataView)gridFilesWorkingSet.ItemsSource).Table))
      {
        v.Sort = "MediaSubsetNumber desc";
        return(v.FindRows(MediaSubsetNumberCurrent));
      }
    }

    private void SymLinkToBurn_Click(object sender, RoutedEventArgs e)
    {
      using (WaitCursorWrapper w = new WaitCursorWrapper())
      {
        foreach (DataRowView r in GetCurrentMediaSubsetFiles())
        {
          string fullpath  = r["FullPath"].ToString();
          Win32Helpers.CreateSymbolicLink(
            /*new symlink filename*/System.IO.Path.Combine(cbxBurnFolders.SelectedValue.ToString(), System.IO.Path.GetFileName(fullpath)), 
            /*source filename*/fullpath, Win32Helpers.SYMBOLIC_LINK_FLAG.File);
        }
      }
    }

    private void AddSingleToBurn_Click(object sender, RoutedEventArgs e)
    {
      DataRowView r = (DataRowView)gridFilesWorkingSet.SelectedItem;
      TallyBytes += Convert.ToInt64(r["Size"]);
      //Win32Helpers.CreateSymbolicLink(/*dest*/cbxBurnFolders.SelectedValue.ToString(), /*source*/r["FullPath"].ToString(), Win32Helpers.SYMBOLIC_LINK_FLAG.File);
    }

    private void RemoveSingleToBurn_Click(object sender, RoutedEventArgs e)
    {
      //ActiveIncrementalRow = ((DataRowView)gridIncrementalHistory.SelectedItem).Row;
    }

  }

  public class FileTreeBackgroundBrushConverter : WPFValueConverters.MarkupExtensionConverter, IMultiValueConverter
  {
    public FileTreeBackgroundBrushConverter() { } //to avoid an annoying warning from XAML designer: "No constructor for type 'xyz' has 0 parameters."  Somehow the inherited one doesn'SelectedFolders do the trick!?!  I guess it's a reflection bug.

    public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      if (WPFHelpers.DesignMode) return (DependencyProperty.UnsetValue);

      //values[0] = IsSelected
      //values[1] = IsExcluded
      //hard coded as much as possible to make sure there's no unecessary cycles lost in this critical section... this routine fires *for every sub tree node* *whenever a checkbox changes* (i.e. A LOT)
      return ((bool)values[1] ? Brushes.LightPink : (bool)values[0] ? Brushes.LightGreen : DependencyProperty.UnsetValue);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }


  public class IsFileInCurrentMediaSubsetConverter : WPFValueConverters.MarkupExtensionConverter, IValueConverter //nugget: leverage the JScript.dll StringEvaluator to build dynamic ValueConverters
  {
    public IsFileInCurrentMediaSubsetConverter() { } //to avoid an XAML annoying warning: "No constructor for type 'xyz' has 0 parameters."  Somehow the inherited one doesn'SelectedFolders do the trick!?!  I guess it's a reflection bug.

    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      return ((int)value == (App.Current.MainWindow as MainWindow).MediaSubsetNumberCurrent); 
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }




}
