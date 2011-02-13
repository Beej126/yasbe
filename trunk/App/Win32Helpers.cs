using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Win32;
using System.Runtime.InteropServices;

class Win32Helpers
{
  public enum SYMBOLIC_LINK_FLAG
  {
    File = 0,
    Directory = 1
  }

  [DllImport("kernel32.dll")]
  [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.I1)]
  static public extern bool CreateSymbolicLink(string lpSymlinkFileName, string lpTargetFileName, SYMBOLIC_LINK_FLAG dwFlags);

}
