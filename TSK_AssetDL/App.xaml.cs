using System;
using System.Collections.Generic;
using System.Windows;
using static System.Net.WebRequestMethods;

namespace TSK_AssetDL
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static string Root = Environment.CurrentDirectory;
        public static string Respath = String.Empty;
        public static int TotalCount = 0;
        public static int glocount = 0;
        public static string ServerURL = "https://dz87n5pasv7ep.cloudfront.net/assetbundle/game/";
        public static string ResUrl = ServerURL + "catalog_0.0.0.json";
        public static List<string> log = new List<string>();
    }
}
