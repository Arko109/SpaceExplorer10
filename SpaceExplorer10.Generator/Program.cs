using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace SpaceExplorer10.Generator
{
    class Program
    {
        static void Main(string[] args)
        {
            var path = Directory.GetCurrentDirectory();
            var xmlName = "result.xml";
            if (args.Length > 0) path = args[0];
            if (args.Length > 1) xmlName = args[1];

            Console.WriteLine("Reading contents...");
            var startTime = DateTime.Now;
            var result = GetEntry(path);
            result.ScanEnd = DateTime.Now.ToString("O");
            result.ScanStart = startTime.ToString("O");
            result.BasePath = path;

            Console.WriteLine("Generating XML...");
            using var file = File.Create(xmlName);
            var serializer = new XmlSerializer(typeof(Entry));
            serializer.Serialize(file, result);

            Console.WriteLine("Done");
        }

        static Entry GetEntry(string path)
        {
            var fileInfo = new FileInfo(path);
            var entry = new Entry { Name = fileInfo.Name };

            if (fileInfo.Attributes.HasFlag(FileAttributes.ReparsePoint)) entry.IsSymbolicLink = "true";
            else if (Directory.Exists(path))
            {
                try
                {
                    entry.Children = new List<Entry>();
                    foreach (var child in Directory.EnumerateFileSystemEntries(path))
                    {
                        var childEntry = GetEntry(child);
                        entry.Children.Add(childEntry);
                        entry.Size += childEntry.Size;
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    entry.WasAccessDenied = "true";
                    Console.Error.WriteLine($"Access denied to path {path}");
                }
            }
            else if (File.Exists(path)) entry.Size = fileInfo.Length;

            return entry;
        }
    }

    [XmlRoot("RootEntry")]
    public class Entry
    {
        [XmlAttribute]
        public string Name { get; set; }

        /// <summary>
        /// Size in bytes.
        /// </summary>
        [XmlAttribute]
        public long Size { get; set; }

        [XmlAttribute]
        public string WasAccessDenied { get; set; }

        [XmlAttribute]
        public string BasePath { get; set; }

        [XmlAttribute]
        public string IsSymbolicLink { get; set; }

        [XmlAttribute]
        public string ScanStart { get; set; }

        [XmlAttribute]
        public string ScanEnd { get; set; }

        public List<Entry> Children { get; set; }

        public override string ToString()
        {
            return $"{Name} - {Size}B";
        }
    }
}
