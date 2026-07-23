using System;
using System.IO;
using System.Text;
using Crumble.Gameplay;
using Crumble.Data;
using Crumble.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Crumble.EditorTools
{
    /// <summary>
    /// One-shot, idempotent Step 8 setup: 12 artifacts across 3 museum sets, 3 expedition
    /// missions, their placeholder icons, and the scene wiring for the museum panel,
    /// expedition tent panel, event toast, night tint and sandstorm overlay.
    /// Writes a summary to Temp/crumble_step8_build.txt.
    /// </summary>
    public static class Step8ContentBuilder
    {
        private const string ResultsPath = "Temp/crumble_step8_build.txt";
        private const string ArtDir = "Assets/_Game/Art/Placeholders/Artifacts";
        private const string ArtifactDataDir = "Assets/_Game/Data/Artifacts";
        private const string ExpeditionDataDir = "Assets/_Game/Data/Expeditions";
        private const int IconSize = 64;

        private struct SetDef
        {
            public string Id;
            public string Name;
            public MuseumBonusType Bonus;
            public double BonusAmount;
            public float Hue;
            public (string name, string desc)[] Members;
        }

        private static readonly SetDef[] Sets =
        {
            new SetDef
            {
                Id = "set_fossil_record", Name = "The Fossil Record",
                Bonus = MuseumBonusType.CoinMultiplier, BonusAmount = 0.25, Hue = 0.31f,
                Members = new[]
                {
                    ("Ancient Fern", "A frond pressed into stone before words existed."),
                    ("Trilobite Shell", "It scuttled across a seabed no map remembers."),
                    ("Petrified Feather", "Proof that something once sang up there."),
                    ("Dawn Fish", "Swam in the first warm oceans of the world."),
                },
            },
            new SetDef
            {
                Id = "set_amber_vault", Name = "The Amber Vault",
                Bonus = MuseumBonusType.ClickDamageMultiplier, BonusAmount = 0.25, Hue = 0.09f,
                Members = new[]
                {
                    ("Trapped Beetle", "Mid-step for forty million years."),
                    ("Honey Amber", "Light itself, slowed down and made solid."),
                    ("Sunstone Drop", "The oldest sunset ever preserved."),
                    ("Primal Resin", "The forest's first attempt at forever."),
                },
            },
            new SetDef
            {
                Id = "set_rosetta_codex", Name = "The Rosetta Codex",
                Bonus = MuseumBonusType.DpsMultiplier, BonusAmount = 0.25, Hue = 0.58f,
                Members = new[]
                {
                    ("First Fragment", "Three scripts, one voice, endless doors."),
                    ("Trade Ledger Shard", "Someone owed someone four goats. Immortalized."),
                    ("Star Map Piece", "They charted skies we can no longer see."),
                    ("Founder's Seal", "Pressed by the hand that raised the first camp."),
                },
            },
        };

        private struct ExpeditionDef
        {
            public string Id;
            public string Name;
            public string Desc;
            public double Hours;
            public double RewardSeconds;
            public double MinTabletMultiple;
            public float ArtifactChance;
        }

        private static readonly ExpeditionDef[] Expeditions =
        {
            new ExpeditionDef
            {
                Id = "expedition_short_scout", Name = "Short Scout",
                Desc = "A quick sweep of the nearby dunes.",
                Hours = 0.5, RewardSeconds = 1800, MinTabletMultiple = 15, ArtifactChance = 0.35f,
            },
            new ExpeditionDef
            {
                Id = "expedition_day_trip", Name = "Day Trip",
                Desc = "A full day at the ruined caravanserai.",
                Hours = 2, RewardSeconds = 9000, MinTabletMultiple = 25, ArtifactChance = 0.6f,
            },
            new ExpeditionDef
            {
                Id = "expedition_grand", Name = "Grand Expedition",
                Desc = "Deep into the valley no camel will enter.",
                Hours = 4, RewardSeconds = 21600, MinTabletMultiple = 40, ArtifactChance = 0.9f,
            },
        };

        [MenuItem("Crumble/Build Step 8 Content And Scene")]
        public static void Build()
        {
            var log = new StringBuilder();
            try
            {
                GenerateIcons(log);
                var artifacts = CreateArtifactAssets(log);
                var sets = CreateSetAssets(artifacts, log);
                var expeditions = CreateExpeditionAssets(log);
                WireScene(artifacts, sets, expeditions, log);
                log.AppendLine("RESULT: OK");
            }
            catch (Exception e)
            {
                log.AppendLine("RESULT: ERROR " + e);
                Debug.LogException(e);
            }

            File.WriteAllText(ResultsPath, log.ToString());
            Debug.Log("[Step8ContentBuilder]\n" + log);
        }

        private static string ArtifactId(string name)
        {
            return "artifact_" + name.ToLowerInvariant().Replace(" ", "_").Replace("'", "");
        }

        private static string IconPath(string artifactId) => $"{ArtDir}/{artifactId}.png";

        // ---------- icons ----------

        private static void GenerateIcons(StringBuilder log)
        {
            Directory.CreateDirectory(ArtDir);
            var count = 0;
            foreach (var set in Sets)
            {
                for (var i = 0; i < set.Members.Length; i++)
                {
                    var id = ArtifactId(set.Members[i].name);
                    var hue = (set.Hue + 0.03f * i) % 1f;
                    File.WriteAllBytes(IconPath(id), RenderIcon(hue, count * 17 + 3).EncodeToPNG());
                    count++;
                }
            }

            AssetDatabase.Refresh();
            foreach (var set in Sets)
            {
                foreach (var member in set.Members)
                {
                    var importer = (TextureImporter)AssetImporter.GetAtPath(IconPath(ArtifactId(member.name)));
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.filterMode = FilterMode.Point;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.mipmapEnabled = false;
                    importer.SaveAndReimport();
                }
            }

            log.AppendLine($"Icons: {count} artifact icons in {ArtDir}");
        }

        private static Texture2D RenderIcon(float hue, int seed)
        {
            var rng = new System.Random(seed);
            var tex = new Texture2D(IconSize, IconSize, TextureFormat.RGBA32, false);
            var fill = Color.HSVToRGB(hue, 0.55f, 0.92f);
            var border = Color.HSVToRGB(hue, 0.6f, 0.4f);
            var clear = new Color(0, 0, 0, 0);
            var center = IconSize / 2f;

            for (var y = 0; y < IconSize; y++)
            {
                for (var x = 0; x < IconSize; x++)
                {
                    // diamond silhouette — reads as "relic" against the square tool icons
                    var manhattan = Mathf.Abs(x - center) + Mathf.Abs(y - center);
                    if (manhattan > center - 4)
                    {
                        tex.SetPixel(x, y, clear);
                        continue;
                    }

                    var edge = manhattan > center - 9;
                    var noise = 1f + ((float)rng.NextDouble() - 0.5f) * 0.14f;
                    var c = edge ? border : fill * noise;
                    c.a = 1f;
                    tex.SetPixel(x, y, c);
                }
            }

            tex.Apply();
            return tex;
        }

        // ---------- assets ----------

        private static ArtifactSO[] CreateArtifactAssets(StringBuilder log)
        {
            Directory.CreateDirectory(ArtifactDataDir);
            var result = new System.Collections.Generic.List<ArtifactSO>();
            foreach (var set in Sets)
            {
                foreach (var (name, desc) in set.Members)
                {
                    var id = ArtifactId(name);
                    var path = $"{ArtifactDataDir}/{id}.asset";
                    var so = AssetDatabase.LoadAssetAtPath<ArtifactSO>(path);
                    var isNew = so == null;
                    if (isNew)
                    {
                        so = ScriptableObject.CreateInstance<ArtifactSO>();
                    }

                    so.Id = id;
                    so.DisplayName = name;
                    so.Description = desc;
                    so.DropWeight = 1;
                    so.Icon = AssetDatabase.LoadAssetAtPath<Sprite>(IconPath(id));

                    if (isNew)
                    {
                        AssetDatabase.CreateAsset(so, path);
                    }
                    else
                    {
                        EditorUtility.SetDirty(so);
                    }

                    result.Add(so);
                }
            }

            AssetDatabase.SaveAssets();
            log.AppendLine($"Artifacts: {result.Count} ArtifactSO assets");
            return result.ToArray();
        }

        private static MuseumSetSO[] CreateSetAssets(ArtifactSO[] artifacts, StringBuilder log)
        {
            var result = new MuseumSetSO[Sets.Length];
            for (var s = 0; s < Sets.Length; s++)
            {
                var def = Sets[s];
                var path = $"{ArtifactDataDir}/{def.Id}.asset";
                var so = AssetDatabase.LoadAssetAtPath<MuseumSetSO>(path);
                var isNew = so == null;
                if (isNew)
                {
                    so = ScriptableObject.CreateInstance<MuseumSetSO>();
                }

                so.Id = def.Id;
                so.DisplayName = def.Name;
                so.BonusType = def.Bonus;
                so.BonusAmount = def.BonusAmount;
                so.Artifacts = new ArtifactSO[def.Members.Length];
                for (var i = 0; i < def.Members.Length; i++)
                {
                    var id = ArtifactId(def.Members[i].name);
                    foreach (var artifact in artifacts)
                    {
                        if (artifact.Id == id)
                        {
                            so.Artifacts[i] = artifact;
                            break;
                        }
                    }
                }

                if (isNew)
                {
                    AssetDatabase.CreateAsset(so, path);
                }
                else
                {
                    EditorUtility.SetDirty(so);
                }

                result[s] = so;
            }

            AssetDatabase.SaveAssets();
            log.AppendLine($"Sets: {result.Length} MuseumSetSO assets");
            return result;
        }

        private static ExpeditionSO[] CreateExpeditionAssets(StringBuilder log)
        {
            Directory.CreateDirectory(ExpeditionDataDir);
            var result = new ExpeditionSO[Expeditions.Length];
            for (var i = 0; i < Expeditions.Length; i++)
            {
                var def = Expeditions[i];
                var path = $"{ExpeditionDataDir}/{def.Id}.asset";
                var so = AssetDatabase.LoadAssetAtPath<ExpeditionSO>(path);
                var isNew = so == null;
                if (isNew)
                {
                    so = ScriptableObject.CreateInstance<ExpeditionSO>();
                }

                so.Id = def.Id;
                so.DisplayName = def.Name;
                so.Description = def.Desc;
                so.BaseDurationHours = def.Hours;
                so.RewardDpsSeconds = def.RewardSeconds;
                so.MinRewardTabletMultiple = def.MinTabletMultiple;
                so.ArtifactChance = def.ArtifactChance;

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
            log.AppendLine($"Expeditions: {result.Length} ExpeditionSO assets");
            return result;
        }

        // ---------- scene ----------

        private static void WireScene(ArtifactSO[] artifacts, MuseumSetSO[] sets, ExpeditionSO[] expeditions, StringBuilder log)
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

            // --- managers + content arrays ---
            var museum = EnsureComponent<MuseumManager>(bootstrap);
            var museumSerialized = new SerializedObject(museum);
            FillArray(museumSerialized.FindProperty("artifacts"), artifacts);
            FillArray(museumSerialized.FindProperty("sets"), sets);
            museumSerialized.ApplyModifiedPropertiesWithoutUndo();

            var expeditionManager = EnsureComponent<ExpeditionManager>(bootstrap);
            var expeditionSerialized = new SerializedObject(expeditionManager);
            FillArray(expeditionSerialized.FindProperty("expeditions"), expeditions);
            expeditionSerialized.ApplyModifiedPropertiesWithoutUndo();

            EnsureComponent<EnvironmentManager>(bootstrap);
            EnsureComponent<SandstormManager>(bootstrap);

            var camera = GameObject.Find("Main Camera");
            EnsureComponent<NightTintView>(camera);

            var hud = GameObject.Find("HUD");
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // --- HUD buttons under RESEARCH ---
            var museumButton = EnsureButton(hud.transform, "MuseumButton", font, "MUSEUM", 30);
            Anchor((RectTransform)museumButton.transform, new Vector2(0f, 1f), new Vector2(20f, -195f), new Vector2(260f, 80f));
            museumButton.image.color = new Color(0.5f, 0.38f, 0.2f);

            var tentButton = EnsureButton(hud.transform, "TentButton", font, "TENT", 30);
            Anchor((RectTransform)tentButton.transform, new Vector2(0f, 1f), new Vector2(20f, -280f), new Vector2(260f, 80f));
            tentButton.image.color = new Color(0.35f, 0.45f, 0.25f);

            // --- event toast (under the fever bar) ---
            var toast = EnsureText(hud.transform, "EventToast", font, 34, new Color(1f, 0.85f, 0.4f));
            Anchor(toast.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -400f), new Vector2(980f, 50f));
            toast.text = "";
            var toastView = EnsureComponent<EventToastView>(hud);
            var toastSerialized = new SerializedObject(toastView);
            toastSerialized.FindProperty("toastText").objectReferenceValue = toast;
            toastSerialized.ApplyModifiedPropertiesWithoutUndo();

            // --- museum panel ---
            var museumPanel = BuildOverlayPanel(hud.transform, "MuseumPanel", font, "MUSEUM",
                out var museumContent, out var museumClose);
            var setHeaderTemplate = BuildSetHeaderTemplate(museumContent.transform, font);
            var artifactRowTemplate = BuildArtifactRowTemplate(museumContent.transform, font);

            var museumController = EnsureComponent<MuseumPanelController>(museumPanel);
            var museumControllerSerialized = new SerializedObject(museumController);
            museumControllerSerialized.FindProperty("content").objectReferenceValue = museumContent.transform;
            museumControllerSerialized.FindProperty("setHeaderTemplate").objectReferenceValue = setHeaderTemplate;
            museumControllerSerialized.FindProperty("artifactRowTemplate").objectReferenceValue = artifactRowTemplate;
            museumControllerSerialized.FindProperty("closeButton").objectReferenceValue = museumClose;
            museumControllerSerialized.ApplyModifiedPropertiesWithoutUndo();

            WireOpener(museumButton, museumPanel);

            // --- expedition panel ---
            var tentPanel = BuildOverlayPanel(hud.transform, "ExpeditionPanel", font, "EXPEDITION TENT",
                out var tentContent, out var tentClose);
            var expeditionRowTemplate = BuildExpeditionRowTemplate(tentContent.transform, font);

            var tentController = EnsureComponent<ExpeditionPanelController>(tentPanel);
            var tentControllerSerialized = new SerializedObject(tentController);
            tentControllerSerialized.FindProperty("content").objectReferenceValue = tentContent.transform;
            tentControllerSerialized.FindProperty("rowTemplate").objectReferenceValue = expeditionRowTemplate;
            tentControllerSerialized.FindProperty("closeButton").objectReferenceValue = tentClose;
            tentControllerSerialized.ApplyModifiedPropertiesWithoutUndo();

            WireOpener(tentButton, tentPanel);

            // --- sandstorm overlay ---
            var storm = EnsureChild(hud.transform, "SandstormOverlay");
            var stormRt = (RectTransform)storm.transform;
            stormRt.anchorMin = Vector2.zero;
            stormRt.anchorMax = Vector2.one;
            stormRt.offsetMin = Vector2.zero;
            stormRt.offsetMax = Vector2.zero;
            var dust = EnsureComponent<Image>(storm);
            dust.color = new Color(0.76f, 0.55f, 0.25f, 0.86f);
            dust.raycastTarget = true; // swallow taps; drags feed the swipe area
            EnsureComponent<SandstormSwipeArea>(storm);

            var stormLabel = EnsureText(storm.transform, "Label", font, 52, Color.white);
            Anchor(stormLabel.rectTransform, new Vector2(0.5f, 0.72f), Vector2.zero, new Vector2(950f, 140f));
            stormLabel.text = "SANDSTORM!\nSWIPE TO CLEAN!";

            var stormBarBg = EnsureImage(storm.transform, "ProgressBg", new Color(0.1f, 0.08f, 0.05f, 0.9f));
            Anchor(stormBarBg.rectTransform, new Vector2(0.5f, 0.6f), Vector2.zero, new Vector2(760f, 36f));
            var stormFill = EnsureImage(stormBarBg.transform, "Fill", new Color(1f, 0.8f, 0.3f));
            stormFill.rectTransform.anchorMin = Vector2.zero;
            stormFill.rectTransform.anchorMax = new Vector2(0f, 1f);
            stormFill.rectTransform.offsetMin = Vector2.zero;
            stormFill.rectTransform.offsetMax = Vector2.zero;

            var stormView = EnsureComponent<SandstormOverlayView>(hud);
            var stormSerialized = new SerializedObject(stormView);
            stormSerialized.FindProperty("overlayRoot").objectReferenceValue = storm;
            stormSerialized.FindProperty("dustImage").objectReferenceValue = dust;
            stormSerialized.FindProperty("progressFill").objectReferenceValue = stormFill.rectTransform;
            stormSerialized.ApplyModifiedPropertiesWithoutUndo();

            storm.SetActive(false);

            // --- sibling order: panels < storm < welcome-back < prestige dialog ---
            museumPanel.transform.SetAsLastSibling();
            tentPanel.transform.SetAsLastSibling();
            storm.transform.SetAsLastSibling();
            var welcome = hud.transform.Find("WelcomeBackDialog");
            if (welcome != null)
            {
                welcome.SetAsLastSibling();
            }

            var prestige = hud.transform.Find("PrestigeDialog");
            if (prestige != null)
            {
                prestige.SetAsLastSibling();
            }

            museumPanel.SetActive(false);
            tentPanel.SetActive(false);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            log.AppendLine("Scene: managers, museum + tent panels, toast, night tint, sandstorm overlay wired and saved");
        }

        private static GameObject BuildOverlayPanel(
            Transform hud, string name, Font font, string title,
            out GameObject content, out Button closeButton)
        {
            var panel = EnsureChild(hud, name);
            var panelRt = (RectTransform)panel.transform;
            panelRt.anchorMin = Vector2.zero;
            panelRt.anchorMax = Vector2.one;
            panelRt.offsetMin = Vector2.zero;
            panelRt.offsetMax = Vector2.zero;
            var bg = EnsureComponent<Image>(panel);
            bg.color = new Color(0.07f, 0.06f, 0.05f, 0.985f);
            bg.raycastTarget = true;

            var titleText = EnsureText(panel.transform, "TitleText", font, 54, new Color(0.85f, 0.65f, 0.25f));
            Anchor(titleText.rectTransform, new Vector2(0f, 1f), new Vector2(30f, -30f), new Vector2(700f, 70f));
            titleText.alignment = TextAnchor.MiddleLeft;
            titleText.text = title;

            closeButton = EnsureButton(panel.transform, "CloseButton", font, "X", 44);
            Anchor((RectTransform)closeButton.transform, new Vector2(1f, 1f), new Vector2(-20f, -20f), new Vector2(110f, 90f));
            closeButton.image.color = new Color(0.5f, 0.2f, 0.18f);

            var scrollGo = EnsureChild(panel.transform, "ScrollView");
            var scrollRt = (RectTransform)scrollGo.transform;
            scrollRt.anchorMin = Vector2.zero;
            scrollRt.anchorMax = Vector2.one;
            scrollRt.offsetMin = new Vector2(10f, 15f);
            scrollRt.offsetMax = new Vector2(-10f, -130f);
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

            content = EnsureChild(viewport.transform, "Content");
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

            return panel;
        }

        private static GameObject BuildSetHeaderTemplate(Transform content, Font font)
        {
            var row = EnsureChild(content, "SetHeaderTemplate");
            var image = EnsureComponent<Image>(row);
            image.color = new Color(0.2f, 0.17f, 0.13f, 0.95f);
            image.raycastTarget = true;
            var element = EnsureComponent<LayoutElement>(row);
            element.preferredHeight = 120f;
            element.minHeight = 120f;

            var nameText = EnsureText(row.transform, "NameText", font, 40, new Color(0.95f, 0.85f, 0.6f));
            SetStretch(nameText.rectTransform, new Vector2(0.03f, 0.5f), new Vector2(0.75f, 1f), Vector2.zero, new Vector2(0f, -6f));
            nameText.alignment = TextAnchor.MiddleLeft;

            var bonusText = EnsureText(row.transform, "BonusText", font, 28, new Color(0.65f, 0.8f, 0.55f));
            SetStretch(bonusText.rectTransform, new Vector2(0.03f, 0f), new Vector2(0.75f, 0.5f), new Vector2(0f, 8f), Vector2.zero);
            bonusText.alignment = TextAnchor.MiddleLeft;

            var progressText = EnsureText(row.transform, "ProgressText", font, 40, Color.white);
            SetStretch(progressText.rectTransform, new Vector2(0.78f, 0f), new Vector2(0.98f, 1f), Vector2.zero, Vector2.zero);

            var view = EnsureComponent<MuseumSetHeaderView>(row);
            var serialized = new SerializedObject(view);
            serialized.FindProperty("background").objectReferenceValue = image;
            serialized.FindProperty("nameText").objectReferenceValue = nameText;
            serialized.FindProperty("bonusText").objectReferenceValue = bonusText;
            serialized.FindProperty("progressText").objectReferenceValue = progressText;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            row.SetActive(false);
            return row;
        }

        private static GameObject BuildArtifactRowTemplate(Transform content, Font font)
        {
            var row = EnsureChild(content, "ArtifactRowTemplate");
            var image = EnsureComponent<Image>(row);
            image.color = new Color(0.16f, 0.14f, 0.12f, 0.92f);
            image.raycastTarget = true;
            EnsureComponent<CanvasGroup>(row);
            var element = EnsureComponent<LayoutElement>(row);
            element.preferredHeight = 140f;
            element.minHeight = 140f;

            var icon = EnsureImage(row.transform, "Icon", Color.white);
            Anchor(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(24f, 0f), new Vector2(96f, 96f), new Vector2(0f, 0.5f));

            var nameText = EnsureText(row.transform, "NameText", font, 36, Color.white);
            SetStretch(nameText.rectTransform, new Vector2(0.13f, 0.5f), new Vector2(0.82f, 1f), Vector2.zero, new Vector2(0f, -6f));
            nameText.alignment = TextAnchor.MiddleLeft;

            var descText = EnsureText(row.transform, "DescText", font, 25, new Color(0.75f, 0.72f, 0.65f));
            SetStretch(descText.rectTransform, new Vector2(0.13f, 0f), new Vector2(0.82f, 0.5f), new Vector2(0f, 8f), Vector2.zero);
            descText.alignment = TextAnchor.MiddleLeft;
            descText.horizontalOverflow = HorizontalWrapMode.Wrap;

            var countText = EnsureText(row.transform, "CountText", font, 34, new Color(0.9f, 0.88f, 0.8f));
            SetStretch(countText.rectTransform, new Vector2(0.84f, 0f), new Vector2(0.98f, 1f), Vector2.zero, Vector2.zero);

            var view = EnsureComponent<MuseumArtifactRowView>(row);
            var serialized = new SerializedObject(view);
            serialized.FindProperty("canvasGroup").objectReferenceValue = row.GetComponent<CanvasGroup>();
            serialized.FindProperty("icon").objectReferenceValue = icon;
            serialized.FindProperty("nameText").objectReferenceValue = nameText;
            serialized.FindProperty("descText").objectReferenceValue = descText;
            serialized.FindProperty("countText").objectReferenceValue = countText;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            row.SetActive(false);
            return row;
        }

        private static GameObject BuildExpeditionRowTemplate(Transform content, Font font)
        {
            var row = EnsureChild(content, "ExpeditionRowTemplate");
            var image = EnsureComponent<Image>(row);
            image.color = new Color(0.16f, 0.14f, 0.12f, 0.92f);
            image.raycastTarget = true;
            var element = EnsureComponent<LayoutElement>(row);
            element.preferredHeight = 190f;
            element.minHeight = 190f;

            var nameText = EnsureText(row.transform, "NameText", font, 40, Color.white);
            SetStretch(nameText.rectTransform, new Vector2(0.03f, 0.62f), new Vector2(0.68f, 1f), Vector2.zero, new Vector2(0f, -8f));
            nameText.alignment = TextAnchor.MiddleLeft;

            var infoText = EnsureText(row.transform, "InfoText", font, 28, new Color(0.75f, 0.72f, 0.65f));
            SetStretch(infoText.rectTransform, new Vector2(0.03f, 0.32f), new Vector2(0.68f, 0.62f), Vector2.zero, Vector2.zero);
            infoText.alignment = TextAnchor.MiddleLeft;

            var rewardText = EnsureText(row.transform, "RewardText", font, 30, new Color(0.65f, 0.8f, 0.55f));
            SetStretch(rewardText.rectTransform, new Vector2(0.03f, 0f), new Vector2(0.68f, 0.32f), new Vector2(0f, 8f), Vector2.zero);
            rewardText.alignment = TextAnchor.MiddleLeft;

            var actionButton = EnsureButton(row.transform, "ActionButton", font, "START", 32);
            SetStretch((RectTransform)actionButton.transform, new Vector2(0.70f, 0.15f), new Vector2(0.98f, 0.85f), Vector2.zero, Vector2.zero);
            actionButton.image.color = new Color(0.35f, 0.45f, 0.25f);

            var view = EnsureComponent<ExpeditionRowView>(row);
            var serialized = new SerializedObject(view);
            serialized.FindProperty("nameText").objectReferenceValue = nameText;
            serialized.FindProperty("infoText").objectReferenceValue = infoText;
            serialized.FindProperty("rewardText").objectReferenceValue = rewardText;
            serialized.FindProperty("actionButton").objectReferenceValue = actionButton;
            serialized.FindProperty("actionLabel").objectReferenceValue = actionButton.GetComponentInChildren<Text>(true);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            row.SetActive(false);
            return row;
        }

        private static void WireOpener(Button button, GameObject panel)
        {
            var opener = EnsureComponent<PanelOpenButton>(button.gameObject);
            var serialized = new SerializedObject(opener);
            serialized.FindProperty("target").objectReferenceValue = panel;
            serialized.ApplyModifiedPropertiesWithoutUndo();
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
