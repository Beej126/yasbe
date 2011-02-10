using System;
using System.Drawing;
using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
//using System.Windows.Media; //brushes

namespace WPFValueConverters
{
  //by making the ValueConverter a MarkupExtension we avoid the typical step of needing to create an instance of the converter in the XAML Resources block

  public abstract class MarkupExtensionConverter : MarkupExtension
  {
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
      return this;
    }

    public MarkupExtensionConverter() { }
  }

  public class BoolToItalicConverter : MarkupExtensionConverter, IValueConverter
  {
    public BoolToItalicConverter() { } //to avoid an XAML annoying warning: "No constructor for type 'xyz' has 0 parameters."  Somehow the inherited one doesn't do the trick!?!  I guess it's a reflection bug.

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      return ((bool)value ? System.Windows.FontStyles.Italic : System.Windows.FontStyles.Normal);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }

  public class InverseBooleanConverter : MarkupExtensionConverter, IValueConverter
  {
    public InverseBooleanConverter() { } //to avoid an annoying XAML warning: "No constructor for type 'xyz' has 0 parameters."  Somehow the inherited one doesn't do the trick!?!  I guess it's a reflection bug.

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      return !(bool)value;
    }

    public object ConvertBack(object value, Type targetType, object parameter,
        CultureInfo culture)
    {
      return !(bool)value;
      //throw new NotSupportedException();
    }
  }

  public class BooleanToVisibilityConverter : MarkupExtensionConverter, IValueConverter
  {
    enum Direction
    {
      Normal, Inverse
    }

    public BooleanToVisibilityConverter() { } //to avoid an XAML annoying warning: "No constructor for type 'xyz' has 0 parameters."  Somehow the inherited one doesn't do the trick!?!  I guess it's a reflection bug.

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value == null) return (Visibility.Visible);

      bool b = bool.Parse(value.ToString());
      Direction d = (parameter == null) ? Direction.Normal : (Direction)Enum.Parse(typeof(Direction), (string)parameter);

      return (((d == Direction.Normal) ? b : !b) ? Visibility.Visible : Visibility.Collapsed);
    }

    public object ConvertBack(object value, Type targetType,
        object parameter, CultureInfo culture)
    {
      return null;
    }
  }


  /// <summary>
  /// This converter's Parameter should specifiy a Path string which refers to a string Property on current DataContext
  /// That string Property should return another Path string which corresponds to the desired Property which will be pulled and returned
  /// This converter allows us to do a "double hop", otherwise unavailable in basic Binding syntax
  /// Don't use it for something that should change often, looping and reflection obviously aren't performant
  /// </summary>
  public class IndirectPropertyConverter : MarkupExtensionConverter, IValueConverter
  {
    public IndirectPropertyConverter() { } //to avoid an XAML annoying warning: "No constructor for type 'xyz' has 0 parameters."  Somehow the inherited one doesn't do the trick!?!  I guess it's a reflection bug.

    public object Convert(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      if (value == null) return (null);

      bool firstpass = true;
      string[] PropertyPathPieces = new string[] { parameter.ToString() };
      string[] indexer = null;
      int i = 0;
      while (i < PropertyPathPieces.Length)
      {
        string PropertyPathPiece = PropertyPathPieces[i];
        PropertyInfo pi = value.GetType().GetProperty(PropertyPathPiece); //first look for a normal propertyName
        if (pi == null)
        {
          pi = value.GetType().GetProperty("Item", new System.Type[] { typeof(string) }); //otherwise, look for a string indexer
          indexer = new string[] { PropertyPathPiece };
        }
        object obj = pi.GetValue(value, indexer);
        indexer = null;

        if (firstpass) {
          PropertyPathPieces = obj.ToString().Split('.');
          firstpass = false;
        }
        else
        {
          value = obj;
          i++;
        }
      }

      return (value.ToString());
    }

    public object ConvertBack(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      throw new System.NotImplementedException();
    }
  }

  /// <summary>
  /// Parameter should be comma delimited string of Brush color strings for True, False (e.g. Gray, Black)
  /// </summary>
  public class BoolToSolidBrushConverter : MarkupExtensionConverter, IValueConverter
  {
    public BoolToSolidBrushConverter() { } //to avoid an XAML annoying warning: "No constructor for type 'xyz' has 0 parameters."  Somehow the inherited one doesn't do the trick!?!  I guess it's a reflection bug.

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      string[] colors = parameter.ToString().Split(',');
      return new System.Windows.Media.BrushConverter().ConvertFromString((bool)value ? colors[0].Trim() : colors[1].Trim());
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }

  public class IntToSolidBrushConverter : MarkupExtensionConverter, IValueConverter
  {
    public IntToSolidBrushConverter() { } //to avoid an XAML annoying warning: "No constructor for type 'xyz' has 0 parameters."  Somehow the inherited one doesn't do the trick!?!  I guess it's a reflection bug.

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      return new System.Windows.Media.BrushConverter().ConvertFromString(parameter.ToString().Split(',')[(int)value]);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }

  public class BoolToParmStringConverter : MarkupExtensionConverter, IValueConverter
  {
    public BoolToParmStringConverter() { } //to avoid an XAML annoying warning: "No constructor for type 'xyz' has 0 parameters."  Somehow the inherited one doesn't do the trick!?!  I guess it's a reflection bug.

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      string[] strings = parameter.ToString().Split(',');
      return((bool)value ? strings[0] : strings[1]);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }

  public class BoolExpressionToVisibility : MarkupExtensionConverter, IValueConverter
  {
    public BoolExpressionToVisibility() { } //to avoid an XAML annoying warning: "No constructor for type 'xyz' has 0 parameters."  Somehow the inherited one doesn't do the trick!?!  I guess it's a reflection bug.

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value == null || value == DBNull.Value) return (Visibility.Visible);
      return (StringEvaluator.EvalToBool(parameter.ToString().Replace("?", value.ToString())) ? Visibility.Visible : Visibility.Collapsed);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }

  public class BoolExpressionToBool : MarkupExtensionConverter, IValueConverter //nugget: leverage the JScript.dll StringEvaluator to build dynamic ValueConverters
  {
    public BoolExpressionToBool() { } //to avoid an XAML annoying warning: "No constructor for type 'xyz' has 0 parameters."  Somehow the inherited one doesn't do the trick!?!  I guess it's a reflection bug.

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      return (StringEvaluator.EvalToBool(parameter.ToString().Replace("?", value.ToString()).ToLower())); //nugget:
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }



  public class AND : WPFValueConverters.MarkupExtensionConverter, IMultiValueConverter
  {
    public AND() { } //to avoid an XAML annoying warning from XAML designer: "No constructor for type 'xyz' has 0 parameters."  Somehow the inherited one doesn't do the trick!?!  I guess it's a reflection bug.

    public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      bool result = true;
      foreach (object value in values) result = result && ((value == DependencyProperty.UnsetValue) ? false : (bool)value);

      return (result);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
    {
      throw new NotImplementedException();
    }

  }


  public class IndirectMultiValue : WPFValueConverters.MarkupExtensionConverter, IMultiValueConverter
  {
    public IndirectMultiValue() { } //to avoid an XAML annoying warning from XAML designer: "No constructor for type 'xyz' has 0 parameters."  Somehow the inherited one doesn't do the trick!?!  I guess it's a reflection bug.

    public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      object obj = values[0]; 
      string propertyName = values[1].ToString(); 
      string[] indexer = null;

      PropertyInfo pi = obj.GetType().GetProperty(propertyName); //first look for a normal propertyName

      if (pi == null)
      {
        pi = obj.GetType().GetProperty("Item", new System.Type[] { typeof(string) }); //otherwise, look for a string indexer
        if (pi == null) return (null);
        indexer = new string[] { propertyName };
      }

      return (pi.GetValue(obj, indexer));

    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
    {
      throw new NotImplementedException();
    }

  }



}