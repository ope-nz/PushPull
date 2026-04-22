using System;
using System.Collections.Generic;
using System.IO;
using System.Web.Script.Serialization;

namespace GFD
{
    public class GfdProject
    {
        public string Name { get; set; }
        public string LocalFolder { get; set; }
        public string Owner { get; set; }
        public string Repo { get; set; }
        public string Branch { get; set; }
        public List<string> IgnorePatterns { get; set; }

        public GfdProject()
        {
            Branch = "main";
            IgnorePatterns = new List<string> { "*.exe", "*.dll", "*.pdb", "bin/", "obj/", ".vs/" };
        }

        public override string ToString() { return Name ?? (Owner + "/" + Repo); }
    }

    public class GfdConfig
    {
        public string Token { get; set; }
        public List<GfdProject> Projects { get; set; }

        public GfdConfig() { Projects = new List<GfdProject>(); }
    }

    public static class ConfigManager
    {
        static string ConfigPath
        {
            get
            {
                string dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GFD");
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                return Path.Combine(dir, "config.json");
            }
        }

        public static GfdConfig Load()
        {
            if (!File.Exists(ConfigPath)) return new GfdConfig();
            try
            {
                string json = File.ReadAllText(ConfigPath);
                var ser = new JavaScriptSerializer();
                return ser.Deserialize<GfdConfig>(json) ?? new GfdConfig();
            }
            catch { return new GfdConfig(); }
        }

        public static void Save(GfdConfig config)
        {
            var ser = new JavaScriptSerializer();
            File.WriteAllText(ConfigPath, ser.Serialize(config));
        }
    }
}
