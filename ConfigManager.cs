using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace StardewValley_Mod_Manager
{
    public static class ConfigManager
    {
        public static readonly string ConfigFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "StardewValleyModManager",
            "config.xml");

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
            catch
            {
                return null;
            }
        }

        public static void WriteSetting(string key, string value)
        {
            EnsureConfigFilePath();
            XDocument doc;

            if (!File.Exists(ConfigFilePath) || IsConfigFileEmpty())
            {
                // config.xml 파일이 없거나 비어 있으면 기본 구조 생성
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

        private static void SaveInnerFolders(XElement parentElement, string folderPath)
        {
            foreach (var dir in Directory.GetDirectories(folderPath))
            {
                var innerFolderElement = new XElement("Inner_Folder", new XAttribute("name", Path.GetFileName(dir)), new XAttribute("path", dir));
                parentElement.Add(innerFolderElement);
                SaveInnerFolders(innerFolderElement, dir); // 재귀적으로 하위 폴더 저장
            }

            foreach (var file in Directory.GetFiles(folderPath))
            {
                long fileSize = new FileInfo(file).Length;
                var fileElement = new XElement("File", new XAttribute("name", Path.GetFileName(file)), new XAttribute("size", fileSize));
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
                // config.xml 파일이 없으면 기본 구조 생성
                doc = new XDocument(new XElement("Configuration", new XElement("Folders"), new XElement("Settings")));
                doc.Save(ConfigFilePath);
            }
            else
            {
                doc = XDocument.Load(ConfigFilePath);
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
        private static void EnsureConfigFilePath()
        {
            string directory = Path.GetDirectoryName(ConfigFilePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }
    }
}
