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

using System.Data;
using System.Data.SqlClient;

namespace YASBE
{
  public partial class MainWindow : Window
  {
    
    public MainWindow()
    {
      LoadBackProfilesList();
      InitializeComponent();
      BackupFile.List.CollectionChanged += (s, a) => { if (a.NewItems != null) datagrid.ScrollIntoView(a.NewItems[0]); };
      datagrid.AutoGeneratingColumn += new EventHandler<DataGridAutoGeneratingColumnEventArgs>(datagrid_AutoGeneratingColumn);
    }

    //public DataTable BackupProfiles { get; private set; }

    static private DataTable _BlankBackupProfileFolderTable = null;

    static public DataTable GetBlankBackupProfileTable()
    {
      return (_BlankBackupProfileFolderTable.Clone());
    }

    private bool _beenhere = false;
    private Style _RightAlignStyle = null;
    void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
      if (_beenhere) return; _beenhere = true; //this sucks but is actually recommended by an MSDN MVP: http://social.msdn.microsoft.com/Forums/en/wpf/thread/39ce4ebd-75a6-46d5-b303-2e0f89c6eb8d

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

    private void LoadSelectedBackupProfileSelectedFolders()
    {
      //FileSystemNode.LoadSelectedNodes(ds.Tables[1]);
    }

    private void LoadBackProfilesList()
    {
      //load initial backup profile names drop down list so that a previously selected entry stored in Property.Settings has something to Bind to when the Window first comes up
      using (Proc BackupProfiles_s = new Proc("BackupProfiles_s"))
      {
        cbxBackupProfiles.ItemsSource = BackupProfiles_s.Table0.DefaultView;
        _BlankBackupProfileFolderTable = BackupProfiles_s.dataSet.Tables[1];
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
      //DataRowView v = cbxBackupProfiles.SelectedItem as DataRowView;
      using (Proc BackupProfile_u = new Proc("BackupProfile_u"))
      {
        BackupProfile_u["@BackupProfileID"] = cbxBackupProfiles.SelectedValue;
        BackupProfile_u["@Folders"] = FileSystemNode.GetSelected(MainWindow.GetBlankBackupProfileTable());
        BackupProfile_u.ExecuteNonQuery();
      }

    }
  }

  public class FileTreeBackgroundConverter : WPFValueConverters.MarkupExtensionConverter, IMultiValueConverter
  {
    public FileTreeBackgroundConverter() { } //to avoid an XAML annoying warning from XAML designer: "No constructor for type 'xyz' has 0 parameters."  Somehow the inherited one doesn't do the trick!?!  I guess it's a reflection bug.

    public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      if (Helpers.DesignMode) return (DependencyProperty.UnsetValue);

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


}
