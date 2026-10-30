using BepInEx;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace RandomLoadingScreens
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    internal class RandomLoadingScreens : BaseUnityPlugin
    {
        public const string PluginGUID = "ewigl.RandomLoadingScreens";
        public const string PluginName = "Random Loading Screens";
        public const string PluginVersion = "0.0.4";
        private readonly Harmony _harmony = new(PluginGUID);
        public static List<string> loadingScreens = new();
        private static Sprite loadingSprite;

        private void Awake()
        {
            LoadCustomLoadingScreens();
            _harmony.PatchAll();
        }

        private void OnDestroy()
        {
            CleanupLoadingSprite();
            _harmony.UnpatchSelf();
        }

        private static string GetPluginDirectory()
        {
            return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        }

        private static void LoadCustomLoadingScreens()
        {
            loadingScreens.Clear();
            string path = Path.Combine(GetPluginDirectory(), "RandomLoadingScreens");

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                return;
            }

            HashSet<string> validExtensions = new(StringComparer.OrdinalIgnoreCase) { ".png", ".jpg", ".jpeg" };

            try
            {
                loadingScreens = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories)
                    .Where(file => validExtensions.Contains(Path.GetExtension(file)))
                    .ToList();

                Debug.Log($"[RandomLoadingScreens] Loaded {loadingScreens.Count} images.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RandomLoadingScreens] Load folder error: {ex.Message}");
            }
        }

        private static void CleanupLoadingSprite()
        {
            if (loadingSprite != null)
            {
                if (loadingSprite.texture != null)
                {
                    Destroy(loadingSprite.texture);
                }
                Destroy(loadingSprite);
                loadingSprite = null;
            }
        }

        private static Sprite GetRandomLoadingScreen()
        {
            if (loadingScreens.Count == 0)
                return null;

            CleanupLoadingSprite();

            int attempts = 3;
            while (attempts > 0)
            {
                attempts--;
                try
                {
                    string randomFile = loadingScreens[UnityEngine.Random.Range(0, loadingScreens.Count)];
                    if (!File.Exists(randomFile)) continue;

                    byte[] imageData = File.ReadAllBytes(randomFile);

                    Texture2D tex = new(2, 2, TextureFormat.RGBA32, false);
                    if (tex.LoadImage(imageData))
                    {
                        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100.0f);
                    }

                    Destroy(tex);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[RandomLoadingScreens] Read image error: {ex.Message}");
                }
            }

            return null;
        }

        [HarmonyPatch(typeof(FejdStartup), "LoadMainScene")]
        public static class LoadMainScene_Patch
        {
            public static void Postfix(FejdStartup __instance)
            {
                loadingSprite = GetRandomLoadingScreen();
            }
        }

        [HarmonyPatch(typeof(Hud), "UpdateBlackScreen")]
        public static class UpdateBlackScreen_Patch
        {
            public static void Prefix(Hud __instance, bool ___m_haveSetupLoadScreen, ref bool __state)
            {
                __state = !___m_haveSetupLoadScreen;
            }
            public static void Postfix(Hud __instance, bool ___m_haveSetupLoadScreen, ref bool __state)
            {
                if (__state && ___m_haveSetupLoadScreen)
                {
                    __instance.m_loadingImage.sprite = loadingSprite;
                    __instance.m_loadingImage.color = Color.white;
                }
            }
        }
    }
}
