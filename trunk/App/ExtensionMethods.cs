using System;
using System.Text;
using System.Collections.Generic;
using System.Text.RegularExpressions;

//make sure to keep this clean of any particular UI assembly dependencies so that it can be
//reused across ASP.Net, Windows.Forms and WPF projects

public static class Extensions
{

  public static string Right(this string s, int count)
  {
    if (count < 1) return ("");
    count = Math.Min(s.Length, count);
    return (s.Substring((s.Length) - count, count));
  }

  public static string Left(this string s, int count)
  {
    if (count < 1) return ("");
    count = Math.Min(s.Length, count);
    return (s.Substring(0, count));
  }


  static private Regex _PluralizeKeyword = new Regex("{(s|es)}", RegexOptions.IgnoreCase);
  static public string Pluralize(string Text, object CheckValue)
  {
    Match m = _PluralizeKeyword.Match(Text);
    return (String.Format(Text.Replace(m.Value, (Convert.ToInt32(CheckValue) > 1) ? m.Groups[1].Value : ""), Convert.ToInt32(CheckValue)));
  }


}
