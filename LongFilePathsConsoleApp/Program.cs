using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using WinApi;

namespace LongFilePathsConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var root = @"\\?\c:";
            Console.WriteLine($"Searching at {root}");
            var lfp = new LongFilePath();
            var longPaths = lfp.GetLongFiles(root);
            Console.WriteLine($"Files: {longPaths.Count(x => x.Type == LongPath.LongPathType.File)}");
            Console.WriteLine($"Directories: {longPaths.Count(x => x.Type == LongPath.LongPathType.Dir)}");
            var longPathsGroupedByType = 
                longPaths
                .OrderByDescending(f => f.Path.Length)
                .GroupBy(lp => lp.Type)
                .ToList();

            Action<string, LongPath.LongPathType, Func<string, bool>> tryToDelete = (path, fileOrDir, deleteFunc) =>
            {
                if (deleteFunc(path))
                {
                    Console.WriteLine($"Deleted {fileOrDir} {path}");
                }
                else
                {
                    Console.WriteLine($"Error: {new Win32Exception(Marshal.GetLastWin32Error()).Message}");
                    Console.WriteLine($"Could not delete {fileOrDir} path: {path}");
                }
            };
            
            longPathsGroupedByType.ForEach(grouping =>
            {
                foreach (var pathType in grouping)
                {
                    if (grouping.Key == LongPath.LongPathType.File)
                        tryToDelete(pathType.Path, grouping.Key, lfp.DeleteLongFile);
                    tryToDelete(pathType.Path, grouping.Key, lfp.RemoveLongDirectory);
                }
            });
            
            Console.WriteLine("Press Escape (Esc) key to quit.");
            Console.WriteLine();
            ConsoleKeyInfo cki;
            Console.TreatControlCAsInput = true;
            do
            {
                cki = Console.ReadKey(true);

            } while (cki.Key != ConsoleKey.Escape);
        }
    }
}
