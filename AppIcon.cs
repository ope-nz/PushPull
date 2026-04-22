using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace GFD
{
    static class AppIcon
    {
        static Icon _icon;

        static Icon Load()
        {
            if (_icon != null) return _icon;
            _icon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);
            return _icon;
        }

        public static void Apply(Form form)
        {
            var icon = Load();
            if (icon != null) form.Icon = icon;
        }
    }
}
