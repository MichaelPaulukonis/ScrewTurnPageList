using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Diagnostics;
using System.IO;

namespace stw_build_auto_update
{
    class Program
    {
        static int Main(string[] args)
        {

            // args contain the target file, destination, optionally update URL
            // update URL is only used if file does not currently exist

            var overriderFileUrl = false;

            //http://www.codeproject.com/KB/dotnet/build_versioning.aspx

            if (args.Length == 0 || args.Length > 3)
            {
                var appName = Assembly.GetExecutingAssembly().GetName().Name;
                Console.WriteLine("Usage: {0} <filename> <destination-dir> [updateURL].\n", appName);
                return 1;
            }
            var pathFile = args[0];

            var fileInfo = new FileInfo(pathFile);
            if (!fileInfo.Exists)
            {
                Console.WriteLine("File does not exist.");
                return 1;
            }

            string updateFile = null;
            if (args.Length >= 2)
            {
                updateFile = args[1].Trim("\\".ToCharArray());
                var dir = Path.GetDirectoryName(updateFile);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
            }

            var updateURL = string.Empty;
            if (args.Length == 3)
            {
                updateFile = args[2];
                overriderFileUrl = true;
            }

            // if file exists
            //    read first line, and append pipe + version
            //    if URL supplied, write as second line

            FileVersionInfo verInfo = FileVersionInfo.GetVersionInfo(pathFile);

            //string version = verInfo.FileMajorPart + "." + verInfo.FileMinorPart + "."
            //                 + verInfo.FileBuildPart + "." + verInfo.ProductPrivatePart;
            // http://stackoverflow.com/questions/1388178/information-from-exe-file
            string version = Assembly.LoadFrom(pathFile).GetName().Version.ToString();



            StreamReader sr = new StreamReader(updateFile);

            string allVersions = sr.ReadLine();
            allVersions += "|" + version;

            // TODO: try..catch that makes sense
            if (!overriderFileUrl)
            {
                updateURL = sr.ReadLine();
            }


            sr.Close();

            StreamWriter sw = new StreamWriter(updateFile);

            sw.WriteLine(allVersions);
            sw.WriteLine(updateURL);
            sw.Close();

            return 0;


        }
    }
}
