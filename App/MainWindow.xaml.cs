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

using Microsoft.SqlServer.Server;

using System.Text.RegularExpressions;

using WinForms = System.Windows.Forms;

using System.Diagnostics; //Process.Start()

namespace YASBE
{
  public partial class MainWindow : Window
  {

    public MainWindow()
    {
      InitializeComponent();

      gridFilesWorkingSet.AutoGeneratingColumn += new EventHandler<DataGridAutoGeneratingColumnEventArgs>(WPFHelpers.DataGridRightAlignAutoGeneratedNumericColumns); //nugget: this the most generic way recordindex could figure this so far...see helper comments
      gridIncrementalHistory.AutoGeneratingColumn += new EventHandler<DataGridAutoGeneratingColumnEventArgs>(WPFHelpers.DataGridRightAlignAutoGeneratedNumericColumns);

      //bool isAdmin = new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator) ? true : false;
      //if (isAdmin) {MessageBox.Show("you are an administrator");} else{ MessageBox.Show("You are not an administrator");}

      LoadBackProfilesList();
    }

    private void LoadBackProfilesList()
    {
      //load initial backup profile names drop down list so that a previously selected entry stored in Property.Settings has something to Bind to when the Window first comes up
      using (Proc BackupProfiles_s = new Proc("BackupProfiles_s"))
      {
        BackupProfiles_s["@BackupProfileID"] = YASBE.Properties.Settings.Default.SelectedBackupProfileID;

        BackupProfiles_s.ExecuteDataSet();

        cbxBackupProfiles.ItemsSource = BackupProfiles_s.Tables[0].DefaultView; //this fires cbxBackupProfiles_SelectionChanged which is assigned in XAML

        cbxMediaSize.ItemsSource = BackupProfiles_s.dataSet.Tables[1].DefaultView; //I belive this order is what allowed the cbxBackupProfiles Selected row to properly drive the selected cbxMediaSize.MediaSizeID

        lbxFavoriteBurnFolders.ItemsSource = BackupProfiles_s.Tables[2].DefaultView;

        SelectedFoldersTable = BackupProfiles_s.Tables[3];

        IncludedFilesTable = BackupProfiles_s.Tables[4];
        IncludedFilesTable.PrimaryKey = new DataColumn[] { IncludedFilesTable.Columns["FullPath"] };
      }

      ResetAllCounters();
    }

    private void ResetAllCounters()
    {
      WorkingFilesTable = null;
      gridFilesWorkingSet.ItemsSource = null;

      lblCurrentDisc.Content = "-";
      lblTotalBytes.Text = "-";
      lblTotalFiles.Text = "-";
      lblDiscCount.Text = "-";
      lblQtySelected.Text = "-";
      lblBytesSelected.Text = "-";
      lblErrorCount.Text = "-";
    }

    private void cbxBackupProfiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      using (Proc BackupProfile_s = new Proc("BackupProfile_s"))
      {
        BackupProfile_s["@BackupProfileID"] = cbxBackupProfiles.SelectedValue;
        gridIncrementalHistory.ItemsSource = BackupProfile_s.Table0.DefaultView;
        FileSystemNode.LoadSelectedNodes(BackupProfile_s.Tables[1]);
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

    private void OpenStagingFolder_Click(object sender, RoutedEventArgs e)
    {
      System.Diagnostics.Process.Start(cbxBurnFolders.SelectedItem.ToString());
    }

    private void BackupProfileSave_Click(object sender, RoutedEventArgs e)
    {
      DataRowView SelectedProfile = cbxBackupProfiles.SelectedItem as DataRowView;
      using (Proc BackupProfile_u = new Proc("BackupProfile_u"))
      {
        BackupProfile_u["@BackupProfileID"] = cbxBackupProfiles.SelectedValue;
        BackupProfile_u["@Name"] = SelectedProfile["Name"];
        BackupProfile_u["@MediaSizeID"] = SelectedProfile["MediaSizeID"];
        BackupProfile_u["@Folders"] = FileSystemNode.GetSelected(SelectedFoldersTable);
        BackupProfile_u.ExecuteNonQuery();
      }

    }

    private void NewIncremental_Click(object sender, RoutedEventArgs e)
    {
      throw (new Exception("needs some work still"));

      using (Proc Incremental_i = new Proc("Incremental_i"))
      {
        Incremental_i["@BackupProfileID"] = cbxBackupProfiles.SelectedValue;
        gridIncrementalHistory.ItemsSource = Incremental_i.ExecuteDataTable().DefaultView;
      }
    }

    private const string DefaultSort = "FullPath asc, Size desc";
    private const string BigNumberStringFormat = "#,#,#";

    private DataTable WorkingFilesTable = null;
    private DataTable IncludedFilesTable = null;
    private DataTable SelectedFoldersTable = null;

    private void chkShowErrorsOnly_Click(object sender, RoutedEventArgs e)
    {
      WorkingFilesTable.DefaultView.RowFilter = chkShowErrorsOnly.IsChecked.Value ? "SkipError = 1" : "";
    }

    private void IdentifyNextMediaSubset_Click(object sender, RoutedEventArgs e)
    {
      using (WaitCursorWrapper w = new WaitCursorWrapper())
      //using (DataView files = new DataView(WorkingFilesTable))
      {
        if (gridIncrementalHistory.Items.Count == 0)
        {
          MessageBox.Show("Press [New Incremental Backup] button -OR-\r\nRight mouse the Incremental Backup History grid\r\nto establish the container for these new files",
            "No Incremental Backup Container has been established", MessageBoxButton.OK, MessageBoxImage.Information);
          return;
        }

        FileSystemNode.GetSelected(SelectedFoldersTable, IncludedFilesTable);

        using (Proc Files_UploadCompare = new Proc("Files_UploadCompare"))
        {
          Files_UploadCompare["@BackupProfileID"] = (int)cbxBackupProfiles.SelectedValue;
          Files_UploadCompare["@AllFiles"] = IncludedFilesTable;
          WorkingFilesTable = Files_UploadCompare.ExecuteDataTable();
          lblCurrentDisc.Content = Files_UploadCompare["@NextDiscNumber"].ToString();
        }
        DataView files = WorkingFilesTable.DefaultView;
        gridFilesWorkingSet.ItemsSource = files;

        chkShowErrorsOnly.IsChecked = false;
        WorkingFilesTable.DefaultView.RowFilter = "";

        files.Sort = DefaultSort;

        long maxbytes = (long)((decimal)((DataRowView)cbxMediaSize.SelectedItem)["SizeGB"] * 1024 * 1024 * 1024);
        long remainingbytes = files.Cast<DataRowView>().Select(drv => (long)drv["Size"]).Sum();

        lblTotalBytes.Text = remainingbytes.ToString(BigNumberStringFormat); //nugget: requires System.Data.DataSetExtensions assembly added to project References
        lblTotalFiles.Text = files.Count.ToString(BigNumberStringFormat);

        long remainder = 0;
        long DiscCount = Math.DivRem(remainingbytes, maxbytes, out remainder);
        lblDiscCount.Text = String.Format("{0} Full + {1:#.###} GB leftover", DiscCount, remainder / 1024 / 1024 / 1024);

        int retrycount = 10;

        long bytecount = 0;
        long recordcount = 0;
        long errorcount = 0;

        int recordindex = 0; //we need to know this loopcount outside the loop at the end in order to scroll to this current location in the grid
        for (recordindex = 0; recordindex < files.Count; recordindex++)
        {
          //DataGridRow gridrow = WPFHelpers.GetDataGridRow(gridFilesWorkingSet, recordindex);
          long nextsize = Convert.ToInt64(files[recordindex]["Size"]);
          if (bytecount + nextsize > maxbytes)
          {
            //initially assume we just ran into too big of a file to pack on near the end of our free space...
            //so for a few times, try to find another slightly smaller file...
            if (--retrycount > 0) continue; 
            //and after those retries are exhausted, we've successully crammed as close to 100% full as we can at that point
            break;
          }

          retrycount = 10; //when we successfully squeeze on another file, reset the retry count

          if (CheckFileLocked(files[recordindex]["FullPath"].ToString()))
          {
            files[recordindex]["Selected"] = true;
            recordcount++;
            bytecount += nextsize;
          }
          else
          {
            files[recordindex]["SkipError"] = true;
            errorcount++;
          }
        }

        lblQtySelected.Text = recordcount.ToString(BigNumberStringFormat);
        lblBytesSelected.Text = bytecount.ToString(BigNumberStringFormat);
        lblErrorCount.Text = errorcount.ToString(BigNumberStringFormat);

        gridFilesWorkingSet.ScrollIntoView(gridFilesWorkingSet.Items[recordindex]);
      }
    }

    private DataView GetSelectedFiles()
    {
      DataView SelectedFiles = new DataView(WorkingFilesTable);
      SelectedFiles.RowFilter = "Selected = 1";
      return (SelectedFiles);
    }

    private void SymLinkToBurn_Click(object sender, RoutedEventArgs e)
    {
      Process WipeStagingFolder = Process.Start("cmd.exe", "/c deltree.exe \"" + System.IO.Path.Combine(cbxBurnFolders.SelectedValue.ToString(), "*.*") + "\" & pause");
      WipeStagingFolder.WaitForExit();

      Regex rgx = new Regex("[;:]", RegexOptions.Compiled);

      using (WaitCursorWrapper w = new WaitCursorWrapper())
      using (DataView SelectedFiles = GetSelectedFiles())
      {
        foreach (DataRowView r in SelectedFiles)
        {
          string fullpath  = r["FullPath"].ToString();

          string sympath = System.IO.Path.Combine(cbxBurnFolders.SelectedValue.ToString(), rgx.Replace(fullpath, ""));
          Directory.CreateDirectory(sympath.Replace(System.IO.Path.GetFileName(sympath), ""));
          if (rdoSymLink.IsChecked.Value)
           Win32Helpers.CreateSymbolicLink(sympath, fullpath, Win32Helpers.SYMBOLIC_LINK_FLAG.File);
          else
            File.Copy(fullpath, sympath);
        }
      }

      OpenStagingFolder_Click(null, null);
    }

    private void MediaSubsetCommit_Click(object sender, RoutedEventArgs e)
    {
      using (Proc MediaSubset_Commit = new Proc("MediaSubset_Commit"))
      using (DataView SelectedFiles = GetSelectedFiles())
      using (DataTable UploadFiles = SqlClientHelpers.NewTableFromDataView(SelectedFiles, "FullPath", "ModifiedDate", "Size"))
      {
        MediaSubset_Commit["@BackupProfileID"] = (int)cbxBackupProfiles.SelectedValue;
        MediaSubset_Commit["@Files"] = UploadFiles;
        MediaSubset_Commit.ExecuteNonQuery();
      }

      ResetAllCounters();
    }

    private bool CheckFileLocked(string fullpath)
    {
      try
      {
        using (FileStream inputStream = File.Open(fullpath, FileMode.Open, FileAccess.Read, FileShare.None))
        {
          inputStream.Close();
          return true;
        }
      }
      catch (IOException ex)
      {
        return false;
      }
    }

    private void EditMediaSize_Click(object sender, RoutedEventArgs e)
    {
      DataRowView r = ((DataRowView)cbxMediaSize.SelectedItem);
      string size = Microsoft.VisualBasic.Interaction.InputBox("New Size (in GB):", "Edit Media Size", r["SizeGB"].ToString());

      using (Proc MediaSize_u = new Proc("MediaSize_u"))
      {
        MediaSize_u["@MediaSizeID"] = r["MediaSizeID"];
        MediaSize_u["@Size"] = size;
        MediaSize_u.ExecuteNonQuery();
      }

      LoadBackProfilesList();
    }

    private void AddNewFavoriteTempBurnFolder_Click(object sender, RoutedEventArgs e)
    {
      WinForms.FolderBrowserDialog ChooseFolder = new WinForms.FolderBrowserDialog();
      if (ChooseFolder.ShowDialog(this.GetWin32Window()) != WinForms.DialogResult.OK) return;

      using (Proc FavoriteTempBurnFolder_i = new Proc("FavoriteTempBurnFolder_i"))
      {
        FavoriteTempBurnFolder_i["@NewFolder"] = ChooseFolder.SelectedPath;
        lbxFavoriteBurnFolders.ItemsSource = null;
        lbxFavoriteBurnFolders.ItemsSource = FavoriteTempBurnFolder_i.ExecuteDataTable().DefaultView;
      }
    }

    private void AssignNewBurnFolder(string newfolder)
    {
      using (RegistryKey cdburning = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\CD Burning\StagingInfo"))
      {
        foreach (string volumekeystring in cdburning.GetSubKeyNames())
        {
          using (RegistryKey volume = cdburning.OpenSubKey(volumekeystring, true))
          {
            if (volume.GetValue("StagingPath").ToString() == cbxBurnFolders.SelectedValue.ToString())
            {
              volume.SetValue("StagingPath", newfolder);
              break;
            }
          }
        }
      }

      cbxBurnFolders.ItemsSource = null;
      cbxBurnFolders.ItemsSource = WindowsBurnStagingFolders;
    }

    private void AssignFavoriteToSelectedBurnStagingFolder_Click(object sender, RoutedEventArgs e)
    {
      AssignNewBurnFolder(lbxFavoriteBurnFolders.SelectedValue.ToString());
    }

    private void RefreshProfile_Click(object sender, RoutedEventArgs e)
    {
      LoadBackProfilesList();
    }

  }

  public class FileTreeBackgroundBrushConverter : WPFValueConverters.MarkupExtensionConverter, IMultiValueConverter
  {
    public FileTreeBackgroundBrushConverter() { } //to avoid an annoying warning from XAML designer: "No constructor for type 'xyz' has 0 parameters."  Somehow the inherited one doesn'SelectedFoldersTable do the trick!?!  I guess it's a reflection bug.

    public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      if (WPFHelpers.DesignMode) return (DependencyProperty.UnsetValue);

      //values[0] = IsSelected
      //values[1] = IsExcluded
      //hard coded as much as possible to make sure there's no unecessary cycles lost in this critical section... this routine fires *for every sub tree node* *whenever a checkbox changes* (recordindex.e. A LOT)
      return ((bool)values[1] ? Brushes.LightPink : (bool)values[0] ? Brushes.LightGreen : DependencyProperty.UnsetValue);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }

}