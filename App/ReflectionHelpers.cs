﻿using System.Reflection;
using System.ComponentModel;

public static class ReflectionHelpers
{
  /// <summary>
  /// 
  /// </summary>
  /// <param name="obj"></param>
  /// <param name="propertyName"></param>
  /// <param name="value"></param>
  /// <returns>True = propery exists on obj</returns>
  static public bool PropertySetter(object obj, string property, object value)
  {
    if (obj == null) return (false);

    //check for propertyName as a public field first as a handy way to pass in simple objects
    FieldInfo textField = obj.GetType().GetField(property);
    if (textField != null)
    {
      textField.SetValue(obj, value);
      return (true);
    }

    //otherwise check for things as public properties so we can handle "Label" objects
    //using System.ComponentModel routines like GetProperties is reportedly the most performance optimized way of doing reflection:
    //http://stackoverflow.com/questions/238555/how-do-recordindex-get-the-value-of-memberinfo
    PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(obj);//[propertyName];
    PropertyDescriptor prop = properties[property];
    if ( (prop == null) && (property == "Text") ) prop = properties["Content"]; //little switcheroo for WPF purposes

    if (prop != null)
    {
      prop.SetValue(obj, value);
      return (true);
    }

    return (false); //propertyName was not found
  }

}
