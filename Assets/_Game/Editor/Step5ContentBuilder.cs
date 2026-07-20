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
    /// One-shot, idempotent Step 5 setup: 60 ResearchNodeSO assets (4 branches × 15
    /// stages) with the prerequisite chain (stage N needs stage N-1; stage 15 needs
    /// stage 14 MAXED), plus the full-screen research panel wired into Main.unity.
    /// Balance is a clean ladder — tune per-asset in the Inspector.
    /// Writes a summary to Temp/crumble_step5_build.txt.
    /// </summary>
    public static class Step5ContentBuilder
    {
        private const string ResultsPath = "Temp/crumble_step5_build.txt";
        private const string DataDir = "Assets/_Game/Data/Research";

        private struct BranchDef
        {
            public string Key;
            public ResearchBranch Branch;
            public string[] Names;                       // 15 stage names
            public Func<int, ResearchEffectType> Effect; // stage (1-based) → effect
        }

        private static readonly BranchDef[] Branches =
        {
            new BranchDef
            {
                Key = "active",
                Branch = ResearchBranch.ActiveExcavation,
                Names = new[]
                {
                    "Sharper Brushes", "Calloused Hands", "Chisel Techniques", "Leverage Points",
                    "Percussive Method", "Ergonomic Grips", "Twin-Handed Digging", "Fracture Reading",
                    "Resonant Strikes", "Diamond-Tipped Tools", "Master Excavator", "Seismic Precision",
                    "Molecular Cleaving", "Temporal Reflexes", "The Archaeologist's Creed",
                },
                Effect = stage => ResearchEffectType.ClickDamageMultiplier,
            },
            new BranchDef
            {
                Key = "auto",
                Branch = ResearchBranch.AutomationLogistics,
                Names = new[]
                {
                    "Oiled Gears", "Shift Scheduling", "Conveyor Belts", "Steam Pressure",
                    "Assistant Training", "Modular Rigs", "Overclocked Motors", "Synchronized Crews",
                    "Self-Repair Protocols", "Swarm Coordination", "Perpetual Motion", "Quantum Efficiency",
                    "Hive Logistics", "Autonomous Foremen", "The Endless Shift",
                },
                Effect = stage => ResearchEffectType.AssistantDpsMultiplier,
            },
            new BranchDef
            {
                Key = "economy",
                Branch = ResearchBranch.CampEconomy,
                Names = new[]
                {
                    "Bartering Basics", "Bulk Purchasing", "Coin Polishing", "Trade Contacts",
                    "Wholesale Tools", "Relic Appraisal", "Camp Marketplace", "Guild Discounts",
                    "Treasure Instincts", "Auction Mastery", "Golden Ledger", "Monopoly Rights",
                    "Coin Alchemy", "The Midas Method", "The Golden Archive",
                },
                Effect = stage => stage == 2 || stage == 5 || stage == 8 || stage == 12
                    ? ResearchEffectType.UpgradeCostReduction
                    : ResearchEffectType.CoinDropMultiplier,
            },
            new BranchDef
            {
                Key = "intuition",
                Branch = ResearchBranch.ArchaeologicalIntuition,
                Names = new[]
                {
                    "Keen Eyes", "Field Notes", "Trail Mapping", "Dust Reading",
                    "Curator Contacts", "Camel Caravans", "Sixth Sense", "Exhibit Design",
                    "Night Navigation", "Relic Whispering", "Grand Gallery", "Wormhole Shortcuts",
                    "Fate Reading", "Living Museum", "The All-Seeing Eye",
                },
                Effect = stage => stage % 3 == 1 ? ResearchEffectType.ArtifactDropRate
                    : stage % 3 == 2 ? ResearchEffectType.MuseumBonus
                    : ResearchEffectType.ExpeditionSpeed,
            },
        };

        [MenuItem("Crumble/Build Step 5 Content And Scene")]
        public static void Build()
        {
            var log = new StringBuilder();
            try
            {
                var nodes = CreateNodeAssets(log);
                WireScene(nodes, log);
                log.AppendLine("RESULT: OK");
            }
            catch (Exception e)
            {
                log.AppendLine("RESULT: ERROR " + e);
                Debug.LogException(e);
            }

            File.WriteAllText(ResultsPath, log.ToString());
            Debug.Log("[Step5ContentBuilder]\n" + log);
        }

        // ---------- node assets (two passes: create, then link prerequisites) ----------

        private static ResearchNodeSO[] CreateNodeAssets(StringBuilder log)
        {
            Directory.CreateDirectory(DataDir);
            var all = new ResearchNodeSO[Branches.Length * 15];

            for (var b = 0; b < Branches.Length; b++)
            {
                var def = Branches[b];
                for (var stage = 1; stage <= 15; stage++)
                {
                    var id = $"research_{def.Key}_s{stage:00}";
                    var path = $"{DataDir}/{id}.asset";
                    var so = AssetDatabase.LoadAssetAtPath<ResearchNodeSO>(path);
                    var isNew = so == null;
                    if (isNew)
                    {
                        so = ScriptableObject.CreateInstance<ResearchNodeSO>();
                    }

                    var isUltimate = stage == 15;
                    var effect = def.Effect(stage);

                    so.Id = id;
                    so.DisplayName = def.Names[stage - 1];
                    so.Description = ""; // effect line is generated by the UI from type + magnitude
                    so.Branch = def.Branch;
                    so.Stage = stage;
                    so.BaseKpCost = Math.Max(1, Math.Round(Math.Pow(1.6, stage - 1)));
                    so.CostGrowthFactor = 2;
                    so.MaxLevel = isUltimate ? 1 : 5;
                    so.EffectType = effect;
                    so.EffectPerLevel = isUltimate ? 1.0
                        : effect == ResearchEffectType.UpgradeCostReduction ? 0.02
                        : 0.04 + 0.02 * (stage - 1);

                    if (isNew)
                    {
                        AssetDatabase.CreateAsset(so, path);
                    }
                    else
                    {
                        EditorUtility.SetDirty(so);
                    }

                    all[b * 15 + stage - 1] = so;
                }
            }

            // second pass: chain prerequisites within each branch
            for (var b = 0; b < Branches.Length; b++)
            {
                for (var stage = 1; stage <= 15; stage++)
                {
                    var node = all[b * 15 + stage - 1];
                    if (stage == 1)
                    {
                        node.Prerequisites = Array.Empty<ResearchPrerequisite>();
                    }
                    else
                    {
                        var previous = all[b * 15 + stage - 2];
                        node.Prerequisites = new[]
                        {
                            new ResearchPrerequisite
                            {
                                Node = previous,
                                // GDD: stage 15 stays a "?" silhouette until stage 14 is maxed
                                RequiredLevel = stage == 15 ? previous.MaxLevel : 1,
                            },
                        };
                    }

                    EditorUtility.SetDirty(node);
                }
            }

            AssetDatabase.SaveAssets();
            log.AppendLine($"Research: {all.Length} ResearchNodeSO assets in {DataDir}");
            return all;
        }

        // ---------- scene ----------

        private static void WireScene(ResearchNodeSO[] nodes, StringBuilder log)
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

            var researchManager = EnsureComponent<ResearchManager>(bootstrap);
            var managerSerialized = new SerializedObject(researchManager);
            var nodesProp = managerSerialized.FindProperty("nodes");
            nodesProp.arraySize = nodes.Length;
            for (var i = 0; i < nodes.Length; i++)
            {
                nodesProp.GetArrayElementAtIndex(i).objectReferenceValue = nodes[i];
            }

            managerSerialized.ApplyModifiedPropertiesWithoutUndo();

            var hud = GameObject.Find("HUD");
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // --- open button, top-left under the KP counter ---
            var openButton = EnsureButton(hud.transform, "ResearchButton", font, "RESEARCH", 32);
            Anchor((RectTransform)openButton.transform, new Vector2(0f, 1f), new Vector2(20f, -100f), new Vector2(260f, 90f));
            openButton.image.color = new Color(0.2f, 0.45f, 0.5f);

            // --- full-screen research panel ---
            var panel = EnsureChild(hud.transform, "ResearchPanel");
            var panelRt = (RectTransform)panel.transform;
            panelRt.anchorMin = Vector2.zero;
            panelRt.anchorMax = Vector2.one;
            panelRt.offsetMin = Vector2.zero;
            panelRt.offsetMax = Vector2.zero;
            var bg = EnsureComponent<Image>(panel);
            bg.color = new Color(0.07f, 0.06f, 0.05f, 0.985f);
            bg.raycastTarget = true;

            var title = EnsureText(panel.transform, "TitleText", font, 54, new Color(0.85f, 0.65f, 0.25f));
            Anchor(title.rectTransform, new Vector2(0f, 1f), new Vector2(30f, -30f), new Vector2(460f, 70f));
            title.alignment = TextAnchor.MiddleLeft;
            title.text = "RESEARCH";

            var kpText = EnsureText(panel.transform, "KpText", font, 42, new Color(0.55f, 0.75f, 1f));
            Anchor(kpText.rectTransform, new Vector2(1f, 1f), new Vector2(-170f, -45f), new Vector2(420f, 60f));
            kpText.alignment = TextAnchor.MiddleRight;
            kpText.text = "KP 0";

            var closeButton = EnsureButton(panel.transform, "CloseButton", font, "X", 44);
            Anchor((RectTransform)closeButton.transform, new Vector2(1f, 1f), new Vector2(-20f, -20f), new Vector2(110f, 90f));
            closeButton.image.color = new Color(0.5f, 0.2f, 0.18f);

            // --- branch tabs ---
            var tabActive = EnsureButton(panel.transform, "TabActive", font, "ACTIVE", 30);
            Anchor((RectTransform)tabActive.transform, new Vector2(0f, 1f), new Vector2(15f, -125f), new Vector2(250f, 80f));
            var tabAuto = EnsureButton(panel.transform, "TabAuto", font, "AUTO", 30);
            Anchor((RectTransform)tabAuto.transform, new Vector2(0f, 1f), new Vector2(280f, -125f), new Vector2(250f, 80f));
            var tabEconomy = EnsureButton(panel.transform, "TabEconomy", font, "ECONOMY", 30);
            Anchor((RectTransform)tabEconomy.transform, new Vector2(0f, 1f), new Vector2(545f, -125f), new Vector2(250f, 80f));
            var tabIntuition = EnsureButton(panel.transform, "TabIntuition", font, "INTUITION", 30);
            Anchor((RectTransform)tabIntuition.transform, new Vector2(0f, 1f), new Vector2(810f, -125f), new Vector2(250f, 80f));

            // --- scroll list ---
            var scrollGo = EnsureChild(panel.transform, "ScrollView");
            var scrollRt = (RectTransform)scrollGo.transform;
            scrollRt.anchorMin = Vector2.zero;
            scrollRt.anchorMax = Vector2.one;
            scrollRt.offsetMin = new Vector2(10f, 15f);
            scrollRt.offsetMax = new Vector2(-10f, -220f);
            var scrollRect = EnsureComponent<ScrollRect>(scrollGo);

            var viewport = EnsureChild(scrollGo.transform, "Viewport");
            var viewportRt = (RectTransform)viewport.transform;
            viewportRt.anchorMin = Vector2.zero;
            viewportRt.anchorMax = Vector2.one;
            viewportRt.offsetMin = Vector2.zero;
            viewportRt.offsetMax = Vector2.zero;
            EnsureComponent<RectMask2D>(viewport);
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

            var template = BuildRowTemplate(content.transform, font);

            // --- controller + refs ---
            var controller = EnsureComponent<ResearchPanelController>(panel);
            var controllerSerialized = new SerializedObject(controller);
            controllerSerialized.FindProperty("content").objectReferenceValue = contentRt;
            controllerSerialized.FindProperty("rowTemplate").objectReferenceValue = template;
            controllerSerialized.FindProperty("activeTabButton").objectReferenceValue = tabActive;
            controllerSerialized.FindProperty("autoTabButton").objectReferenceValue = tabAuto;
            controllerSerialized.FindProperty("economyTabButton").objectReferenceValue = tabEconomy;
            controllerSerialized.FindProperty("intuitionTabButton").objectReferenceValue = tabIntuition;
            controllerSerialized.FindProperty("kpText").objectReferenceValue = kpText;
            controllerSerialized.FindProperty("closeButton").objectReferenceValue = closeButton;
            controllerSerialized.ApplyModifiedPropertiesWithoutUndo();

            var opener = EnsureComponent<PanelOpenButton>(openButton.gameObject);
            var openerSerialized = new SerializedObject(opener);
            openerSerialized.FindProperty("target").objectReferenceValue = panel;
            openerSerialized.ApplyModifiedPropertiesWithoutUndo();

            // panel above the game HUD, but the prestige dialog stays topmost
            panel.transform.SetAsLastSibling();
            var dialog = hud.transform.Find("PrestigeDialog");
            if (dialog != null)
            {
                dialog.SetAsLastSibling();
            }

            panel.SetActive(false);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            log.AppendLine("Scene: ResearchManager (60 nodes), research button + panel wired and saved");
        }

        private static GameObject BuildRowTemplate(Transform content, Font font)
        {
            var row = EnsureChild(content, "RowTemplate");
            var rowImage = EnsureComponent<Image>(row);
            rowImage.color = new Color(0.16f, 0.14f, 0.12f, 0.92f);
            rowImage.raycastTarget = true;
            EnsureComponent<CanvasGroup>(row);
            var element = EnsureComponent<LayoutElement>(row);
            element.preferredHeight = 160f;
            element.minHeight = 160f;

            var stageText = EnsureText(row.transform, "StageText", font, 30, new Color(0.6f, 0.55f, 0.45f));
            SetStretch(stageText.rectTransform, new Vector2(0f, 0f), new Vector2(0.09f, 1f), Vector2.zero, Vector2.zero);

            var nameText = EnsureText(row.transform, "NameText", font, 38, Color.white);
            SetStretch(nameText.rectTransform, new Vector2(0.09f, 0.5f), new Vector2(0.62f, 1f), Vector2.zero, new Vector2(0f, -6f));
            nameText.alignment = TextAnchor.MiddleLeft;

            var effectText = EnsureText(row.transform, "EffectText", font, 27, new Color(0.65f, 0.8f, 0.55f));
            SetStretch(effectText.rectTransform, new Vector2(0.09f, 0f), new Vector2(0.62f, 0.5f), new Vector2(0f, 8f), Vector2.zero);
            effectText.alignment = TextAnchor.MiddleLeft;
            effectText.horizontalOverflow = HorizontalWrapMode.Wrap;

            var levelText = EnsureText(row.transform, "LevelText", font, 34, new Color(0.9f, 0.88f, 0.8f));
            SetStretch(levelText.rectTransform, new Vector2(0.62f, 0f), new Vector2(0.74f, 1f), Vector2.zero, Vector2.zero);

            var buyButton = EnsureButton(row.transform, "BuyButton", font, "1 KP", 30);
            SetStretch((RectTransform)buyButton.transform, new Vector2(0.75f, 0.12f), new Vector2(0.98f, 0.88f), Vector2.zero, Vector2.zero);
            buyButton.image.color = new Color(0.25f, 0.4f, 0.55f);

            var view = EnsureComponent<ResearchRowView>(row);
            var viewSerialized = new SerializedObject(view);
            viewSerialized.FindProperty("canvasGroup").objectReferenceValue = row.GetComponent<CanvasGroup>();
            viewSerialized.FindProperty("stageText").objectReferenceValue = stageText;
            viewSerialized.FindProperty("nameText").objectReferenceValue = nameText;
            viewSerialized.FindProperty("effectText").objectReferenceValue = effectText;
            viewSerialized.FindProperty("levelText").objectReferenceValue = levelText;
            viewSerialized.FindProperty("buyButton").objectReferenceValue = buyButton;
            viewSerialized.FindProperty("costText").objectReferenceValue = buyButton.GetComponentInChildren<Text>(true);
            viewSerialized.ApplyModifiedPropertiesWithoutUndo();

            row.SetActive(false);
            return row;
        }

        // ---------- helpers ----------

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
            image.raycastTarget = true;
            var button = EnsureComponent<Button>(go);
            button.targetGraphic = image;
            if (image.color == Color.white)
            {
                image.color = new Color(0.55f, 0.4f, 0.15f);
            }

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
