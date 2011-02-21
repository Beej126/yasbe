using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Win32;
using System.Runtime.InteropServices;

static class Win32Helpers
{
  public enum SYMBOLIC_LINK_FLAG
  {
    File = 0,
    Directory = 1
  }

  [DllImport("kernel32.dll")]
  [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.I1)]
  static public extern bool CreateSymbolicLink(string lpSymlinkFileName, string lpTargetFileName, SYMBOLIC_LINK_FLAG dwFlags);


  public static System.Windows.Forms.IWin32Window GetWin32Window(this System.Windows.Media.Visual visual)
  {
    var source = System.Windows.PresentationSource.FromVisual(visual) as System.Windows.Interop.HwndSource;
    System.Windows.Forms.IWin32Window win = new Win32Window(source.Handle);
    return win;
  }

  private class Win32Window : System.Windows.Forms.IWin32Window
  {
    private readonly System.IntPtr _handle;
    public Win32Window(System.IntPtr handle)
    {
      _handle = handle;
    }

    #region IWin32Window Members
    System.IntPtr System.Windows.Forms.IWin32Window.Handle
    {
      get { return _handle; }
    }
    #endregion
  }


}
