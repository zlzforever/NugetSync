using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace NugetSync
{
    class Program
    {
        static void Main(string[] args)
        {
            var configurationBuilder = new ConfigurationBuilder();
            var configuration = configurationBuilder.AddCommandLine(args).Build();
            var key = configuration["k"];
            var source = configuration["s"];
            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(source))
            {
                Console.WriteLine("Please use command like: nuget-sync -k {apiKey} -s {source}");
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
            foreach (var package in packages)
            {
                i++;
                var file = Path.GetFileName(package);
                var targetPath = Path.Combine(targetDirectory.FullName, file);
                if (!File.Exists(targetPath))
                {
                    File.Copy(package, targetPath);
                    Console.WriteLine($"Copy {file} completed, {i}/{packages.Count}");
                }
            }

            Parallel.ForEach(packages, new ParallelOptions
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