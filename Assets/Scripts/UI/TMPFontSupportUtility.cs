using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace MathHighLow.UI
{
    internal static class TMPFontSupportUtility
    {
        private const char SampleHangul = '전';

        private static readonly string[] OsFontCandidates =
        {
            "Noto Sans CJK KR",
            "Noto Sans KR",
            "NanumGothic",
            "Nanum Gothic",
            "Malgun Gothic",
            "MalgunGothic",
            "Apple SD Gothic Neo",
            "AppleGothic",
            "Source Han Sans KR",
            "Source Han Sans K",
            "Droid Sans Fallback"
        };

        private static readonly uint[] HangulUnicodeRange = BuildHangulUnicodeRange();

        private static TMP_FontAsset hangulFallbackFont;
        public static void EnsureHangulSupport()
        {
            var baseFont = TMP_Settings.defaultFontAsset;

            if (baseFont == null)
            {
                baseFont = CreateDefaultFontAsset();
                if (baseFont != null)
                {
                    TMP_Settings.defaultFontAsset = baseFont;
                }
            }

            EnsureHangulFallback(baseFont);
        }

        private static TMP_FontAsset CreateDefaultFontAsset()
        {
            var legacyFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (legacyFont == null)
            {
                legacyFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            if (legacyFont == null)
            {
                return null;
            }

            var defaultFont = TMP_FontAsset.CreateFontAsset(
                legacyFont,
                90,
                9,
                GlyphRenderMode.SDFAA,
                1024,
                1024,
                AtlasPopulationMode.Dynamic,
                true);

            if (defaultFont != null)
            {
                defaultFont.name = $"{legacyFont.name} TMP Font";
            }

            return defaultFont;
        }

        private static void EnsureHangulFallback(TMP_FontAsset baseFont)
        {
            if (baseFont == null)
            {
                return;
            }

            if (HasHangulGlyph(baseFont))
            {
                return;
            }

            if (TryEnsureExistingFallback(baseFont))
            {
                return;
            }

            if (hangulFallbackFont == null)
            {
                hangulFallbackFont = TryCreateHangulFallbackFont();
            }

            if (hangulFallbackFont != null)
            {
                AttachFallback(baseFont, hangulFallbackFont);
            }
            else
            {
                Debug.LogWarning(
                    "한국어 글리프를 지원하는 폰트를 찾지 못했습니다. " +
                    "운영체제에 한국어 폰트가 설치돼 있는지 확인하고, 필요하면 프로젝트에 Noto Sans KR 같은 폰트를 추가해 주세요.");
            }
        }

        private static bool TryEnsureExistingFallback(TMP_FontAsset baseFont)
        {
            if (baseFont.fallbackFontAssetTable != null)
            {
                foreach (var fallback in baseFont.fallbackFontAssetTable)
                {
                    if (AttachFallback(baseFont, fallback))
                    {
                        return true;
                    }
                }
            }

            var liberationFallback = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF - Fallback");
            if (AttachFallback(baseFont, liberationFallback))
            {
                return true;
            }

            var globalFallbacks = TMP_Settings.fallbackFontAssets;
            if (globalFallbacks != null)
            {
                foreach (var fallback in globalFallbacks)
                {
                    if (AttachFallback(baseFont, fallback))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static TMP_FontAsset TryCreateHangulFallbackFont()
        {
            foreach (var fontName in OsFontCandidates)
            {
                try
                {
                    var osFont = Font.CreateDynamicFontFromOSFont(fontName, 90);
                    if (osFont == null)
                    {
                        continue;
                    }

                    var fallbackAsset = TMP_FontAsset.CreateFontAsset(
                        osFont,
                        90,
                        9,
                        GlyphRenderMode.SDFAA,
                        1024,
                        1024,
                        AtlasPopulationMode.Dynamic,
                        true);

                    if (fallbackAsset == null)
                    {
                        continue;
                    }

                    fallbackAsset.name = $"{fontName} TMP Dynamic";
                    fallbackAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
                    fallbackAsset.isMultiAtlasTexturesEnabled = true;

                    if (!TryAddHangulCharacters(fallbackAsset))
                    {
                        continue;
                    }

                    RegisterGlobalFallback(fallbackAsset);
                    return fallbackAsset;
                }
                catch (Exception)
                {
                    // OS 폰트를 찾지 못한 경우 무시하고 다음 후보를 시도합니다.
                }
            }

            return null;
        }

        private static bool AttachFallback(TMP_FontAsset baseFont, TMP_FontAsset fallback)
        {
            if (baseFont == null || fallback == null)
            {
                return false;
            }

            if (!HasHangulGlyph(fallback) && !TryAddHangulCharacters(fallback))
            {
                return false;
            }

            if (baseFont.fallbackFontAssetTable == null)
            {
                baseFont.fallbackFontAssetTable = new List<TMP_FontAsset>();
            }

            if (!baseFont.fallbackFontAssetTable.Contains(fallback))
            {
                baseFont.fallbackFontAssetTable.Add(fallback);
            }

            RegisterGlobalFallback(fallback);
            return true;
        }

        private static bool TryAddHangulCharacters(TMP_FontAsset fontAsset)
        {
            if (fontAsset == null)
            {
                return false;
            }

            fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            fontAsset.isMultiAtlasTexturesEnabled = true;

            fontAsset.TryAddCharacters(HangulUnicodeRange, out var missingCharacters);
            return missingCharacters == null || missingCharacters.Length == 0;
        }

        private static bool HasHangulGlyph(TMP_FontAsset fontAsset)
        {
            return fontAsset != null && fontAsset.HasCharacter(SampleHangul);
        }

        private static void RegisterGlobalFallback(TMP_FontAsset fallback)
        {
            if (fallback == null)
            {
                return;
            }

            var globalFallbacks = TMP_Settings.fallbackFontAssets;
            if (globalFallbacks == null)
            {
                globalFallbacks = new List<TMP_FontAsset>();
                TMP_Settings.fallbackFontAssets = globalFallbacks;
            }

            if (!globalFallbacks.Contains(fallback))
            {
                globalFallbacks.Add(fallback);
            }
        }

        private static uint[] BuildHangulUnicodeRange()
        {
            const uint start = 0xAC00;
            const uint end = 0xD7A3;
            var length = end - start + 1;
            var characters = new uint[length];

            for (var i = 0; i < length; i++)
            {
                characters[i] = start + (uint)i;
            }

            return characters;
        }
    }
}
