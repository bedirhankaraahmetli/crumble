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
    /// One-shot, idempotent Step 9 setup: the 4 Cosmic Altar upgrade assets, the
    /// CosmicArchive/CosmicAltar managers on _Bootstrap, the HUD Time Crystal counter,
    /// the (hidden-until-revealed) COSMIC button, the Cosmic Archive panel with altar
    /// rows + "Solve the Universal Secret" button, and the Hard Prestige confirm dialog.
    /// Writes a summary to Temp/crumble_step9_build.txt.
    /// </summary>
    public static class Step9ContentBuilder
    {
        private const string ResultsPath = "Temp/crumble_step9_build.txt";
        private const string AltarFolder = "Assets/_Game/Data/Altar";

        [MenuItem("Crumble/Build Step 9 Content And Scene")]
        public static void Build()
        {
            var log = new StringBuilder();
            try
            {
                var upgrades = BuildAltarAssets(log);
                WireScene(upgrades, log);
                log.AppendLine("RESULT: OK");
            }
            catch (Exception e)
            {
                log.AppendLine("RESULT: ERROR " + e);
                Debug.LogException(e);
            }

            File.WriteAllText(ResultsPath, log.ToString());
            Debug.Log("[Step9ContentBuilder]\n" + log);
        }

        // ---------- content assets ----------

        private static AltarUpgradeSO[] BuildAltarAssets(StringBuilder log)
        {
            if (!AssetDatabase.IsValidFolder(AltarFolder))
            {
                AssetDatabase.CreateFolder("Assets/_Game/Data", "Altar");
            }

            var upgrades = new[]
            {
                EnsureAltarUpgrade("altar_chrono_hammer", "Chrono Hammer",
                    "Every strike echoes across timelines.",
                    AltarEffectType.ClickDamage, multiplierPerLevel: 1.5, baseCost: 5, growth: 1.8),
                EnsureAltarUpgrade("altar_eternal_engine", "Eternal Engine",
                    "Assistants that never sleep, in any era.",
                    AltarEffectType.AssistantDps, multiplierPerLevel: 1.5, baseCost: 5, growth: 1.8),
                EnsureAltarUpgrade("altar_golden_timeline", "Golden Timeline",
                    "Choose only the histories where you struck it rich.",
                    AltarEffectType.CoinGain, multiplierPerLevel: 1.5, baseCost: 8, growth: 2.0),
                EnsureAltarUpgrade("altar_akashic_memory", "Akashic Memory",
                    "Remember every secret you have ever solved.",
                    AltarEffectType.KnowledgeGain, multiplierPerLevel: 1.25, baseCost: 10, growth: 2.2),
            };

            AssetDatabase.SaveAssets();
            log.AppendLine($"Altar upgrades: {upgrades.Length} assets in {AltarFolder}");
            return upgrades;
        }

        private static AltarUpgradeSO EnsureAltarUpgrade(
            string id, string displayName, string description,
            AltarEffectType effectType, double multiplierPerLevel, double baseCost, double growth)
        {
            var path = $"{AltarFolder}/Altar_{id.Replace("altar_", "")}.asset";
            var so = AssetDatabase.LoadAssetAtPath<AltarUpgradeSO>(path);
            if (so == null)
            {
                so = ScriptableObject.CreateInstance<AltarUpgradeSO>();
                AssetDatabase.CreateAsset(so, path);
            }

            so.Id = id;
            so.DisplayName = displayName;
            so.Description = description;
            so.EffectType = effectType;
            so.MultiplierPerLevel = multiplierPerLevel;
            so.BaseTimeCrystalCost = baseCost;
            so.CostGrowthFactor = growth;
            EditorUtility.SetDirty(so);
            return so;
        }

        // ---------- scene ----------

        private static void WireScene(AltarUpgradeSO[] upgrades, StringBuilder log)
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

            var altarManager = EnsureComponent<CosmicAltarManager>(bootstrap);
            var altarSerialized = new SerializedObject(altarManager);
            var upgradesProp = altarSerialized.FindProperty("upgrades");
            upgradesProp.arraySize = upgrades.Length;
            for (var i = 0; i < upgrades.Length; i++)
            {
                upgradesProp.GetArrayElementAtIndex(i).objectReferenceValue = upgrades[i];
            }

            altarSerialized.ApplyModifiedPropertiesWithoutUndo();
            EnsureComponent<CosmicArchiveManager>(bootstrap);

            var hud = GameObject.Find("HUD");
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // --- Time Crystal counter, right side under the prestige button ---
            var tcText = EnsureText(hud.transform, "TcText", font, 34, new Color(0.45f, 0.85f, 0.95f));
            Anchor(tcText.rectTransform, new Vector2(1f, 1f), new Vector2(-20f, -160f), new Vector2(250f, 50f));
            tcText.alignment = TextAnchor.MiddleRight;
            tcText.text = "TC 0";

            var hudController = hud.GetComponent<HudController>();
            var hudSerialized = new SerializedObject(hudController);
            hudSerialized.FindProperty("tcText").objectReferenceValue = tcText;
            hudSerialized.ApplyModifiedPropertiesWithoutUndo();

            // --- COSMIC button, left column below TENT (revealed by CosmicButtonView) ---
            var cosmicButton = EnsureButton(hud.transform, "CosmicButton", font, "COSMIC", 30);
            Anchor((RectTransform)cosmicButton.transform, new Vector2(0f, 1f), new Vector2(20f, -365f), new Vector2(260f, 80f));
            cosmicButton.image.color = new Color(0.4f, 0.2f, 0.62f);

            // --- the Cosmic Archive panel ---
            var panel = EnsureChild(hud.transform, "CosmicPanel");
            var panelRt = (RectTransform)panel.transform;
            SetStretch(panelRt, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var panelBg = EnsureComponent<Image>(panel);
            panelBg.color = new Color(0.06f, 0.05f, 0.10f, 1f); // opaque: HUD text bleeds through anything less

            panelBg.raycastTarget = true;

            var title = EnsureText(panel.transform, "TitleText", font, 58, new Color(0.75f, 0.6f, 1f));
            Anchor(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(900f, 90f));
            title.text = "COSMIC ARCHIVE";

            var closeButton = EnsureButton(panel.transform, "CloseButton", font, "X", 40);
            Anchor((RectTransform)closeButton.transform, new Vector2(1f, 1f), new Vector2(-20f, -20f), new Vector2(110f, 90f));
            closeButton.image.color = new Color(0.35f, 0.33f, 0.3f);

            var tcBalance = EnsureText(panel.transform, "TcBalanceText", font, 40, new Color(0.45f, 0.85f, 0.95f));
            Anchor(tcBalance.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -165f), new Vector2(900f, 60f));
            tcBalance.text = "Time Crystals: 0";

            var altarTitle = EnsureText(panel.transform, "AltarTitle", font, 36, new Color(0.85f, 0.65f, 0.25f));
            Anchor(altarTitle.rectTransform, new Vector2(0f, 1f), new Vector2(40f, -245f), new Vector2(500f, 50f), new Vector2(0f, 1f));
            altarTitle.alignment = TextAnchor.MiddleLeft;
            altarTitle.text = "COSMIC ALTAR";

            var rows = new CosmicAltarRowView[upgrades.Length];
            for (var i = 0; i < upgrades.Length; i++)
            {
                rows[i] = EnsureAltarRow(panel.transform, upgrades[i], i, font);
            }

            var status = EnsureText(panel.transform, "StatusText", font, 30, new Color(0.85f, 0.8f, 0.95f));
            Anchor(status.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 420f), new Vector2(960f, 90f), new Vector2(0.5f, 0f));
            status.horizontalOverflow = HorizontalWrapMode.Wrap;

            var solveButton = EnsureButton(panel.transform, "SolveButton", font, "SOLVE THE UNIVERSAL SECRET\n+0 TC", 36);
            Anchor((RectTransform)solveButton.transform, new Vector2(0.5f, 0f), new Vector2(0f, 230f), new Vector2(940f, 160f), new Vector2(0.5f, 0f));
            solveButton.image.color = new Color(0.62f, 0.18f, 0.35f);

            // --- Hard Prestige confirmation dialog ---
            var dialog = EnsureChild(hud.transform, "CosmicDialog");
            var dialogRt = (RectTransform)dialog.transform;
            SetStretch(dialogRt, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var dim = EnsureComponent<Image>(dialog);
            dim.color = new Color(0f, 0f, 0f, 0.85f);
            dim.raycastTarget = true;

            var box = EnsureImage(dialog.transform, "DialogBox", new Color(0.09f, 0.07f, 0.14f, 0.98f));
            Anchor(box.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(920f, 780f));

            var dialogTitle = EnsureText(box.transform, "TitleText", font, 50, new Color(0.75f, 0.6f, 1f));
            Anchor(dialogTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(860f, 80f));
            dialogTitle.text = "THE UNIVERSAL SECRET";

            var body = EnsureText(box.transform, "BodyText", font, 32, new Color(0.92f, 0.9f, 0.95f));
            SetStretch(body.rectTransform, new Vector2(0.06f, 0.28f), new Vector2(0.94f, 0.84f), Vector2.zero, Vector2.zero);
            body.horizontalOverflow = HorizontalWrapMode.Wrap;
            body.alignment = TextAnchor.MiddleCenter;

            var confirm = EnsureButton(box.transform, "ConfirmButton", font, "CONFIRM", 38);
            Anchor((RectTransform)confirm.transform, new Vector2(0.28f, 0f), new Vector2(0f, 60f), new Vector2(330f, 110f), new Vector2(0.5f, 0f));
            confirm.image.color = new Color(0.6f, 0.18f, 0.3f);

            var cancel = EnsureButton(box.transform, "CancelButton", font, "CANCEL", 38);
            Anchor((RectTransform)cancel.transform, new Vector2(0.72f, 0f), new Vector2(0f, 60f), new Vector2(330f, 110f), new Vector2(0.5f, 0f));
            cancel.image.color = new Color(0.35f, 0.33f, 0.3f);

            // --- components + refs ---
            var dialogView = EnsureComponent<CosmicDialogView>(dialog);
            var dialogSerialized = new SerializedObject(dialogView);
            dialogSerialized.FindProperty("bodyText").objectReferenceValue = body;
            dialogSerialized.FindProperty("confirmButton").objectReferenceValue = confirm;
            dialogSerialized.FindProperty("cancelButton").objectReferenceValue = cancel;
            dialogSerialized.FindProperty("panelToClose").objectReferenceValue = panel;
            dialogSerialized.ApplyModifiedPropertiesWithoutUndo();

            var panelController = EnsureComponent<CosmicPanelController>(panel);
            var panelSerialized = new SerializedObject(panelController);
            panelSerialized.FindProperty("tcBalanceText").objectReferenceValue = tcBalance;
            panelSerialized.FindProperty("statusText").objectReferenceValue = status;
            panelSerialized.FindProperty("solveButton").objectReferenceValue = solveButton;
            panelSerialized.FindProperty("solveLabel").objectReferenceValue =
                solveButton.GetComponentInChildren<Text>(true);
            panelSerialized.FindProperty("confirmDialog").objectReferenceValue = dialog;
            panelSerialized.FindProperty("closeButton").objectReferenceValue = closeButton;
            var rowsProp = panelSerialized.FindProperty("rows");
            rowsProp.arraySize = rows.Length;
            for (var i = 0; i < rows.Length; i++)
            {
                rowsProp.GetArrayElementAtIndex(i).objectReferenceValue = rows[i];
            }

            panelSerialized.ApplyModifiedPropertiesWithoutUndo();

            var opener = EnsureComponent<PanelOpenButton>(cosmicButton.gameObject);
            var openerSerialized = new SerializedObject(opener);
            openerSerialized.FindProperty("target").objectReferenceValue = panel;
            openerSerialized.ApplyModifiedPropertiesWithoutUndo();

            var buttonView = EnsureComponent<CosmicButtonView>(hud);
            var buttonViewSerialized = new SerializedObject(buttonView);
            buttonViewSerialized.FindProperty("buttonRoot").objectReferenceValue = cosmicButton.gameObject;
            buttonViewSerialized.ApplyModifiedPropertiesWithoutUndo();

            // --- sibling order: panels < storm < welcome < prestige dialog < cosmic dialog ---
            panel.transform.SetAsLastSibling();
            foreach (var name in new[] { "SandstormOverlay", "WelcomeBackDialog", "PrestigeDialog" })
            {
                var t = hud.transform.Find(name);
                if (t != null)
                {
                    t.SetAsLastSibling();
                }
            }

            dialog.transform.SetAsLastSibling();

            panel.SetActive(false);
            dialog.SetActive(false);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            log.AppendLine("Scene: managers, TC counter, COSMIC button, Archive panel + dialog wired and saved");
        }

        private static CosmicAltarRowView EnsureAltarRow(
            Transform panel, AltarUpgradeSO upgrade, int index, Font font)
        {
            var row = EnsureChild(panel, "AltarRow_" + upgrade.Id);
            var rt = (RectTransform)row.transform;
            var top = -310f - index * 185f;
            SetStretch(rt, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(20f, top - 170f), new Vector2(-20f, top));
            var bg = EnsureComponent<Image>(row);
            bg.color = new Color(0.13f, 0.11f, 0.2f, 0.92f);
            bg.raycastTarget = false;

            var nameText = EnsureText(row.transform, "NameText", font, 36, new Color(0.95f, 0.9f, 0.75f));
            Anchor(nameText.rectTransform, new Vector2(0f, 1f), new Vector2(25f, -18f), new Vector2(720f, 50f), new Vector2(0f, 1f));
            nameText.alignment = TextAnchor.MiddleLeft;
            nameText.text = upgrade.DisplayName + " — Lv 0";

            var effectText = EnsureText(row.transform, "EffectText", font, 30, new Color(0.75f, 0.85f, 0.95f));
            Anchor(effectText.rectTransform, new Vector2(0f, 0f), new Vector2(25f, 18f), new Vector2(720f, 45f), new Vector2(0f, 0f));
            effectText.alignment = TextAnchor.MiddleLeft;

            var buyButton = EnsureButton(row.transform, "BuyButton", font, "BUY\n0 TC", 30);
            Anchor((RectTransform)buyButton.transform, new Vector2(1f, 0.5f), new Vector2(-25f, 0f), new Vector2(220f, 130f), new Vector2(1f, 0.5f));
            buyButton.image.color = new Color(0.2f, 0.45f, 0.6f);

            var view = EnsureComponent<CosmicAltarRowView>(row);
            var serialized = new SerializedObject(view);
            serialized.FindProperty("upgrade").objectReferenceValue = upgrade;
            serialized.FindProperty("nameText").objectReferenceValue = nameText;
            serialized.FindProperty("effectText").objectReferenceValue = effectText;
            serialized.FindProperty("buyButton").objectReferenceValue = buyButton;
            serialized.FindProperty("buyLabel").objectReferenceValue =
                buyButton.GetComponentInChildren<Text>(true);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return view;
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
