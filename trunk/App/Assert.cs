using System;

public static class Assert
{
  static public void Check(Boolean expression, string errormsg)
  {
    if (!expression) throw (new Exception(errormsg));
  }
}
