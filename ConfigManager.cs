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

        private static readonly string FirstRunFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "StardewValleyModManager",
            "HelloFriend");
        public static bool IsFirstRun()
        {
            return !File.Exists(FirstRunFilePath);
        }

        public static void SetFirstRun()
        {
            EnsureFirstRunFilePath();
            File.Create(FirstRunFilePath).Close();
        }
        private static void EnsureFirstRunFilePath()
        {
            string directory = Path.GetDirectoryName(FirstRunFilePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        public static void CheckAndBackupModsFolder(string modsFolderPath)
        {
            if (IsFirstRun())
            {
                // 최초 실행 시 Mods 폴더를 ModsBackup으로 이름 변경
                string backupFolderPath = Path.Combine(Path.GetDirectoryName(modsFolderPath), "ModsBackup");

                if (Directory.Exists(modsFolderPath))
                {
                    Directory.Move(modsFolderPath, backupFolderPath);
                }

                // 최초 실행이 완료되었음을 표시하는 파일 생성
                SetFirstRun();
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

        public static void SaveFolder(string folderName, string folderPath, XElement updatedInnerFolder = null)
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

            if (updatedInnerFolder != null)
            {
                var existingInnerFolder = folderElement.Elements("Inner_Folder").FirstOrDefault(f => f.Attribute("name").Value == updatedInnerFolder.Attribute("name").Value);
                if (existingInnerFolder != null)
                {
                    // 기존 LatestVersion 값을 유지하며 업데이트
                    var latestVersion = existingInnerFolder.Attribute("LatestVersion")?.Value;
                    existingInnerFolder.ReplaceWith(updatedInnerFolder);
                    if (latestVersion != null)
                    {
                        updatedInnerFolder.SetAttributeValue("LatestVersion", latestVersion);
                    }
                }
                else
                {
                    folderElement.Add(updatedInnerFolder);
                }
            }
            else
            {
                SaveInnerFolders(folderElement, folderPath);
            }

            doc.Save(ConfigFilePath);
        }
        private static void SaveInnerFolders(XElement parentElement, string folderPath, bool isTopLevel = true)
        {
            var directoryInfo = new DirectoryInfo(folderPath);
            var subDirectories = directoryInfo.GetDirectories();
            var files = directoryInfo.GetFiles();

            // 기존 Inner_Folder 요소들을 가져옴
            var existingInnerFolders = parentElement.Elements("Inner_Folder").ToDictionary(f => f.Attribute("name").Value);
            var existingFiles = parentElement.Elements("File").ToDictionary(f => f.Attribute("name").Value);

            // 새롭게 Inner_Folder와 File 요소를 추가 또는 업데이트
            foreach (var dir in subDirectories)
            {
                XElement innerFolderElement;
                if (existingInnerFolders.TryGetValue(dir.Name, out innerFolderElement))
                {
                    // 기존 요소 업데이트
                    innerFolderElement.SetAttributeValue("path", dir.FullName);

                    // manifest.json 파일을 확인하고 필요한 값을 업데이트
                    var manifestPath = Path.Combine(dir.FullName, "manifest.json");
                    if (File.Exists(manifestPath))
                    {
                        var manifestJson = File.ReadAllText(manifestPath);
                        var manifest = JObject.Parse(manifestJson);
                        var version = manifest["Version"]?.ToString();
                        var uniqueId = manifest["UniqueID"]?.ToString();
                        var updateKeys = manifest["UpdateKeys"]?.FirstOrDefault(k => k.ToString().StartsWith("Nexus:"))?.ToString().Split(':').Last();

                        if (version != null)
                            innerFolderElement.SetAttributeValue("version", version);

                        if (uniqueId != null)
                            innerFolderElement.SetAttributeValue("UniqueID", uniqueId);

                        if (updateKeys != null)
                            innerFolderElement.SetAttributeValue("UpdateKey", updateKeys);
                    }
                }
                else
                {
                    // 새 요소 추가
                    innerFolderElement = new XElement("Inner_Folder",
                        new XAttribute("name", dir.Name),
                        new XAttribute("path", dir.FullName));

                    if (isTopLevel)
                    {
                        innerFolderElement.Add(new XAttribute("IsChecked", "true"));
                    }

                    // manifest.json 파일을 확인하고 필요한 값을 저장
                    var manifestPath = Path.Combine(dir.FullName, "manifest.json");
                    if (File.Exists(manifestPath))
                    {
                        var manifestJson = File.ReadAllText(manifestPath);
                        var manifest = JObject.Parse(manifestJson);
                        var version = manifest["Version"]?.ToString();
                        var uniqueId = manifest["UniqueID"]?.ToString();
                        var updateKeys = manifest["UpdateKeys"]?.FirstOrDefault(k => k.ToString().StartsWith("Nexus:"))?.ToString().Split(':').Last();

                        if (version != null)
                            innerFolderElement.SetAttributeValue("version", version);

                        if (uniqueId != null)
                            innerFolderElement.SetAttributeValue("UniqueID", uniqueId);

                        if (updateKeys != null)
                            innerFolderElement.SetAttributeValue("UpdateKey", updateKeys);
                    }

                    parentElement.Add(innerFolderElement);
                }

                // 재귀적으로 하위 폴더 저장
                SaveInnerFolders(innerFolderElement, dir.FullName, false);
            }

            foreach (var file in files)
            {
                XElement fileElement;
                if (existingFiles.TryGetValue(file.Name, out fileElement))
                {
                    // 기존 요소 업데이트
                    fileElement.SetAttributeValue("path", file.FullName);
                    fileElement.SetAttributeValue("size", file.Length);
                }
                else
                {
                    // 새 요소 추가
                    fileElement = new XElement("File",
                        new XAttribute("name", file.Name),
                        new XAttribute("path", file.FullName),
                        new XAttribute("size", file.Length));
                    parentElement.Add(fileElement);
                }
            }

            // 기존 요소들 중 업데이트되지 않은 요소 제거
            var newInnerFolderNames = subDirectories.Select(d => d.Name).ToHashSet();
            var newFileNames = files.Select(f => f.Name).ToHashSet();

            foreach (var existingInnerFolder in existingInnerFolders.Values)
            {
                if (!newInnerFolderNames.Contains(existingInnerFolder.Attribute("name").Value))
                {
                    existingInnerFolder.Remove();
                }
            }

            foreach (var existingFile in existingFiles.Values)
            {
                if (!newFileNames.Contains(existingFile.Attribute("name").Value))
                {
                    existingFile.Remove();
                }
            }
        }





        public static List<XElement> GetAllFolders()
        {
            if (File.Exists(ConfigFilePath))
            {
                var doc = XDocument.Load(ConfigFilePath);
                return doc.Descendants("Folder").ToList();
            }
            return new List<XElement>();
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
        public static void SaveConfig(XDocument doc)
        {
            doc.Save(ConfigFilePath);
            
        }
        public static void UpdateIsChecked(string folderName, string itemName, bool isChecked)
        {
            EnsureConfigFilePath();
            var doc = XDocument.Load(ConfigFilePath);
            var folderElement = doc.Descendants("Folder")
                                   .FirstOrDefault(f => f.Attribute("name").Value == folderName);

            if (folderElement != null)
            {
                var itemElement = folderElement.Descendants("Inner_Folder")
                                               .FirstOrDefault(i => i.Attribute("name").Value == itemName) ??
                                  folderElement.Descendants("File")
                                               .FirstOrDefault(i => i.Attribute("name").Value == itemName);

                if (itemElement != null)
                {
                    itemElement.SetAttributeValue("IsChecked", isChecked.ToString());
                    doc.Save(ConfigFilePath);
                }
            }
        }
        public static List<XElement> GetAllInnerFolders()
        {
            EnsureConfigFilePath();
            var doc = XDocument.Load(ConfigFilePath);
            var innerFolders = doc.Descendants("Inner_Folder").ToList();
            return innerFolders;
        }

    }
}
