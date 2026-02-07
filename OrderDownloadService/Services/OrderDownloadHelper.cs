using Newtonsoft.Json;
using OrderDonwLoadService.Model;
using Polly;
using Service.Contracts;
using StructureInditexOrderFile;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OrderDonwLoadService.Services
{
    public static class OrderDownloadHelper
    {
        public static List<Credential> LoadInditexCreadentials(IAppLog _log)
        {
            var baseDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase).Replace("file:\\", "");
            var path = Path.Combine(baseDir, "InditexCredentials.json");
            if (!File.Exists(path))
            {
                _log.LogMessage($"Error not found File InditexCredentials.json in path {path} ");
                return null;
            }
            string cpf = File.ReadAllText(path);

            return JsonConvert.DeserializeObject<ApiCredentials>(cpf).Credentials;


        }

        public static string SaveFileIntoWorkDirectory(
            InditexOrderData orderLabel,
            string workDirectory,
            bool overwrite = true,
            bool skipIfUnchanged = true)
        {
            if (orderLabel?.POInformation == null)
                throw new ArgumentNullException(nameof(orderLabel), "The order label data cannot be null.");

            var id = orderLabel.POInformation.productionOrderNumber.ToString();
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("LabelOrderId is null or empty.", nameof(orderLabel));

            foreach (var c in Path.GetInvalidFileNameChars())
                id = id.Replace(c, '_');

            Directory.CreateDirectory(workDirectory);

            var finalPath = Path.Combine(workDirectory, $"{id}.json");
            var tempPath = Path.Combine(workDirectory, $".{id}.{Guid.NewGuid():N}.tmp");

            var json = JsonConvert.SerializeObject(orderLabel, Formatting.Indented);
            var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

            var mutexName = BuildGlobalMutexName(finalPath);
            using (var mutex = new Mutex(false, mutexName))
            {
                bool lockTaken = false;
                try
                {
                    try
                    {
                        lockTaken = mutex.WaitOne(TimeSpan.FromSeconds(15));
                    }
                    catch (AbandonedMutexException)
                    {
                        lockTaken = true;
                    }

                    if (!lockTaken)
                        throw new TimeoutException($"Timeout waiting for file lock: {finalPath}");

                    if (skipIfUnchanged && File.Exists(finalPath))
                    {
                        if (ContentEquals(finalPath, json, encoding))
                            return finalPath;
                    }

                    const int maxRetries = 3;
                    var rnd = new Random();

                    for (int attempt = 1; attempt <= maxRetries; attempt++)
                    {
                        try
                        {
                            using (var fs = new FileStream(
                                tempPath,
                                FileMode.CreateNew,
                                FileAccess.Write,
                                FileShare.None,
                                4096,
                                FileOptions.WriteThrough))
                            using (var sw = new StreamWriter(fs, encoding))
                            {
                                sw.Write(json);
                                sw.Flush();
                                fs.Flush(true);
                            }

                            if (overwrite && File.Exists(finalPath))
                            {
                                File.Replace(tempPath, finalPath, destinationBackupFileName: null);
                            }
                            else
                            {
                                File.Move(tempPath, finalPath);
                            }

                            return finalPath;
                        }
                        catch (IOException ioEx)
                        {
                            SafeDelete(tempPath);
                            if (attempt == maxRetries)
                                throw new IOException($"Failed to save file after {maxRetries} attempts. Path: {finalPath}", ioEx);

                            int delayMs = (int)(Math.Pow(2, attempt - 1) * 300) + rnd.Next(0, 200);
                            Thread.Sleep(delayMs);
                        }
                        catch (UnauthorizedAccessException)
                        {
                            SafeDelete(tempPath);
                            throw;
                        }
                        catch
                        {
                            SafeDelete(tempPath);
                            throw;
                        }
                    }

                    throw new IOException("Unexpected fall-through in save loop.");
                }
                finally
                {
                    if (lockTaken)
                    {
                        try { mutex.ReleaseMutex(); } catch { /* ignore */ }
                    }
                }
            }
        }

        private static bool ContentEquals(string existingPath, string json, Encoding encoding)
        {
            byte[] newHash;
            using (var shaNew = SHA256.Create())
            {
                var jsonBytes = encoding.GetBytes(json);
                newHash = shaNew.ComputeHash(jsonBytes);
            }

            byte[] oldHash;
            using (var shaOld = SHA256.Create())
            using (var fs = new FileStream(existingPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                oldHash = shaOld.ComputeHash(fs);
            }

            return oldHash.SequenceEqual(newHash);
        }

        private static string BuildGlobalMutexName(string path)
        {
            var full = Path.GetFullPath(path).ToLowerInvariant();
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(full));
                var prefix = BitConverter.ToString(bytes, 0, 8).Replace("-", "");
                return @"Global\SaveLabel_" + prefix;
            }
        }

        private static void SafeDelete(string p)
        {
            try { if (!string.IsNullOrEmpty(p) && File.Exists(p)) File.Delete(p); } catch { /* ignore */ }
        }


        public static async Task<AutenticationResult> CallGetToken(
                IAppConfig appConfig, string userNameInditex,
                string passwordInditex, IApiCallerService apiCaller)
        {
            //Politica de reintentos acitiva
            string url = appConfig.GetValue<string>("DownloadServices.TokenApiUrl", "https://Inditex.okta.com/oauth2/default/v1/token");
            var maxTrys = appConfig.GetValue<int>("DownloadServices.MaxTrys", 2);
            var timeToWait = TimeSpan.FromSeconds(appConfig.GetValue<double>("DownloadServices.SecondsToWait", 240));

            var retryPolity = Policy.Handle<Exception>().WaitAndRetryAsync(maxTrys - 1, i => timeToWait);
            var atuenticationresult = await retryPolity.ExecuteAsync
            (
                 async () => await apiCaller.GetToken(url, userNameInditex, passwordInditex)
            );

            return await Task.FromResult(atuenticationresult);
        }

        public static bool ClenerFiles(string filePath, IAppConfig appConfig)
        {
            var workDirectory = appConfig.GetValue<string>("DownloadServices.WorkDirectory", Directory.GetCurrentDirectory() + "/WorkDirectory");
            var historyDirectory = appConfig.GetValue<string>("DownloadServices.HistoryDirectory", Directory.GetCurrentDirectory() + "/HistoryDirectory");

            if (!Directory.Exists(workDirectory))
                throw new InvalidOperationException("Error: the work directory not found. ");


            if (File.Exists(filePath))
            {
                var destinyFilePath = Path.Combine(historyDirectory, Path.GetFileName(filePath));
                if (File.Exists(destinyFilePath)) File.Delete(destinyFilePath);

                if (File.Exists(filePath)) File.Delete(filePath);
            }

            return true;
        }
       
    }
}
