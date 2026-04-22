using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace GFD
{
    static class AppIcon
    {
        static System.Drawing.Icon _icon;

        static System.Drawing.Icon Load()
        {
            if (_icon != null) return _icon;
            string path = Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                "icon.ico");
            if (File.Exists(path))
                _icon = new System.Drawing.Icon(path);
            return _icon;
        }

        public static void Apply(Form form)
        {
            var icon = Load();
            if (icon != null) form.Icon = icon;
        }
    }
}
