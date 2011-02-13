﻿using System;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

public static class WPFHelpers
{
  //public static void UIThreadSafe(Delegate method)
  //{
  //  if (!Application.Current.Dispatcher.CheckAccess())
  //    Application.Current.Dispatcher.Invoke(method);
  //  else
  //    method.DynamicInvoke(null);
  //}


  static public DataGridRow GetDataGridRow(DataGrid grid, int index)
  {
    DataGridRow gridrow = ((DataGridRow)grid.ItemContainerGenerator.ContainerFromIndex(index));
    grid.UpdateLayout(); //nugget: for DataGrid.ItemContainerGenerator.ContainerFromIndex() to work, sometimes you have to bring a "virtualized" DataGridRow into view
    grid.ScrollIntoView(grid.Items[index]);
    gridrow = ((DataGridRow)grid.ItemContainerGenerator.ContainerFromIndex(index));
    return (gridrow);
  }


  //nugget: this the most generic way i could figure this so far...still not satisfied... but there are absolutely *zero* *XAML* based examples for setting the default ElementStyle for *AutoGenerated* columns
  static public void DataGridRightAlignAutoGeneratedNumericColumns(object sender, DataGridAutoGeneratingColumnEventArgs e)
  {
    DataGridTextColumn c = (e.Column as DataGridTextColumn);
    if (c != null && e.PropertyType.IsNumeric())
    {
      if (c.ElementStyle.IsSealed) c.ElementStyle = new Style(c.ElementStyle.TargetType, c.ElementStyle.BasedOn);
      c.ElementStyle.Setters.Add(new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Right));
      c.ElementStyle.Seal();
      c.Binding.StringFormat = "{0:#,0}";
    }
  }


  public static Brush BeginBrushColorAnimation(this Brush brush, Color color, int seconds = 1)
  {
    Brush br = (brush == null)? new SolidColorBrush() : brush.Clone(); //otherwise the default brush is "frozen" and can'SelectedFolders be animated
    br.BeginAnimation(SolidColorBrush.ColorProperty, new ColorAnimation(color, TimeSpan.FromSeconds(seconds)) { AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever });
    return (br);
  }

  public static void EndBrushColorAnimation(this Brush brush)
  {
    brush.BeginAnimation(SolidColorBrush.ColorProperty, null);
  }

  //dictionary the storyboards per each usage
  private static System.Collections.Generic.Dictionary<DefinitionBase, Storyboard> GridSplitterPositions = new System.Collections.Generic.Dictionary<DefinitionBase, Storyboard>();
  public static void GridSplitterOpeningBounce(DefinitionBase RowColDefinition, int InitialSize, bool Opening)
  {
    if (RowColDefinition == null) return; //for when events fire before everything is initialized

    bool IsRow = (RowColDefinition.GetType() == typeof(RowDefinition));

    Storyboard story;
    if (!GridSplitterPositions.TryGetValue(RowColDefinition, out story))
    {
      GridLengthAnimation animation = new GridLengthAnimation();
      animation.To = new GridLength(InitialSize);
      animation.Duration = new TimeSpan(0,0,1);

      Storyboard.SetTarget(animation, RowColDefinition);
      Storyboard.SetTargetProperty(animation, new PropertyPath(IsRow ? "Height" : "Width"));

      GridSplitterPositions[RowColDefinition] = story = new Storyboard();
      story.Children.Add(animation);
    }

    if (Opening) story.Begin();
    else
    {
      story.Stop();

      DependencyProperty CurrentPositionProperty = IsRow ? RowDefinition.HeightProperty : ColumnDefinition.WidthProperty;
      
      //save the current position in the animation's "To" property so it opens back to where it was before we closed it
      (story.Children[0] as GridLengthAnimation).To = (GridLength)RowColDefinition.GetValue(CurrentPositionProperty);

      RowColDefinition.SetValue(CurrentPositionProperty, new GridLength(0, GridUnitType.Pixel));
    }
  }

  //nugget: DoEvents() WPF equivalent: http://kentb.blogspot.com/2008/04/dispatcher-frames.html
  public static void DoEvents()
  {
    //Invoke won'SelectedFolders return until all higher priority messages have been pumped from the queue
    //DispatcherPriority.Background is lower than DispatcherPriority.Input
    //http://msdn.microsoft.com/en-us/library/system.windows.threading.dispatcherpriority.aspx
    Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.Background, new VoidHandler(() => { }));
  }
  private delegate void VoidHandler();

  static public void AutoTabTextBox_TextChanged(object sender, TextChangedEventArgs e)
  {
    TextBox txt = sender as TextBox;
    if (txt.Text.Length == txt.MaxLength)
    {
      //implement "auto-tab" effect
      (e.OriginalSource as UIElement).MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
    }
  }

  static public void IntegerOnlyTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
  {
    int dummy = 0;
    e.Handled = !(int.TryParse(e.Text, out dummy));
  }


  static public bool DesignMode { get { return (System.Diagnostics.Process.GetCurrentProcess().ProcessName == "devenv"); } }

  static public void ComboBoxDataTable(ComboBox cbx, DataTable t, string TextColumnsName, string ValueColumnName, string DefaultText, string DefaultValue)
  {
    if (DefaultText != null)
      cbx.Items.Add(new ComboBoxItem() { Content = DefaultText, Tag = DefaultValue });

    foreach (DataRowView r in t.DefaultView)
    {
      cbx.Items.Add(new ComboBoxItem() { Content = r[TextColumnsName].ToString(), Tag = r[ValueColumnName].ToString() });
    }
  }

  static private DataGridCell _lastCell = null;
  static public void WPFDataGrid_MouseRightButtonUp_SaveCell(object sender, System.Windows.Input.MouseButtonEventArgs e)
  {
    DependencyObject dep = (DependencyObject)e.OriginalSource;

    // iteratively traverse the visual tree
    while ((dep != null) &&
            !(dep is DataGridCell) &&
            !(dep is DataGridColumnHeader) &&
            !(dep is System.Windows.Documents.Run))
    {
      dep = VisualTreeHelper.GetParent(dep);
    }

    if (dep == null)
      return;

    if (dep is DataGridColumnHeader)
    {
      DataGridColumnHeader columnHeader = dep as DataGridColumnHeader;
      (e.Source as DataGrid).ClipboardCopyMode = DataGridClipboardCopyMode.IncludeHeader;
    }
    else (e.Source as DataGrid).ClipboardCopyMode = DataGridClipboardCopyMode.ExcludeHeader;

    if (dep is DataGridCell)
    {
      _lastCell = dep as DataGridCell;
    }
  }

  static public void WPFDataGrid_CopyCell_Click(object sender, System.Windows.RoutedEventArgs e)
  {
    DependencyObject dep = _lastCell;
    if (_lastCell == null) return;

    // navigate further up the tree
    while ((dep != null) && !(dep is DataGridRow))
    {
      dep = VisualTreeHelper.GetParent(dep);
    }
    DataGridRow row = dep as DataGridRow;

    // find the column that this cell belongs to
    DataGridBoundColumn col = _lastCell.Column as DataGridBoundColumn;

    // find the propertyName that this column is bound to
    Binding binding = col.Binding as Binding;
    string boundPropertyName = binding.Path.Path;

    // find the object that is related to this row
    object data = row.Item;

    // extract the propertyName value
    PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(data);

    PropertyDescriptor property = properties[boundPropertyName];
    object value = property.GetValue(data).ToString();

    Clipboard.SetText(value.ToString());
  }

  static public void GridSort(DataGrid Grid, string ColumnName, ListSortDirection Direction)
  {
    var cols = Grid.Columns.Where(c => c.SortMemberPath == ColumnName);
    if (cols.Count() > 0)
      cols.Single().SortDirection = Direction;
  }
}

public class IsEnabledWrapper : IDisposable
{
  private Control _ctrl = null;
  private WaitCursorWrapper _wc = null;

  public IsEnabledWrapper(Control ctrl) : this(ctrl, false) { }

  public IsEnabledWrapper(Control ctrl, bool ShowWaitCursor)
  {
    _ctrl = ctrl;
    _ctrl.IsEnabled = false;
    _wc = new WaitCursorWrapper();
  }

  // IDisposable Members
  public void Dispose()
  {
    _ctrl.IsEnabled = true;
    _wc.Dispose();
  }
}

/// <summary>
/// Made this a Disposable object so that we can wrapper it in a using() {} block as a convenient way to automatically "turn off" the wait cursor
/// </summary>
public class WaitCursorWrapper : IDisposable
{
  //sure enough, others thought of the IDisposable trick: http://stackoverflow.com/questions/307004/changing-the-cursor-in-wpf-sometimes-works-sometimes-doesnt
  private Cursor oldCursor = null;
  public WaitCursorWrapper()
  {
    //nugget: Application.Current.Dispatcher.CheckAccess() confirms whether the current thread is the UI thread before attempting to hit objects tied to the UI thread, like Application.Current.MainWindow
    //in the iTRAAC v2 application, this conflict happens when we use a BackgroundWorker to execute certain datalayer stuff off the UI thread, leaving the UI responsive for more input during the data access, yet we've but we've enabled the datalayer methods to hit this WaitCursor logic via a callback
    //no loss though, because the BackgroundWorkerEx class implements WaitCursor toggling on its own anyway
    //nugget: crazy, Dispatcher.CheckAccess() is hidden from intellisense on purpose!?!: http://arstechnica.com/phpbb/viewtopic.php?f=20&SelectedFolders=103740
    //if (Application.Current.Dispatcher.CheckAccess() 
    //  && Application.Current.MainWindow != null) //for the edge case where we're hitting the database right on App.OnStartup and App.MainWindow hasn'SelectedFolders been populated yet ) 
    //{
    //  oldCursor = Application.Current.MainWindow.Cursor;
    //  Application.Current.MainWindow.Cursor = Cursors.AppStarting;
    //}

    //nugget: Mouse.OverrideCursor is much more effective than Application.Current.MainWindow.Cursor: http://stackoverflow.com/questions/307004/changing-the-cursor-in-wpf-sometimes-works-sometimes-doesnt
    if (Application.Current.Dispatcher.CheckAccess())
    {
      oldCursor = Mouse.OverrideCursor;
      Mouse.OverrideCursor = Cursors.Wait; //AppStarting;
    }
  }

  public void Dispose()
  {
    //if (Application.Current.Dispatcher.CheckAccess() && Application.Current.MainWindow != null)
    //  Application.Current.MainWindow.Cursor = oldCursor;
    if (Application.Current.Dispatcher.CheckAccess())
      Mouse.OverrideCursor = null;
  }

  /// <summary>
  /// Used to pass down to lower layers (e.g. SqlClientHelper) as a callback so they effect a visual delay w/o undesirable bottom-up coupling.
  /// </summary>
  /// <returns></returns>
  static public WaitCursorWrapper WaitCursorWrapperFactory()
  {
    return (new WaitCursorWrapper());
  }
}

