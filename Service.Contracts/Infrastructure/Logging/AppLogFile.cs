using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace Service.Contracts.Logging
{
    class AppLogFile : IAppLogFile
    {
        private object syncObj = new object();
        private string filePath;
        private FileStream fs;
        private StreamWriter logFile;
        private int maxSize = 2097152;
        private bool disposed;
        private int maxFiles = 3;

        public AppLogFile()
        {
        }


        public IAppLogFile Initialize(string fileName, int maxSize = 2097152)
        {
            if (String.IsNullOrWhiteSpace(fileName))
                throw new ArgumentNullException(nameof(fileName));
            this.maxSize = maxSize;
            OpenFile(fileName);
            return this;
        }


        public void LogEvent(ILogEntry e)
        {
            // Writes down to the log file
            lock (syncObj)
            {
                if (disposed) return;
                logFile.Write(e.ToString());
                logFile.Flush();
                if (fs != null)
                {
                    if (fs.Length > maxSize)
                        Trim(0);
                }
            }
        }

        public void Dispose()
        {
            lock (syncObj)
            {
                if (!disposed)
                {
                    disposed = true;
                    logFile.Flush();
                    logFile.Dispose();
                    fs.Dispose();
                    fs = null;
                    logFile = null;
                }
            }
        }


        private void OpenFile(string fileName)
        {
            if (String.IsNullOrWhiteSpace(fileName))
                fileName = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.Create) + "\\logfile.log";
            string path = Path.GetDirectoryName(fileName);
            if (String.IsNullOrWhiteSpace(path))
                fileName = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.Create) + "\\" + fileName;
            if (String.Compare(Path.GetExtension(fileName), ".log", true) != 0)
                fileName += ".log";
            if (!Directory.Exists(Path.GetDirectoryName(fileName)))
                Directory.CreateDirectory(Path.GetDirectoryName(fileName));
            try
            {
                fs = File.Open(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
                filePath = fileName;
            }
            catch (Exception ex)
            {
                if (ex is IOException || ex is UnauthorizedAccessException)
                {
                    string alternateFileName;
                    for (int i = 0; i < 20; i++)
                    {
                        alternateFileName = String.Format("{0}\\{1}-{2}{3}", Path.GetDirectoryName(fileName), Path.GetFileNameWithoutExtension(fileName), i.ToString("D2"), Path.GetExtension(fileName));
                        try
                        {
                            fs = File.Open(alternateFileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
                            filePath = alternateFileName;
                            break;
                        }
                        catch { }
                    }
                }
                else throw;
            }
            if (fs != null)
            {
                fs.Seek(0L, SeekOrigin.End);
                logFile = new StreamWriter(fs);
                logFile.AutoFlush = true;
                if (fs.Length > maxSize)
                    Trim(0);
            }
        }

        public void Trim(int newSize)
        {

            var currentMonth = DateTime.Now.ToString("yyyyMM");
            // logfilename-202204-0.log, logfilename-202204-1.log logfilename-202204-2.log
            var logPatterns = string.Format("{0}-{1}-*.log", Path.GetFileNameWithoutExtension(filePath), currentMonth);// Path.GetFileNameWithoutExtension(filePath) + '-' + currentMonth + "-*.log";
            var path =  Path.Combine(Path.GetDirectoryName(filePath), "history");

            System.IO.Directory.CreateDirectory(path); //Creates all directories and subdirectories in the specified path unless they already exist.

            DirectoryInfo di = new DirectoryInfo(path);
            List<FileInfo> files = di.GetFiles(logPatterns, SearchOption.TopDirectoryOnly).OrderBy(f => f.CreationTime).ToList();
            int counter = 0;

            while (files.Count >= maxFiles)
            {
                var older = files.First();
                files.RemoveAt(0);

                File.Delete(older.FullName);
            }

            if (files.Count > 0)
            {
                var lastest = files.Last();
                var lastestName = Path.GetFileNameWithoutExtension(lastest.FullName);
                Match match = Regex.Match(lastestName, @".*\-\d{6}\-(\d{2})$");

                if (match.Success)
                {
                    //var m = match.NextMatch();
                    if (int.TryParse(match.Groups[1].Value, out int currentCounter))
                        counter = currentCounter + 1;
                }
            }

            
            if (fs == null || newSize < 0) return;
            byte[] buffer = new byte[4096];
            if (fs.Length > newSize)
            {
                
                logFile.Flush();
                fs.Close();
                

                var backupFileName = string.Format("{0}-{1}-{2}.log", Path.GetFileNameWithoutExtension(filePath), currentMonth, counter.ToString("D2"));

                File.Move(filePath, Path.Combine(path, backupFileName));
                fs = File.Open(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
                fs.Seek(0L, SeekOrigin.End);
                logFile = new StreamWriter(fs);
                logFile.AutoFlush = true;


            }
        }
        // OLD METHOD - leave hear for faster recovery, will be remove after ensure the new method is working ok
        // the new method save history logs inner [LOGFOLDERPATH]/history folder
        //public void Trim(int newSize)
        //{
        //    newSize = maxSize / 2;
        //    int b, rb;
        //    if (fs == null || newSize < 0) return;
        //    byte[] buffer = new byte[4096];
        //    if (fs.Length > newSize)
        //    {
        //        using (FileStream tmp = new FileStream(filePath + ".tmp", FileMode.OpenOrCreate, FileAccess.Write))
        //        {
        //            tmp.SetLength(0L);
        //            fs.Seek(-newSize, SeekOrigin.End);
        //            do
        //            {
        //                b = fs.ReadByte();
        //            } while (b != 10);
        //            do
        //            {
        //                rb = fs.Read(buffer, 0, buffer.Length);
        //                tmp.Write(buffer, 0, rb);
        //            } while (rb == buffer.Length);
        //        }
        //        fs.Close();
        //        File.Delete(filePath);
        //        File.Move(filePath + ".tmp", filePath);
        //        fs = File.Open(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
        //        fs.Seek(0L, SeekOrigin.End);
        //        logFile = new StreamWriter(fs);
        //        logFile.AutoFlush = true;
        //    }
        //}

        public string FileName
        {
            get { return filePath; }
        }


        public void Clear()
        {
            lock (syncObj)
            {
                if (disposed) return;
                logFile.Flush();
                fs.SetLength(0L);
            }
        }
    }
}
