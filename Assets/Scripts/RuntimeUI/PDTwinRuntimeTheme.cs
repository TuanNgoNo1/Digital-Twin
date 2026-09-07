using UnityEngine;

namespace PDTwin.RuntimeUI
{
    public enum RuntimeHealth
    {
        Unknown,
        Online,
        Warning,
        Offline
    }

    public static class PDTwinRuntimeTheme
    {
        public static readonly Color PtitRed = Hex("#D71920");
        public static readonly Color PtitRedDark = Hex("#A90F16");
        public static readonly Color PtitRedSoft = Hex("#FFF1F2");
        public static readonly Color Ink = Hex("#172033");
        public static readonly Color Muted = Hex("#667085");
        public static readonly Color Border = Hex("#E4E7EC");
        public static readonly Color Surface = Hex("#FFFFFF");
        public static readonly Color SurfaceMuted = Hex("#F8FAFC");
        public static readonly Color Success = Hex("#12B76A");
        public static readonly Color SuccessSoft = Hex("#ECFDF3");
        public static readonly Color Warning = Hex("#F79009");
        public static readonly Color WarningSoft = Hex("#FFFAEB");
        public static readonly Color Danger = Hex("#F04438");
        public static readonly Color DangerSoft = Hex("#FEF3F2");
        public static readonly Color ProgressTrack = Hex("#F4C9CC");
        public static readonly Color Disabled = Hex("#98A2B3");
        public static readonly Color Scrim = new Color(0.05f, 0.08f, 0.14f, 0.54f);

        public static Color HealthColor(RuntimeHealth health)
        {
            switch (health)
            {
                case RuntimeHealth.Online: return Success;
                case RuntimeHealth.Warning: return Warning;
                case RuntimeHealth.Offline: return Danger;
                default: return Hex("#98A2B3");
            }
        }

        public static Color HealthSurface(RuntimeHealth health)
        {
            switch (health)
            {
                case RuntimeHealth.Online: return SuccessSoft;
                case RuntimeHealth.Warning: return WarningSoft;
                case RuntimeHealth.Offline: return DangerSoft;
                default: return SurfaceMuted;
            }
        }

        private static Color Hex(string value)
        {
            return ColorUtility.TryParseHtmlString(value, out Color color) ? color : Color.white;
        }
    }

    public sealed class PointCriterionView
    {
        public string title;
        public string evidence;
        public string state;
        public float earned;
        public float maximum;
        public RuntimeHealth health;
        public bool interactive;
        public bool selected;
        public int testIndex = -1;
    }
}
