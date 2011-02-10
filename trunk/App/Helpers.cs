using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO;
using System.Security.Cryptography;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Collections.ObjectModel;

using System.Windows.Controls;

using System.Windows.Markup;
using System.Windows.Data;
using System.Globalization;


namespace YASBE
{
  static class Helpers
  {

    enum SYMBOLIC_LINK_FLAG
    {
      File = 0,
      Directory = 1
    }

    [DllImport("kernel32.dll")]
    [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.I1)]
    static extern bool CreateSymbolicLink(string lpSymlinkFileName, string lpTargetFileName, SYMBOLIC_LINK_FLAG dwFlags);


    static private readonly Type[] numericTypes = new Type[] { typeof(Byte), typeof(Decimal), typeof(Double),
        typeof(Int16), typeof(Int32), typeof(Int64), typeof(SByte),
        typeof(Single), typeof(UInt16), typeof(UInt32), typeof(UInt64)};

    public static bool IsNumeric(this Type type)
    {
      return (type == null)?false:numericTypes.Contains(type);
    }

    static public string GetMD5HashFromFile(string fileName)
    {
      FileStream file = new FileStream(fileName, FileMode.Open);
      MD5 md5 = new MD5CryptoServiceProvider();
      byte[] retVal = md5.ComputeHash(file);
      file.Close();

      StringBuilder sb = new StringBuilder();
      for (int i = 0; i < retVal.Length; i++)
      {
        sb.Append(retVal[i].ToString("x2"));
      }
      return sb.ToString();
    }


    static public bool DesignMode { get { return (System.Diagnostics.Process.GetCurrentProcess().ProcessName == "devenv"); } }

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
