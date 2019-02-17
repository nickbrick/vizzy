using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace vizzy
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        void AppStartup(object sender, StartupEventArgs e)
        {
            string file;
            //file = e.Args.Length > 0 ? e.Args[0] : System.IO.Path.GetFullPath(@"..\..\..\testfiles\welcome");
            file = e.Args.Length > 0 ? e.Args[0] : System.IO.Path.GetFullPath(@"Resources\welcome");
            MainWindow mainWindow = new MainWindow(file);
            mainWindow.Show();
        }
    }
}
