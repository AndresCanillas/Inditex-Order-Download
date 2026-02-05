using Google.Cloud.Translation.V2;
using Newtonsoft.Json;
using Service.Contracts;
using System;
using System.Collections.Generic;
using System.IO;

namespace ResourceGen
{
    class Program
    {
        private static ServiceFactory factory;
        private static Dictionary<string, int> foundKeys = new Dictionary<string, int>();
        private static TranslationClient translationClient;
        private static bool translate;
        private const string KeyFile = "C:\\Temp\\Print\\PROD\\GoogleCloud\\AccountKey.json";


        private static List<LanguageInfo> languages = new List<LanguageInfo>();

        static void Main(string[] args)
        {
            //while (!Debugger.IsAttached)
            //	Thread.Sleep(100);

            factory = new ServiceFactory();
            var log = factory.GetInstance<IAppLog>();
            try
            {
                log.InitializeLogFile("ResourceGen.log");
                log.LogMessage("Starting resource generation process...");
                translate = File.Exists(KeyFile);
                if (translate)
                {
                    Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", KeyFile);
                    using (translationClient = TranslationClient.Create())
                    {
                        Execute();
                    }
                }
                else
                {
                    Execute();
                }
            }
            catch (Exception ex)
            {
                log.LogException(ex);
            }
            finally
            {
                log.Terminate();
            }
        }

        private static void Execute()
        {
            var srcPath = CmdLineHelper.ExtractCmdArgument("srcPath", true);
            var outPath = CmdLineHelper.ExtractCmdArgument("outPath", true);
            LoadConfiguration(srcPath);
            foreach (var lang in languages)
            {
                lang.OutputFile = Path.Combine(outPath, $"Resources.{lang.Culture}.json");
                if (File.Exists(lang.OutputFile))
                {
                    var strings = JsonConvert.DeserializeObject<Strings>(File.ReadAllText(lang.OutputFile));
                    foreach (var elm in strings.Resources)
                        lang.index[elm.Key] = elm;
                }
            }

            ProcessFiles(srcPath, "*.cs");
            ProcessFiles(srcPath, "*.cshtml");
            RemoveUnusedEntries();

            foreach (var lang in languages)
            {
                var data = new Strings();
                data.Resources = new List<StringPair>();
                foreach (var key in lang.index.Keys)
                {
                    var keyValue = lang.index[key];
                    data.Resources.Add(new StringPair() { Key = key, Value = keyValue.Value, T = keyValue.T });
                }
                if (lang.DetectedChanges)
                {
                    File.WriteAllText(lang.OutputFile, JsonConvert.SerializeObject(data, Formatting.Indented));
                }
            }
        }

        private static void LoadConfiguration(string srcPath)
        {
            try
            {
                var appConfig = factory.GetInstance<IAppConfig>();
                var languagesConfigFile = Path.Combine(srcPath, "appsettings.json");
                appConfig.Load(languagesConfigFile);
                var config = appConfig.Bind<LanguageConfig>("Languages");
                foreach (var lang in config.Languages)
                {
                    languages.Add(new LanguageInfo() { Name = lang.Name, APICode = lang.APICode, Culture = lang.Culture });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("********** ResourceGen Error **********");
                Console.WriteLine("Error loading configuration file from source directory ({0}).", srcPath);
                Console.WriteLine("{0}\r\n{1}", ex.Message, ex.StackTrace);
                Environment.Exit(-1);
            }
        }

        private static void ProcessFiles(string path, string extension)
        {
            foreach (string file in Directory.EnumerateFiles(path, extension))
                SearchTokens(file);
            EnumerateDirectories(path, extension);
        }

        private static void EnumerateDirectories(string path, string extension)
        {
            foreach (string dir in Directory.EnumerateDirectories(path))
            {
                var dirName = Path.GetFileName(dir).ToLower();
                if ("obj,bin".IndexOf(dirName) >= 0 || dirName.StartsWith("."))
                    continue;
                ProcessFiles(dir, extension);
            }
        }


        private static void SearchTokens(string file)
        {
            var text = File.ReadAllText(file);
            int idx1, idx2 = 0;
            do
            {
                idx1 = text.IndexOf("g[\"", idx2);
                if (idx1 > 0)
                {
                    idx1 += 3;
                    idx2 = SearchTokenEnd(text, idx1);
                    if (idx2 > 0)
                    {
                        var token = text.Substring(idx1, idx2 - idx1);
                        ProcessMatch(token);
                    }
                }
            } while (idx1 > 0 && idx2 > 0);
        }

        private static int SearchTokenEnd(string text, int idx1)
        {
            int idx2;
            int eol = text.IndexOf("\r", idx1);
            bool found = false;
            do
            {
                idx2 = text.IndexOf("\"", idx1);
                if (idx2 > eol || idx2 < 0) return -1;
                if (text[idx2 - 1] == '\\')
                    idx1 = idx2 + 1;
                else
                    found = true;
            } while (idx2 < eol && !found);
            return idx2;
        }

        private static void ProcessMatch(string key)
        {
            if (!foundKeys.TryGetValue(key, out _))
            {
                foundKeys.Add(key, 0);
                foreach (var lang in languages)
                {
                    StringPair data;
                    var exists = lang.index.TryGetValue(key, out data);
                    if (!exists)
                    {
                        data = new StringPair() { Key = key, Value = key, T = false };
                        lang.index.Add(key, data);
                    }
                    if (data.Key == data.Value && !(data.T ?? false))
                    {
                        Translate(data, lang.APICode);
                        lang.DetectedChanges = true;
                    }
                }
            }
        }

        private static void Translate(StringPair data, string targetLanguage)
        {
            if (!translate)
            {
                data.Value = data.Key;
                data.T = false;
            }
            else
            {
                try
                {
                    var response = translationClient.TranslateText(data.Key, targetLanguage, "en");
                    data.Value = response.TranslatedText;
                    data.T = true;
                }
                catch (Exception)
                {
                    data.Value = data.Key;
                    data.T = false;
                }
            }
        }

        private static void RemoveUnusedEntries()
        {
            foreach (var lang in languages)
            {
                List<string> keysToRemove = new List<string>(100);
                foreach (string key in lang.index.Keys)
                {
                    if (!foundKeys.ContainsKey(key))
                        keysToRemove.Add(key);
                }
                foreach (string key in keysToRemove)
                    lang.index.Remove(key);
            }
        }
    }


    public class Strings
    {
        public List<StringPair> Resources;
    }

    public class StringPair
    {
        public string Key;
        public string Value;
        public bool? T;
    }

    public class LanguageInfo
    {
        public string Name;
        public string APICode;
        public string Culture;
        public string OutputFile;
        public Dictionary<string, StringPair> index = new Dictionary<string, StringPair>();
        public bool DetectedChanges;
    }



    public class LanguageConfig
    {
        public LanguageCfg Default { get; set; }
        public LanguageCfg[] Languages { get; set; }
    }

    public class LanguageCfg
    {
        public string Name { get; set; }
        public string APICode { get; set; }
        public string Culture { get; set; }
    }

}