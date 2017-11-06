using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace WinApi
{
    public class LongFilePath
    {
        // If the function succeeds, the return value is the length, in TCHARs, of the string copied to lpBuffer, not including the terminating null character.
        // If the lpBuffer buffer is too small to contain the path, the return value is the size, in TCHARs, of the buffer that is required to hold the path and the terminating null character.
        // If the function fails for any other reason, the return value is zero.To get extended error information, call GetLastError.

        [DllImport("kernel32.dll", EntryPoint = "GetFullPathName")]
        private static extern uint GetFullPathName(string lpFileName, uint nBufferLength, [Out] StringBuilder lpBuffer, out StringBuilder lpFilePart);

        // FindFirstFile
        // https://msdn.microsoft.com/en-us/library/windows/desktop/aa364418(v=vs.85).aspx
        //
        // WIN32_FIND_DATA
        // https://msdn.microsoft.com/en-gb/library/windows/desktop/aa365740(v=vs.85).aspx

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct WIN32_FIND_DATA
        {
            internal FileAttributes dwFileAttributes;
            internal FILETIME ftCreationTime;
            internal FILETIME ftLastAccessTime;
            internal FILETIME ftLastWriteTime;
            internal uint nFileSizeHigh; //changed all to uint, otherwise you run into unexpected overflow
            internal uint nFileSizeLow;  //|
            internal uint dwReserved0;   //|
            internal uint dwReserved1;   //v
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            internal string cFileName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
            internal string cAlternate;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr FindFirstFile(string lpFileName, out WIN32_FIND_DATA lpFindFileData);

        // FindNextFile
        // https://msdn.microsoft.com/en-us/library/windows/desktop/aa364428(v=vs.85).aspx
        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        private static extern bool FindNextFile(IntPtr hFindFile, out WIN32_FIND_DATA lpFindFileData);

        [DllImport("kernel32.dll")]
        private static extern bool FindClose(IntPtr hFindFile);

        // DeleteFile
        // https://msdn.microsoft.com/en-us/library/windows/desktop/aa363915(v=vs.85).aspx
        [DllImport("kernel32.dll", EntryPoint = "DeleteFileW", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeleteFile(string lpFileName);

        [DllImport("kernel32.dll", EntryPoint = "RemoveDirectoryW", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool RemoveDirectory(string lpPathName);

        public List<LongPath> GetLongFiles(string directory)
        {
            var longPaths = new List<LongPath>();
            var dirsToCheck = new Queue<string>();
            Process(directory, dirsToCheck, longPaths);
            do
            {
                var dir = dirsToCheck.Dequeue();
                Process(dir, dirsToCheck, longPaths);
            } while (dirsToCheck.Count != 0);
            return longPaths;
        }
        
        private void Process(string dir, Queue<string> toDo, List<LongPath> longPaths)
        {
            IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);
            WIN32_FIND_DATA findData;
            IntPtr fileHandle = FindFirstFile(dir + @"\*", out findData);
            if (fileHandle == INVALID_HANDLE_VALUE)
                return;
            
            do
            {
                if (findData.cFileName == "." || findData.cFileName == "..") continue;

                var path = Path(dir, findData.cFileName);
                if (IsDirectory(findData))
                {
                    toDo.Enqueue(path);
                    if(path.Length > 259)
                        longPaths.Add(new LongPath { Path = path, Type = LongPath.LongPathType.Dir });
                }

                if(IsFile(findData) & path.Length > 259)
                    longPaths.Add(new LongPath { Path = path, Type = LongPath.LongPathType.File });

            } while (FindNextFile(fileHandle, out findData));
            FindClose(fileHandle);
        }

        private string Path(string dir, string fileName)
        {
            return $@"{dir}\{fileName}";
        }

        private bool IsDirectory(WIN32_FIND_DATA findData)
        {
            return (findData.dwFileAttributes & FileAttributes.Directory) == FileAttributes.Directory;
        }

        private bool IsFile(WIN32_FIND_DATA findData)
        {
            return (findData.dwFileAttributes & FileAttributes.Directory) == 0;
        }

        public bool DeleteLongFile(string longFilePath)
        {
            return DeleteFile(longFilePath);
        }

        public bool RemoveLongDirectory(string longDirPath)
        {
            return RemoveDirectory(longDirPath);
        }
    }

    public class LongPath
    {
        public enum LongPathType
        {
            File,
            Dir
        }

        public string Path { get; set; }
        public LongPathType Type { get; set; }
    }
}
