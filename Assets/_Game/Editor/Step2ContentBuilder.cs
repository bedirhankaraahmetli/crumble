using System;
using System.IO;
using System.Text;
using Crumble.Data;
using Crumble.Gameplay;
using Crumble.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Crumble.EditorTools
{
    /// <summary>
    /// One-shot, idempotent Step 2 setup: generates placeholder tablet sprites
    /// (4 Tier-1 materials × 5 crack states), creates their TabletMaterialSO assets,
    /// and wires the Main scene (_Bootstrap managers, Tablet object, portrait HUD).
    /// Placeholder art is deterministic pixel-noise + cracks — replaced by real
    /// 16-bit art later without touching any code (only the SO sprite fields).
    /// Writes a summary to Temp/crumble_step2_build.txt for external tooling.
    /// </summary>
    public static class Step2ContentBuilder
    {
        private const string ResultsPath = "Temp/crumble_step2_build.txt";
        private const string ArtDir = "Assets/_Game/Art/Placeholders/Tablets";
        private const string DataDir = "Assets/_Game/Data/Tablets";
        private const int TexSize = 128;
        private const float PixelsPerUnit = 32f;

        private struct MaterialDef
        {
            public string Id;
            public string Name;
            public Color Base;
            public double BaseHp;
            public double Reward;
        }

        private static readonly MaterialDef[] Defs =
        {
            new MaterialDef { Id = "tablet_dried_mud", Name = "Dried Mud", Base = new Color(0.55f, 0.42f, 0.28f), BaseHp = 10, Reward = 5 },
            new MaterialDef { Id = "tablet_clay", Name = "Clay", Base = new Color(0.71f, 0.44f, 0.32f), BaseHp = 120, Reward = 60 },
            new MaterialDef { Id = "tablet_limestone", Name = "Limestone", Base = new Color(0.80f, 0.78f, 0.68f), BaseHp = 1500, Reward = 700 },
            new MaterialDef { Id = "tablet_sandstone", Name = "Sandstone", Base = new Color(0.85f, 0.66f, 0.38f), BaseHp = 18000, Reward = 8000 },
        };

        [MenuItem("Crumble/Build Step 2 Content And Scene")]
        public static void Build()
        {
            var log = new StringBuilder();
            try
            {
                GenerateSprites(log);
                var materials = CreateMaterialAssets(log);
                WireScene(materials, log);
                log.AppendLine("RESULT: OK");
            }
            catch (Exception e)
            {
                log.AppendLine("RESULT: ERROR " + e);
                Debug.LogException(e);
            }

            File.WriteAllText(ResultsPath, log.ToString());
            Debug.Log("[Step2ContentBuilder]\n" + log);
        }

        // ---------- sprites ----------

        private static void GenerateSprites(StringBuilder log)
        {
            Directory.CreateDirectory(ArtDir);
            for (var m = 0; m < Defs.Length; m++)
            {
                for (var state = 0; state < 5; state++)
                {
                    var path = SpritePath(m, state);
                    File.WriteAllBytes(path, RenderTablet(Defs[m].Base, m, state).EncodeToPNG());
                }
            }

            AssetDatabase.Refresh();

            for (var m = 0; m < Defs.Length; m++)
            {
                for (var state = 0; state < 5; state++)
                {
                    var importer = (TextureImporter)AssetImporter.GetAtPath(SpritePath(m, state));
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.spritePixelsPerUnit = PixelsPerUnit;
                    importer.filterMode = FilterMode.Point;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.mipmapEnabled = false;
                    importer.SaveAndReimport();
                }
            }

            log.AppendLine($"Sprites: {Defs.Length * 5} generated in {ArtDir}");
        }

        private static string SpritePath(int materialIndex, int state)
        {
            return $"{ArtDir}/{Defs[materialIndex].Id}_state{state}.png";
        }

        private static Texture2D RenderTablet(Color baseColor, int materialIndex, int state)
        {
            var rng = new System.Random(materialIndex * 100 + state * 7 + 1);
            var tex = new Texture2D(TexSize, TexSize, TextureFormat.RGBA32, false);
            var clear = new Color(0, 0, 0, 0);
            var border = baseColor * 0.55f;
            border.a = 1f;

            // Shattered state is darker overall so it reads instantly.
            var fill = state == 4 ? baseColor * 0.7f : baseColor;
            fill.a = 1f;

            const int inset = 10;
            const int corner = 14;
            for (var y = 0; y < TexSize; y++)
            {
                for (var x = 0; x < TexSize; x++)
                {
                    var inside = x >= inset && x < TexSize - inset && y >= inset && y < TexSize - inset;
                    if (inside)
                    {
                        // cut the corners for a stone-slab silhouette
                        var cx = Mathf.Min(x - inset, TexSize - inset - 1 - x);
                        var cy = Mathf.Min(y - inset, TexSize - inset - 1 - y);
                        if (cx + cy < corner)
                        {
                            inside = false;
                        }
                    }

                    if (!inside)
                    {
                        tex.SetPixel(x, y, clear);
                        continue;
                    }

                    var edge = x < inset + 3 || x >= TexSize - inset - 3 || y < inset + 3 || y >= TexSize - inset - 3;
                    var noise = 1f + ((float)rng.NextDouble() - 0.5f) * 0.16f;
                    var c = edge ? border : fill * noise;
                    c.a = 1f;
                    tex.SetPixel(x, y, c);
                }
            }

            // Cracks: more and longer as the state advances.
            var crackColor = baseColor * 0.3f;
            crackColor.a = 1f;
            var crackCount = state == 4 ? 18 : state * 4;
            for (var i = 0; i < crackCount; i++)
            {
                var x = rng.Next(inset + 8, TexSize - inset - 8);
                var y = rng.Next(inset + 8, TexSize - inset - 8);
                var steps = rng.Next(20, 55);
                var dx = rng.Next(-1, 2);
                var dy = rng.Next(-1, 2);
                if (dx == 0 && dy == 0)
                {
                    dy = -1;
                }

                for (var s = 0; s < steps; s++)
                {
                    if (rng.NextDouble() < 0.3)
                    {
                        dx = Mathf.Clamp(dx + rng.Next(-1, 2), -1, 1);
                    }

                    if (rng.NextDouble() < 0.3)
                    {
                        dy = Mathf.Clamp(dy + rng.Next(-1, 2), -1, 1);
                    }

                    x += dx;
                    y += dy;
                    if (x <= inset || x >= TexSize - inset || y <= inset || y >= TexSize - inset)
                    {
                        break;
                    }

                    if (tex.GetPixel(x, y).a > 0)
                    {
                        tex.SetPixel(x, y, crackColor);
                        if (x + 1 < TexSize - inset && tex.GetPixel(x + 1, y).a > 0)
                        {
                            tex.SetPixel(x + 1, y, crackColor);
                        }
                    }
                }
            }

            tex.Apply();
            return tex;
        }

        // ---------- ScriptableObject assets ----------

        private static TabletMaterialSO[] CreateMaterialAssets(StringBuilder log)
        {
            Directory.CreateDirectory(DataDir);
            var result = new TabletMaterialSO[Defs.Length];

            for (var m = 0; m < Defs.Length; m++)
            {
                var def = Defs[m];
                var assetPath = $"{DataDir}/{def.Id}.asset";
                var so = AssetDatabase.LoadAssetAtPath<TabletMaterialSO>(assetPath);
                var isNew = so == null;
                if (isNew)
                {
                    so = ScriptableObject.CreateInstance<TabletMaterialSO>();
                }

                so.Id = def.Id;
                so.DisplayName = def.Name;
                so.Tier = TabletTier.Surface;
                so.OrderIndex = m;
                so.BaseHp = def.BaseHp;
                so.DifficultyFactor = 1.5;
                so.BreakReward = def.Reward;
                so.RewardGrowthFactor = 1.4;
                so.MilestoneHpMultiplier = 2.0;
                so.MilestoneRewardMultiplier = 3.0;
                so.BaseColor = def.Base;
                so.CrackStates = new Sprite[5];
                for (var state = 0; state < 5; state++)
                {
                    so.CrackStates[state] = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath(m, state));
                }

                if (isNew)
                {
                    AssetDatabase.CreateAsset(so, assetPath);
                }
                else
                {
                    EditorUtility.SetDirty(so);
                }

                result[m] = so;
            }

            AssetDatabase.SaveAssets();
            log.AppendLine($"Materials: {Defs.Length} TabletMaterialSO assets in {DataDir}");
            return result;
        }

        // ---------- scene ----------

        private static void WireScene(TabletMaterialSO[] materials, StringBuilder log)
        {
            const string scenePath = "Assets/_Game/Scenes/Main.unity";
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != scenePath)
            {
                scene = EditorSceneManager.OpenScene(scenePath);
            }

            // --- _Bootstrap managers ---
            var bootstrap = GameObject.Find("_Bootstrap");
            if (bootstrap == null)
            {
                throw new InvalidOperationException("_Bootstrap not found in Main.unity");
            }

            var tabletManager = EnsureComponent<TabletManager>(bootstrap);
            EnsureComponent<CurrencyManager>(bootstrap);
            EnsureComponent<TapInputController>(bootstrap);

            var serialized = new SerializedObject(tabletManager);
            var materialsProp = serialized.FindProperty("materials");
            materialsProp.arraySize = materials.Length;
            for (var i = 0; i < materials.Length; i++)
            {
                materialsProp.GetArrayElementAtIndex(i).objectReferenceValue = materials[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();

            // --- Tablet world object ---
            var tablet = GameObject.Find("Tablet");
            if (tablet == null)
            {
                tablet = new GameObject("Tablet");
            }

            tablet.transform.position = new Vector3(0f, 2f, 0f);
            tablet.transform.localScale = Vector3.one * 1.75f;
            var renderer = EnsureComponent<SpriteRenderer>(tablet);
            renderer.sprite = materials[0].CrackStates[0];
            var collider = EnsureComponent<BoxCollider2D>(tablet);
            collider.size = new Vector2(4f, 4f);
            var view = EnsureComponent<TabletView>(tablet);
            new SerializedObject(view).FindProperty("spriteRenderer").objectReferenceValue = renderer;

            // --- HUD canvas ---
            var hud = GameObject.Find("HUD");
            if (hud == null)
            {
                hud = new GameObject("HUD");
            }

            var canvas = EnsureComponent<Canvas>(hud);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = EnsureComponent<CanvasScaler>(hud);
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;
            EnsureComponent<GraphicRaycaster>(hud);

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var coinText = EnsureText(hud.transform, "CoinText", font, 76, new Color(1f, 0.85f, 0.3f));
            Anchor(coinText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -90f), new Vector2(900f, 110f));
            coinText.text = "0";

            var stageText = EnsureText(hud.transform, "StageText", font, 40, new Color(0.9f, 0.88f, 0.8f));
            Anchor(stageText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -175f), new Vector2(900f, 60f));
            stageText.text = "";

            var hpBg = EnsureImage(hud.transform, "HpBarBg", new Color(0.12f, 0.1f, 0.08f, 0.9f));
            Anchor(hpBg.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -245f), new Vector2(860f, 48f));

            var hpFill = EnsureImage(hpBg.transform, "HpFill", new Color(0.42f, 0.78f, 0.35f));
            hpFill.rectTransform.anchorMin = Vector2.zero;
            hpFill.rectTransform.anchorMax = Vector2.one;
            hpFill.rectTransform.offsetMin = Vector2.zero;
            hpFill.rectTransform.offsetMax = Vector2.zero;

            var hpText = EnsureText(hpBg.transform, "HpText", font, 28, Color.white);
            hpText.rectTransform.anchorMin = Vector2.zero;
            hpText.rectTransform.anchorMax = Vector2.one;
            hpText.rectTransform.offsetMin = Vector2.zero;
            hpText.rectTransform.offsetMax = Vector2.zero;

            var poolGo = hud.transform.Find("FloatingTextRoot")?.gameObject;
            if (poolGo == null)
            {
                poolGo = new GameObject("FloatingTextRoot", typeof(RectTransform));
                poolGo.transform.SetParent(hud.transform, false);
            }

            var poolRt = (RectTransform)poolGo.transform;
            poolRt.anchorMin = Vector2.zero;
            poolRt.anchorMax = Vector2.one;
            poolRt.offsetMin = Vector2.zero;
            poolRt.offsetMax = Vector2.zero;
            var pool = EnsureComponent<FloatingTextPool>(poolGo);
            new SerializedObject(pool).FindProperty("worldAnchor").objectReferenceValue = tablet.transform;

            var hudController = EnsureComponent<HudController>(hud);
            var hudSerialized = new SerializedObject(hudController);
            hudSerialized.FindProperty("coinText").objectReferenceValue = coinText;
            hudSerialized.FindProperty("stageText").objectReferenceValue = stageText;
            hudSerialized.FindProperty("hpFill").objectReferenceValue = hpFill.rectTransform;
            hudSerialized.FindProperty("hpText").objectReferenceValue = hpText;
            hudSerialized.ApplyModifiedPropertiesWithoutUndo();

            // --- EventSystem (Input System UI module) ---
            if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                es.transform.SetAsLastSibling();
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            log.AppendLine("Scene: _Bootstrap managers, Tablet, HUD, EventSystem wired and saved");
        }

        private static T EnsureComponent<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            return c != null ? c : go.AddComponent<T>();
        }

        private static Text EnsureText(Transform parent, string name, Font font, int size, Color color)
        {
            var child = parent.Find(name)?.gameObject;
            if (child == null)
            {
                child = new GameObject(name, typeof(RectTransform));
                child.transform.SetParent(parent, false);
            }

            var text = EnsureComponent<Text>(child);
            text.font = font;
            text.fontSize = size;
            text.fontStyle = FontStyle.Bold;
            text.color = color;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private static Image EnsureImage(Transform parent, string name, Color color)
        {
            var child = parent.Find(name)?.gameObject;
            if (child == null)
            {
                child = new GameObject(name, typeof(RectTransform));
                child.transform.SetParent(parent, false);
            }

            var image = EnsureComponent<Image>(child);
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static void Anchor(RectTransform rt, Vector2 anchor, Vector2 offset, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = offset;
            rt.sizeDelta = size;
        }
    }
}
