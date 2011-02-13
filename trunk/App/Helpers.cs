﻿using System;
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
      for (int i = 0; i < retVal.Length; i++)
      {
        sb.Append(retVal[i].ToString("x2"));
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