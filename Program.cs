using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace HyperlapseBatchProcessor
{

    class Program {

        private static readonly string[] ValidVideoExtensions = new string[] { "mp4", "wmv", "mov" };
        private static readonly string HyperlapsePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft Hyperlapse Pro");

        static Program()
        {
            AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolve;
        }

        static void Main(string[] args) {
            try {
                if (!Directory.Exists(HyperlapsePath))
                {
                    Console.WriteLine("Hyperlapse installation folder not found");
                    return;
                }

                // parse arguments
                int speedupFactor;
                if (args.Length < 1) {
                    Console.WriteLine("Wrong number of arguments. Usage:");
                    Console.WriteLine("> " + Path.GetFileNameWithoutExtension(System.Diagnostics.Process.GetCurrentProcess().ProcessName) + " <speedup factor>");
                    return;                
                }

                if (!int.TryParse(args[0], out speedupFactor)) {
                    Console.WriteLine("Wrong speedup factor");
                    return;
                }

                // get current directory
                var baseDir = new DirectoryInfo(Environment.CurrentDirectory);
                if (!baseDir.Exists) {
                    Console.WriteLine("Directory '" + baseDir + "' does not exist");
                    return;
                }

                // get video files to process
                var videoFiles = ValidVideoExtensions.Aggregate(Enumerable.Empty<FileInfo>(), (files, extension) => files.Concat(baseDir.GetFiles("*." + extension))).ToArray();
                if (videoFiles.Count() == 0) {
                    Console.WriteLine("No video files found");
                    return;
                }

                videoFiles = videoFiles.OrderBy(f => f.Name).ToArray();
                HyperlapseWrapper.ProcessFiles(videoFiles, speedupFactor, /*outputFramesPerSecond*/null);

                Console.WriteLine("Finished");
            } finally {
                //Console.ReadLine();
            }
        }

        private static Assembly AssemblyResolve(object sender, ResolveEventArgs args)
        {
            var assemblyName = args.Name.Substring(0, args.Name.IndexOf(","));
            if(assemblyName == "Microsoft.Research.Hyperlapse.Desktop")
            {
                assemblyName += ".exe";
            } else
            {
                assemblyName += ".dll";
            }
            assemblyName = Path.Combine(HyperlapsePath, assemblyName);
            return Assembly.LoadFrom(assemblyName);
        }

        
    }
}
