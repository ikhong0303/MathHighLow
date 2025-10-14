using UnityEngine;

namespace MathHighLow.UI
{
    internal static class TMPFontBootstrapper
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureHangulFonts()
        {
            TMPFontSupportUtility.EnsureHangulSupport();
        }
    }
}
