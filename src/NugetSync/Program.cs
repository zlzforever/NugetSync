using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NugetSync
{
    class Program
    {
        private const string HelperInfo = "Please use command like: nuget-sync -k {apiKey} -s {source}";

        static void Main(string[] args)
        {
            if (args.Length < 4)
            {
                Console.WriteLine(HelperInfo);
                return;
            }

            var key = string.Empty;
            var source = string.Empty;

            try
            {
                for (int j = 0; j < args.Length; ++j)
                {
                    if (args[j] == "-k")
                    {
                        key = args[j + 1];
                    }

                    if (args[j] == "-s")
                    {
                        source = args[j + 1];
                    }
                }
            }
            catch
            {
                Console.WriteLine(HelperInfo);
                return;
            }

            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(source))
            {
                Console.WriteLine(HelperInfo);
                return;
            }

            var currentUserFolder = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            // work around to windows
            currentUserFolder = currentUserFolder.Replace("\\Documents", "");
            var path = $"{currentUserFolder}/.nuget/packages";
            var directories = Directory.GetDirectories(path);
            var packages = new List<string>();
            foreach (var directory in directories)
            {
                var versions = Directory.GetDirectories(directory);
                foreach (var version in versions)
                {
                    var files = Directory.GetFiles(version);
                    packages.AddRange(files.Where(x => x.EndsWith(".nupkg")));
                }
            }

            var targetDirectory = new DirectoryInfo($"{currentUserFolder}/.nuget/all-packages");
            if (!targetDirectory.Exists)
            {
                targetDirectory.Create();
                Console.WriteLine($"Create package cache folder: {targetDirectory}");
            }

            var i = 0;
            var targets = new List<string>();
            foreach (var package in packages)
            {
                i++;
                var file = Path.GetFileName(package);
                var targetPath = Path.Combine(targetDirectory.FullName, file);
                targets.Add(targetPath);
                if (!File.Exists(targetPath))
                {
                    File.Copy(package, targetPath);
                    Console.WriteLine($"Copy {file} completed, {i}/{packages.Count}");
                }
            }

            Parallel.ForEach(targets, new ParallelOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount / 2
            }, (package) =>
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo("dotnet",
                        $"nuget push --skip-duplicate -k {key} -s {source} {package}")
                };
                process.Start();
                process.WaitForExit();
            });

            Console.WriteLine("Bye");
        }
    }
}