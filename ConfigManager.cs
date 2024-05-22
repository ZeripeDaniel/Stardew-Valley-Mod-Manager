using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Newtonsoft.Json.Linq;

#pragma warning disable CS8600
#pragma warning disable CS8618
#pragma warning disable CS8601
#pragma warning disable CS8602
#pragma warning disable CS8603
#pragma warning disable CS8604
#pragma warning disable CS8605
#pragma warning disable CS8606
#pragma warning disable CS8607
#pragma warning disable CS8608
#pragma warning disable CS8609

namespace StardewValley_Mod_Manager
{
    public static class ConfigManager
    {
        public static readonly string ConfigFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "StardewValleyModManager",
            "config.xml");

        private static void EnsureConfigFilePath()
        {
            string directory = Path.GetDirectoryName(ConfigFilePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        public static string ReadSetting(string key)
        {
            if (!File.Exists(ConfigFilePath))
                return null;

            try
            {
                var doc = XDocument.Load(ConfigFilePath);
                var element = doc.Root.Element("Settings")?.Element(key);
                return element?.Value;
            }
            catch (Exception ex)
            {
                LogException(ex);
                return null;
            }
        }

        public static void LogException(Exception ex)
        {
            if (ex != null)
            {
                string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "error.log");
                File.AppendAllText(logPath, $"{DateTime.Now}: {ex}{Environment.NewLine}");
            }
        }

        public static void WriteSetting(string key, string value)
        {
            EnsureConfigFilePath();
            XDocument doc;

            if (!File.Exists(ConfigFilePath) || IsConfigFileEmpty())
            {
                doc = new XDocument(new XElement("Configuration", new XElement("Folders"), new XElement("Settings")));
                doc.Save(ConfigFilePath);
            }
            else
            {
                doc = XDocument.Load(ConfigFilePath);
            }

            var settingsElement = doc.Root.Element("Settings") ?? new XElement("Settings");
            if (doc.Root.Element("Settings") == null)
            {
                doc.Root.Add(settingsElement);
            }

            var element = settingsElement.Element(key);
            if (element != null)
            {
                element.Value = value;
            }
            else
            {
                settingsElement.Add(new XElement(key, value));
            }
            doc.Save(ConfigFilePath);
        }

        public static void WriteSetting(string key, int value)
        {
            WriteSetting(key, value.ToString());
        }

        private static bool IsConfigFileEmpty()
        {
            try
            {
                var doc = XDocument.Load(ConfigFilePath);
                return doc.Root == null || !doc.Root.HasElements;
            }
            catch
            {
                return true;
            }
        }

        public static void SaveFolder(string folderName, string folderPath)
        {
            EnsureConfigFilePath();
            var doc = XDocument.Load(ConfigFilePath);
            var folders = doc.Root.Element("Folders");
            var folderElement = folders.Elements("Folder").FirstOrDefault(f => f.Attribute("name").Value == folderName);
            if (folderElement == null)
            {
                folderElement = new XElement("Folder", new XAttribute("name", folderName), new XAttribute("path", folderPath));
                folders.Add(folderElement);
            }
            else
            {
                folderElement.SetAttributeValue("path", folderPath);
            }

            SaveInnerFolders(folderElement, folderPath);

            doc.Save(ConfigFilePath);
        }

        //private static void SaveInnerFolders(XElement parentElement, string folderPath)
        //{
        //    var directoryInfo = new DirectoryInfo(folderPath);
        //    var subDirectories = directoryInfo.GetDirectories();
        //    var files = directoryInfo.GetFiles();

        //    // 기존 Inner_Folder와 File 요소를 전부 삭제
        //    parentElement.Elements("Inner_Folder").Remove();
        //    parentElement.Elements("File").Remove();

        //    // 새롭게 Inner_Folder와 File 요소를 추가
        //    foreach (var dir in subDirectories)
        //    {
        //        var innerFolderElement = new XElement("Inner_Folder",
        //            new XAttribute("name", dir.Name),
        //            new XAttribute("path", dir.FullName));
        //        parentElement.Add(innerFolderElement);
        //        SaveInnerFolders(innerFolderElement, dir.FullName); // 재귀적으로 하위 폴더 저장

        //        // manifest.json 파일을 확인하고 필요한 값을 저장
        //        var manifestPath = Path.Combine(dir.FullName, "manifest.json");
        //        if (File.Exists(manifestPath))
        //        {
        //            var manifestJson = File.ReadAllText(manifestPath);
        //            var manifest = JObject.Parse(manifestJson);
        //            var version = manifest["Version"]?.ToString();
        //            var uniqueId = manifest["UniqueID"]?.ToString();

        //            if (version != null)
        //                innerFolderElement.SetAttributeValue("version", version);

        //            if (uniqueId != null)
        //                innerFolderElement.SetAttributeValue("UniqueID", uniqueId);
        //        }
        //    }

        //    foreach (var file in files)
        //    {
        //        var fileElement = new XElement("File",
        //            new XAttribute("name", file.Name),
        //            new XAttribute("size", file.Length));
        //        parentElement.Add(fileElement);
        //    }
        //}
        private static void SaveInnerFolders(XElement parentElement, string folderPath)
        {
            var directoryInfo = new DirectoryInfo(folderPath);
            var subDirectories = directoryInfo.GetDirectories();
            var files = directoryInfo.GetFiles();

            // 기존 Inner_Folder와 File 요소를 전부 삭제
            parentElement.Elements("Inner_Folder").Remove();
            parentElement.Elements("File").Remove();

            // 새롭게 Inner_Folder와 File 요소를 추가
            foreach (var dir in subDirectories)
            {
                var innerFolderElement = new XElement("Inner_Folder",
                    new XAttribute("name", dir.Name),
                    new XAttribute("path", dir.FullName));
                parentElement.Add(innerFolderElement);
                SaveInnerFolders(innerFolderElement, dir.FullName); // 재귀적으로 하위 폴더 저장

                // manifest.json 파일을 확인하고 필요한 값을 저장
                var manifestPath = Path.Combine(dir.FullName, "manifest.json");
                if (File.Exists(manifestPath))
                {
                    var manifestJson = File.ReadAllText(manifestPath);
                    var manifest = JObject.Parse(manifestJson);
                    var version = manifest["Version"]?.ToString();
                    var uniqueId = manifest["UniqueID"]?.ToString();

                    if (version != null)
                        innerFolderElement.SetAttributeValue("version", version);

                    if (uniqueId != null)
                        innerFolderElement.SetAttributeValue("UniqueID", uniqueId);

                    // UpdateKeys 값을 처리
                    var updateKeys = manifest["UpdateKeys"]?.ToObject<string[]>();
                    if (updateKeys != null)
                    {
                        var nexusKey = updateKeys.FirstOrDefault(key => key.StartsWith("Nexus:"));
                        if (nexusKey != null)
                        {
                            var modId = nexusKey.Split(':')[1];
                            innerFolderElement.SetAttributeValue("UpdateKey", modId);
                        }
                    }
                    else
                    {
                        // UpdateKeys 값이 없는 경우
                        innerFolderElement.SetAttributeValue("UpdateKey", ""); // 기본값 설정 또는 속성 추가하지 않음
                    }
                }
            }

            foreach (var file in files)
            {
                var fileElement = new XElement("File",
                    new XAttribute("name", file.Name),
                    new XAttribute("size", file.Length));
                parentElement.Add(fileElement);
            }
        }


        public static List<string> GetFolders()
        {
            if (File.Exists(ConfigFilePath))
            {
                var doc = XDocument.Load(ConfigFilePath);
                return doc.Descendants("Folder").Select(f => f.Attribute("name").Value).ToList();
            }
            return new List<string>();
        }

        public static XElement GetFolderElement(string folderName)
        {
            if (File.Exists(ConfigFilePath))
            {
                var doc = XDocument.Load(ConfigFilePath);
                return doc.Descendants("Folder").FirstOrDefault(f => f.Attribute("name")?.Value == folderName);
            }
            return null;
        }

        public static void ValidateConfig()
        {
            EnsureConfigFilePath();
            XDocument doc;

            if (!File.Exists(ConfigFilePath))
            {
                doc = new XDocument(new XElement("Configuration", new XElement("Folders"), new XElement("Settings")));
                doc.Save(ConfigFilePath);
            }
            else
            {
                try
                {
                    doc = XDocument.Load(ConfigFilePath);
                    if (doc.Root == null)
                    {
                        throw new XmlException("Root element is missing.");
                    }
                }
                catch (XmlException)
                {
                    doc = new XDocument(new XElement("Configuration", new XElement("Folders"), new XElement("Settings")));
                    doc.Save(ConfigFilePath);
                }
            }

            var folders = doc.Root.Element("Folders");
            if (folders == null)
            {
                folders = new XElement("Folders");
                doc.Root.Add(folders);
            }

            foreach (var folder in folders.Elements("Folder").ToList())
            {
                ValidateInnerFolders(folder);
            }

            doc.Save(ConfigFilePath);
        }

        private static void ValidateInnerFolders(XElement parentElement)
        {
            foreach (var innerFolder in parentElement.Elements("Inner_Folder").ToList())
            {
                string path = innerFolder.Attribute("path").Value;
                if (!Directory.Exists(path))
                {
                    innerFolder.Remove();
                }
                else
                {
                    ValidateInnerFolders(innerFolder); // 재귀적으로 하위 폴더 검사
                    foreach (var file in innerFolder.Elements("File").ToList())
                    {
                        string filePath = Path.Combine(path, file.Attribute("name").Value);
                        if (!File.Exists(filePath))
                        {
                            file.Remove();
                        }
                        else
                        {
                            long fileSize = new FileInfo(filePath).Length;
                            file.SetAttributeValue("size", fileSize);
                        }
                    }
                }
            }
        }

        public static void RefreshFolders()
        {
            EnsureConfigFilePath();
            XDocument doc;

            if (!File.Exists(ConfigFilePath))
            {
                doc = new XDocument(new XElement("Configuration", new XElement("Folders"), new XElement("Settings")));
                doc.Save(ConfigFilePath);
            }
            else
            {
                try
                {
                    doc = XDocument.Load(ConfigFilePath);
                    if (doc.Root == null)
                    {
                        throw new XmlException("Root element is missing.");
                    }
                }
                catch (XmlException)
                {
                    doc = new XDocument(new XElement("Configuration", new XElement("Folders"), new XElement("Settings")));
                    doc.Save(ConfigFilePath);
                }
            }

            var folders = doc.Root.Element("Folders");
            if (folders == null)
            {
                folders = new XElement("Folders");
                doc.Root.Add(folders);
            }

            if (folders.HasElements)
            {
                foreach (var folder in folders.Elements("Folder").ToList())
                {
                    SaveInnerFolders(folder, folder.Attribute("path").Value);
                }
            }

            doc.Save(ConfigFilePath);
        }
    }
}
