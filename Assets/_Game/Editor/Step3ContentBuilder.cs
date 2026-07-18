using System;
using System.IO;
using System.Text;
using Crumble.Data;
using Crumble.Gameplay;
using Crumble.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Crumble.EditorTools
{
    /// <summary>
    /// One-shot, idempotent Step 3 setup: placeholder icons + the 12 ToolSO / 12
    /// AssistantSO assets (GDD §5 rosters), and the bottom upgrade panel in Main.unity
    /// (tabs, x1/x10/MAX button, scroll list, row template). Balance is a clean
    /// exponential ladder — tune per-asset in the Inspector at any time.
    /// Writes a summary to Temp/crumble_step3_build.txt.
    /// </summary>
    public static class Step3ContentBuilder
    {
        private const string ResultsPath = "Temp/crumble_step3_build.txt";
        private const string ArtDir = "Assets/_Game/Art/Placeholders/Icons";
        private const string ToolDataDir = "Assets/_Game/Data/Tools";
        private const string AssistantDataDir = "Assets/_Game/Data/Assistants";
        private const int IconSize = 64;

        private static readonly string[] ToolNames =
        {
            "Dusting Brush", "Dental Chisel", "Geologist Hammer", "Magnifying Glass",
            "Acid Vial", "Pressure Washer", "Pneumatic Drill", "Ultrasonic Scalpel",
            "Laser Cutter", "Plasma Torch", "Quantum Dissolver", "Time Accelerator",
        };

        private static readonly string[] AssistantNames =
        {
            "Water Dripper", "Intern Student", "Clockwork Automaton", "Steam Piston",
            "Acid Pump", "Windmill Drill", "Sonar Reflector", "Laser Tripod",
            "Holographic Miner", "Anti-Gravity Field", "Interdimensional Portal", "Cosmic Watcher",
        };

        [MenuItem("Crumble/Build Step 3 Content And Scene")]
        public static void Build()
        {
            var log = new StringBuilder();
            try
            {
                GenerateIcons(log);
                var tools = CreateToolAssets(log);
                var assistants = CreateAssistantAssets(log);
                WireScene(tools, assistants, log);
                log.AppendLine("RESULT: OK");
            }
            catch (Exception e)
            {
                log.AppendLine("RESULT: ERROR " + e);
                Debug.LogException(e);
            }

            File.WriteAllText(ResultsPath, log.ToString());
            Debug.Log("[Step3ContentBuilder]\n" + log);
        }

        private static string ToId(string prefix, string name)
        {
            return prefix + "_" + name.ToLowerInvariant().Replace(" ", "_").Replace("-", "_");
        }

        // ---------- icons ----------

        private static string IconPath(bool tool, int index)
        {
            var name = tool ? ToolNames[index] : AssistantNames[index];
            return $"{ArtDir}/{ToId(tool ? "tool" : "assistant", name)}.png";
        }

        private static void GenerateIcons(StringBuilder log)
        {
            Directory.CreateDirectory(ArtDir);
            for (var i = 0; i < ToolNames.Length; i++)
            {
                var hue = (0.02f + 0.055f * i) % 1f; // warm ladder for tools
                File.WriteAllBytes(IconPath(true, i), RenderIcon(hue, 0.62f, i).EncodeToPNG());
            }

            for (var i = 0; i < AssistantNames.Length; i++)
            {
                var hue = (0.48f + 0.045f * i) % 1f; // cool ladder for assistants
                File.WriteAllBytes(IconPath(false, i), RenderIcon(hue, 0.55f, i + 50).EncodeToPNG());
            }

            AssetDatabase.Refresh();

            for (var i = 0; i < ToolNames.Length; i++)
            {
                ImportIcon(IconPath(true, i));
                ImportIcon(IconPath(false, i));
            }

            log.AppendLine($"Icons: {ToolNames.Length + AssistantNames.Length} generated in {ArtDir}");
        }

        private static void ImportIcon(string path)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        private static Texture2D RenderIcon(float hue, float saturation, int seed)
        {
            var rng = new System.Random(seed * 13 + 7);
            var tex = new Texture2D(IconSize, IconSize, TextureFormat.RGBA32, false);
            var fill = Color.HSVToRGB(hue, saturation, 0.95f);
            var border = Color.HSVToRGB(hue, saturation, 0.45f);
            var stripe = Color.HSVToRGB(hue, saturation * 0.8f, 0.75f);
            var clear = new Color(0, 0, 0, 0);

            const int inset = 6;
            const int corner = 9;
            for (var y = 0; y < IconSize; y++)
            {
                for (var x = 0; x < IconSize; x++)
                {
                    var inside = x >= inset && x < IconSize - inset && y >= inset && y < IconSize - inset;
                    if (inside)
                    {
                        var cx = Mathf.Min(x - inset, IconSize - inset - 1 - x);
                        var cy = Mathf.Min(y - inset, IconSize - inset - 1 - y);
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

                    var edge = x < inset + 3 || x >= IconSize - inset - 3 || y < inset + 3 || y >= IconSize - inset - 3;
                    var onStripe = ((x + y) / 8 + seed) % 3 == 0;
                    var noise = 1f + ((float)rng.NextDouble() - 0.5f) * 0.1f;
                    var c = edge ? border : (onStripe ? stripe : fill) * noise;
                    c.a = 1f;
                    tex.SetPixel(x, y, c);
                }
            }

            tex.Apply();
            return tex;
        }

        // ---------- ScriptableObject assets ----------

        private static ToolSO[] CreateToolAssets(StringBuilder log)
        {
            Directory.CreateDirectory(ToolDataDir);
            var result = new ToolSO[ToolNames.Length];
            for (var i = 0; i < ToolNames.Length; i++)
            {
                var id = ToId("tool", ToolNames[i]);
                var path = $"{ToolDataDir}/{id}.asset";
                var so = AssetDatabase.LoadAssetAtPath<ToolSO>(path);
                var isNew = so == null;
                if (isNew)
                {
                    so = ScriptableObject.CreateInstance<ToolSO>();
                }

                so.Id = id;
                so.DisplayName = ToolNames[i];
                so.OrderIndex = i;
                so.BaseCost = 10 * Math.Pow(21, i);
                so.GrowthFactor = 1.07;
                so.BaseDamagePerLevel = Math.Pow(6.5, i);
                so.Icon = AssetDatabase.LoadAssetAtPath<Sprite>(IconPath(true, i));

                if (isNew)
                {
                    AssetDatabase.CreateAsset(so, path);
                }
                else
                {
                    EditorUtility.SetDirty(so);
                }

                result[i] = so;
            }

            AssetDatabase.SaveAssets();
            log.AppendLine($"Tools: {result.Length} ToolSO assets in {ToolDataDir}");
            return result;
        }

        private static AssistantSO[] CreateAssistantAssets(StringBuilder log)
        {
            Directory.CreateDirectory(AssistantDataDir);
            var result = new AssistantSO[AssistantNames.Length];
            for (var i = 0; i < AssistantNames.Length; i++)
            {
                var id = ToId("assistant", AssistantNames[i]);
                var path = $"{AssistantDataDir}/{id}.asset";
                var so = AssetDatabase.LoadAssetAtPath<AssistantSO>(path);
                var isNew = so == null;
                if (isNew)
                {
                    so = ScriptableObject.CreateInstance<AssistantSO>();
                }

                so.Id = id;
                so.DisplayName = AssistantNames[i];
                so.OrderIndex = i;
                so.BaseCost = 60 * Math.Pow(24, i);
                so.GrowthFactor = 1.15;
                so.BaseDpsPerLevel = 2 * Math.Pow(7, i);
                so.Icon = AssetDatabase.LoadAssetAtPath<Sprite>(IconPath(false, i));

                if (isNew)
                {
                    AssetDatabase.CreateAsset(so, path);
                }
                else
                {
                    EditorUtility.SetDirty(so);
                }

                result[i] = so;
            }

            AssetDatabase.SaveAssets();
            log.AppendLine($"Assistants: {result.Length} AssistantSO assets in {AssistantDataDir}");
            return result;
        }

        // ---------- scene ----------

        private static void WireScene(ToolSO[] tools, AssistantSO[] assistants, StringBuilder log)
        {
            const string scenePath = "Assets/_Game/Scenes/Main.unity";
            if (EditorSceneManager.GetActiveScene().path != scenePath)
            {
                EditorSceneManager.OpenScene(scenePath);
            }

            var bootstrap = GameObject.Find("_Bootstrap");
            if (bootstrap == null)
            {
                throw new InvalidOperationException("_Bootstrap not found in Main.unity");
            }

            // --- UpgradeManager with content arrays ---
            var upgradeManager = EnsureComponent<UpgradeManager>(bootstrap);
            var serialized = new SerializedObject(upgradeManager);
            FillArray(serialized.FindProperty("tools"), tools);
            FillArray(serialized.FindProperty("assistants"), assistants);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            // --- tablet sits above the panel ---
            var tablet = GameObject.Find("Tablet");
            tablet.transform.position = new Vector3(0f, 2.2f, 0f);
            tablet.transform.localScale = Vector3.one * 1.4f;

            var hud = GameObject.Find("HUD");
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // --- stats line under the HP bar ---
            var statsText = EnsureText(hud.transform, "StatsText", font, 34, new Color(0.95f, 0.9f, 0.75f));
            Anchor(statsText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -305f), new Vector2(900f, 46f));
            statsText.text = "";

            var hudController = hud.GetComponent<HudController>();
            var hudSerialized = new SerializedObject(hudController);
            hudSerialized.FindProperty("statsText").objectReferenceValue = statsText;
            hudSerialized.ApplyModifiedPropertiesWithoutUndo();

            // --- upgrade panel ---
            var panel = EnsureChild(hud.transform, "UpgradePanel");
            var panelRt = (RectTransform)panel.transform;
            panelRt.anchorMin = new Vector2(0f, 0f);
            panelRt.anchorMax = new Vector2(1f, 0f);
            panelRt.pivot = new Vector2(0.5f, 0f);
            panelRt.anchoredPosition = Vector2.zero;
            panelRt.sizeDelta = new Vector2(0f, 780f);
            EnsureComponent<Image>(panel).color = new Color(0.09f, 0.08f, 0.07f, 0.96f);
            panel.GetComponent<Image>().raycastTarget = true; // panel swallows taps behind it

            // tab bar
            var toolsTab = EnsureButton(panel.transform, "ToolsTab", font, "TOOLS", 38);
            Anchor((RectTransform)toolsTab.transform, new Vector2(0f, 1f), new Vector2(20f, -8f), new Vector2(300f, 78f), new Vector2(0f, 1f));
            var assistantsTab = EnsureButton(panel.transform, "AssistantsTab", font, "ASSISTANTS", 38);
            Anchor((RectTransform)assistantsTab.transform, new Vector2(0f, 1f), new Vector2(336f, -8f), new Vector2(380f, 78f), new Vector2(0f, 1f));
            var buyModeButton = EnsureButton(panel.transform, "BuyModeButton", font, "x1", 40);
            Anchor((RectTransform)buyModeButton.transform, new Vector2(1f, 1f), new Vector2(-20f, -8f), new Vector2(190f, 78f), new Vector2(1f, 1f));

            // scroll view
            var scrollGo = EnsureChild(panel.transform, "ScrollView");
            var scrollRt = (RectTransform)scrollGo.transform;
            scrollRt.anchorMin = Vector2.zero;
            scrollRt.anchorMax = Vector2.one;
            scrollRt.offsetMin = new Vector2(10f, 10f);
            scrollRt.offsetMax = new Vector2(-10f, -96f);
            var scrollRect = EnsureComponent<ScrollRect>(scrollGo);

            var viewport = EnsureChild(scrollGo.transform, "Viewport");
            var viewportRt = (RectTransform)viewport.transform;
            viewportRt.anchorMin = Vector2.zero;
            viewportRt.anchorMax = Vector2.one;
            viewportRt.offsetMin = Vector2.zero;
            viewportRt.offsetMax = Vector2.zero;
            EnsureComponent<RectMask2D>(viewport);
            // invisible but raycastable, so drags in row gaps/padding also reach the ScrollRect
            var viewportImage = EnsureComponent<Image>(viewport);
            viewportImage.color = new Color(0f, 0f, 0f, 0f);
            viewportImage.raycastTarget = true;

            var content = EnsureChild(viewport.transform, "Content");
            var contentRt = (RectTransform)content.transform;
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.anchoredPosition = Vector2.zero;
            contentRt.sizeDelta = new Vector2(0f, 300f);
            var layout = EnsureComponent<VerticalLayoutGroup>(content);
            layout.padding = new RectOffset(12, 12, 12, 12);
            layout.spacing = 12f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            var fitter = EnsureComponent<ContentSizeFitter>(content);
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = viewportRt;
            scrollRect.content = contentRt;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 40f;

            // row template
            var template = BuildRowTemplate(content.transform, font);

            // panel controller
            var controller = EnsureComponent<UpgradePanelController>(panel);
            var controllerSerialized = new SerializedObject(controller);
            controllerSerialized.FindProperty("content").objectReferenceValue = contentRt;
            controllerSerialized.FindProperty("rowTemplate").objectReferenceValue = template;
            controllerSerialized.FindProperty("toolsTabButton").objectReferenceValue = toolsTab;
            controllerSerialized.FindProperty("assistantsTabButton").objectReferenceValue = assistantsTab;
            controllerSerialized.FindProperty("buyModeButton").objectReferenceValue = buyModeButton;
            controllerSerialized.FindProperty("buyModeLabel").objectReferenceValue =
                buyModeButton.GetComponentInChildren<Text>(true);
            controllerSerialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            log.AppendLine("Scene: UpgradeManager, stats line, upgrade panel + row template wired and saved");
        }

        private static GameObject BuildRowTemplate(Transform content, Font font)
        {
            var row = EnsureChild(content, "RowTemplate");
            var rowImage = EnsureComponent<Image>(row);
            rowImage.color = new Color(0.16f, 0.14f, 0.12f, 0.92f);
            rowImage.raycastTarget = true; // drags must start on something inside the ScrollRect
            var element = EnsureComponent<LayoutElement>(row);
            element.preferredHeight = 150f;
            element.minHeight = 150f;

            var icon = EnsureImage(row.transform, "Icon", Color.white);
            Anchor(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(22f, 0f), new Vector2(110f, 110f), new Vector2(0f, 0.5f));

            var nameText = EnsureText(row.transform, "NameText", font, 40, Color.white);
            SetStretch(nameText.rectTransform, new Vector2(0f, 0.5f), new Vector2(0.58f, 1f), new Vector2(150f, 0f), new Vector2(0f, -6f));
            nameText.alignment = TextAnchor.MiddleLeft;

            var effectText = EnsureText(row.transform, "EffectText", font, 30, new Color(0.65f, 0.8f, 0.55f));
            SetStretch(effectText.rectTransform, new Vector2(0f, 0f), new Vector2(0.58f, 0.5f), new Vector2(150f, 8f), Vector2.zero);
            effectText.alignment = TextAnchor.MiddleLeft;

            var levelText = EnsureText(row.transform, "LevelText", font, 36, new Color(0.9f, 0.88f, 0.8f));
            SetStretch(levelText.rectTransform, new Vector2(0.57f, 0f), new Vector2(0.72f, 1f), Vector2.zero, Vector2.zero);

            var buyButton = EnsureButton(row.transform, "BuyButton", font, "x1\n10", 30);
            SetStretch((RectTransform)buyButton.transform, new Vector2(0.73f, 0.1f), new Vector2(0.98f, 0.9f), Vector2.zero, Vector2.zero);

            var view = EnsureComponent<UpgradeRowView>(row);
            var viewSerialized = new SerializedObject(view);
            viewSerialized.FindProperty("icon").objectReferenceValue = icon;
            viewSerialized.FindProperty("nameText").objectReferenceValue = nameText;
            viewSerialized.FindProperty("levelText").objectReferenceValue = levelText;
            viewSerialized.FindProperty("effectText").objectReferenceValue = effectText;
            viewSerialized.FindProperty("buyButton").objectReferenceValue = buyButton;
            viewSerialized.FindProperty("costText").objectReferenceValue = buyButton.GetComponentInChildren<Text>(true);
            viewSerialized.ApplyModifiedPropertiesWithoutUndo();

            row.SetActive(false);
            return row;
        }

        // ---------- helpers ----------

        private static void FillArray(SerializedProperty property, UnityEngine.Object[] values)
        {
            property.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
        }

        private static GameObject EnsureChild(Transform parent, string name)
        {
            var existing = parent.Find(name);
            if (existing != null)
            {
                return existing.gameObject;
            }

            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static T EnsureComponent<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            return c != null ? c : go.AddComponent<T>();
        }

        private static Button EnsureButton(Transform parent, string name, Font font, string label, int fontSize)
        {
            var go = EnsureChild(parent, name);
            var image = EnsureComponent<Image>(go);
            image.color = new Color(0.55f, 0.4f, 0.15f);
            image.raycastTarget = true;
            var button = EnsureComponent<Button>(go);
            button.targetGraphic = image;

            var text = EnsureText(go.transform, "Label", font, fontSize, Color.white);
            SetStretch(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            text.text = label;
            return button;
        }

        private static Text EnsureText(Transform parent, string name, Font font, int size, Color color)
        {
            var go = EnsureChild(parent, name);
            var text = EnsureComponent<Text>(go);
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
            var go = EnsureChild(parent, name);
            var image = EnsureComponent<Image>(go);
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static void Anchor(RectTransform rt, Vector2 anchor, Vector2 offset, Vector2 size, Vector2? pivot = null)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot ?? anchor;
            rt.anchoredPosition = offset;
            rt.sizeDelta = size;
        }

        private static void SetStretch(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
        }
    }
}
