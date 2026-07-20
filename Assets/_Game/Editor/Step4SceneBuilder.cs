using System;
using System.IO;
using System.Text;
using Crumble.Gameplay;
using Crumble.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Crumble.EditorTools
{
    /// <summary>
    /// One-shot, idempotent Step 4 setup: PrestigeManager on _Bootstrap, the HUD KP
    /// counter, the prestige button with live +KP preview, and the confirmation dialog.
    /// Writes a summary to Temp/crumble_step4_build.txt.
    /// </summary>
    public static class Step4SceneBuilder
    {
        private const string ResultsPath = "Temp/crumble_step4_build.txt";

        [MenuItem("Crumble/Build Step 4 Scene")]
        public static void Build()
        {
            var log = new StringBuilder();
            try
            {
                WireScene(log);
                log.AppendLine("RESULT: OK");
            }
            catch (Exception e)
            {
                log.AppendLine("RESULT: ERROR " + e);
                Debug.LogException(e);
            }

            File.WriteAllText(ResultsPath, log.ToString());
            Debug.Log("[Step4SceneBuilder]\n" + log);
        }

        private static void WireScene(StringBuilder log)
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

            EnsureComponent<PrestigeManager>(bootstrap);

            var hud = GameObject.Find("HUD");
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // --- KP counter, top-left ---
            var kpText = EnsureText(hud.transform, "KpText", font, 40, new Color(0.55f, 0.75f, 1f));
            Anchor(kpText.rectTransform, new Vector2(0f, 1f), new Vector2(30f, -30f), new Vector2(420f, 60f));
            kpText.alignment = TextAnchor.MiddleLeft;
            kpText.text = "KP 0";

            // --- prestige button, top-right ---
            var prestigeButton = EnsureButton(hud.transform, "PrestigeButton", font, "PRESTIGE\n+0 KP", 30);
            Anchor((RectTransform)prestigeButton.transform, new Vector2(1f, 1f), new Vector2(-20f, -20f), new Vector2(250f, 130f));
            prestigeButton.image.color = new Color(0.5f, 0.32f, 0.65f);

            // --- confirmation dialog (initially hidden, rendered above everything) ---
            var dialog = EnsureChild(hud.transform, "PrestigeDialog");
            dialog.transform.SetAsLastSibling();
            var dialogRt = (RectTransform)dialog.transform;
            dialogRt.anchorMin = Vector2.zero;
            dialogRt.anchorMax = Vector2.one;
            dialogRt.offsetMin = Vector2.zero;
            dialogRt.offsetMax = Vector2.zero;
            var dim = EnsureComponent<Image>(dialog);
            dim.color = new Color(0f, 0f, 0f, 0.78f);
            dim.raycastTarget = true; // swallow everything behind the dialog

            var box = EnsureImage(dialog.transform, "DialogBox", new Color(0.13f, 0.11f, 0.09f, 0.98f));
            Anchor(box.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(920f, 700f));

            var title = EnsureText(box.transform, "TitleText", font, 58, new Color(0.85f, 0.65f, 0.25f));
            Anchor(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(800f, 80f));
            title.text = "PRESTIGE";

            var body = EnsureText(box.transform, "BodyText", font, 34, new Color(0.92f, 0.9f, 0.85f));
            SetStretch(body.rectTransform, new Vector2(0.06f, 0.28f), new Vector2(0.94f, 0.82f), Vector2.zero, Vector2.zero);
            body.horizontalOverflow = HorizontalWrapMode.Wrap;
            body.alignment = TextAnchor.MiddleCenter;

            var confirm = EnsureButton(box.transform, "ConfirmButton", font, "CONFIRM", 38);
            Anchor((RectTransform)confirm.transform, new Vector2(0.28f, 0f), new Vector2(0f, 60f), new Vector2(330f, 110f), new Vector2(0.5f, 0f));
            confirm.image.color = new Color(0.28f, 0.55f, 0.28f);

            var cancel = EnsureButton(box.transform, "CancelButton", font, "CANCEL", 38);
            Anchor((RectTransform)cancel.transform, new Vector2(0.72f, 0f), new Vector2(0f, 60f), new Vector2(330f, 110f), new Vector2(0.5f, 0f));
            cancel.image.color = new Color(0.35f, 0.33f, 0.3f);

            // --- components + refs ---
            var dialogView = EnsureComponent<PrestigeDialogView>(dialog);
            var dialogSerialized = new SerializedObject(dialogView);
            dialogSerialized.FindProperty("bodyText").objectReferenceValue = body;
            dialogSerialized.FindProperty("confirmButton").objectReferenceValue = confirm;
            dialogSerialized.FindProperty("cancelButton").objectReferenceValue = cancel;
            dialogSerialized.ApplyModifiedPropertiesWithoutUndo();

            var buttonView = EnsureComponent<PrestigeButtonView>(prestigeButton.gameObject);
            var buttonSerialized = new SerializedObject(buttonView);
            buttonSerialized.FindProperty("button").objectReferenceValue = prestigeButton;
            buttonSerialized.FindProperty("label").objectReferenceValue =
                prestigeButton.GetComponentInChildren<Text>(true);
            buttonSerialized.FindProperty("dialog").objectReferenceValue = dialog;
            buttonSerialized.ApplyModifiedPropertiesWithoutUndo();

            var hudController = hud.GetComponent<HudController>();
            var hudSerialized = new SerializedObject(hudController);
            hudSerialized.FindProperty("kpText").objectReferenceValue = kpText;
            hudSerialized.ApplyModifiedPropertiesWithoutUndo();

            dialog.SetActive(false);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            log.AppendLine("Scene: PrestigeManager, KP counter, prestige button + dialog wired and saved");
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
