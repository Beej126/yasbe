using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;

namespace YASBE
{
  /// <summary>
  /// Interaction logic for App.xaml
  /// </summary>
  public partial class App : Application
  {
    protected override void OnStartup(StartupEventArgs e)
    {
      base.OnStartup(e);
      Proc.ConnectionString = @"Data Source=.\dev2008;User ID=sa;Password=annoying;Initial Catalog=YASBE;";
      //the App.DispatcherUnhandledException is the preferrable catcher because you can "Handle" it and prevent the app from crashing
      App.Current.DispatcherUnhandledException += new System.Windows.Threading.DispatcherUnhandledExceptionEventHandler(Current_DispatcherUnhandledException);
      //AppDomain.UnhandledException is only good for last ditch capturing of the problematic state info... if an Exception bubbles up this far, the app is going down no way to prevent
      System.AppDomain.CurrentDomain.UnhandledException += new System.UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
    }

    void Current_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
      e.Handled = true;
      DefaultExceptionHandler(e.Exception);
    }

    void CurrentDomain_UnhandledException(object sender, System.UnhandledExceptionEventArgs e)
    {
      DefaultExceptionHandler(e.ExceptionObject as Exception);
    }


    private void DefaultExceptionHandler(Exception ex)
    {
      MessageBox.Show("Unexpected Error" + ((ex != null) ? ": " + ex.Message : ""));
    }

    protected override void OnExit(ExitEventArgs e)
    {
      YASBE.Properties.Settings.Default.Save();  //nugget: write any settings changes out to App.Config file
    }


  }
}
