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
    /// One-shot, idempotent Step 6 setup: OfflineProgressManager + AdManager on
    /// _Bootstrap, and the welcome-back popup (COLLECT / COLLECT x2 via rewarded ad).
    /// Writes a summary to Temp/crumble_step6_build.txt.
    /// </summary>
    public static class Step6SceneBuilder
    {
        private const string ResultsPath = "Temp/crumble_step6_build.txt";

        [MenuItem("Crumble/Build Step 6 Scene")]
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
            Debug.Log("[Step6SceneBuilder]\n" + log);
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

            EnsureComponent<OfflineProgressManager>(bootstrap);
            EnsureComponent<AdManager>(bootstrap);

            var hud = GameObject.Find("HUD");
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // --- popup (inactive, topmost) ---
            var popup = EnsureChild(hud.transform, "WelcomeBackDialog");
            popup.transform.SetAsLastSibling();
            var popupRt = (RectTransform)popup.transform;
            popupRt.anchorMin = Vector2.zero;
            popupRt.anchorMax = Vector2.one;
            popupRt.offsetMin = Vector2.zero;
            popupRt.offsetMax = Vector2.zero;
            var dim = EnsureComponent<Image>(popup);
            dim.color = new Color(0f, 0f, 0f, 0.78f);
            dim.raycastTarget = true;

            var box = EnsureImage(popup.transform, "DialogBox", new Color(0.13f, 0.11f, 0.09f, 0.98f));
            Anchor(box.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(920f, 720f));

            var title = EnsureText(box.transform, "TitleText", font, 56, new Color(0.85f, 0.65f, 0.25f));
            Anchor(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(840f, 80f));
            title.text = "WELCOME BACK";

            var body = EnsureText(box.transform, "BodyText", font, 36, new Color(0.92f, 0.9f, 0.85f));
            SetStretch(body.rectTransform, new Vector2(0.06f, 0.32f), new Vector2(0.94f, 0.82f), Vector2.zero, Vector2.zero);
            body.horizontalOverflow = HorizontalWrapMode.Wrap;
            body.alignment = TextAnchor.MiddleCenter;

            var collect = EnsureButton(box.transform, "CollectButton", font, "COLLECT", 36);
            Anchor((RectTransform)collect.transform, new Vector2(0.28f, 0f), new Vector2(0f, 60f), new Vector2(340f, 130f), new Vector2(0.5f, 0f));
            collect.image.color = new Color(0.28f, 0.55f, 0.28f);

            var doubleCollect = EnsureButton(box.transform, "DoubleButton", font, "COLLECT x2\nWATCH AD", 30);
            Anchor((RectTransform)doubleCollect.transform, new Vector2(0.72f, 0f), new Vector2(0f, 60f), new Vector2(340f, 130f), new Vector2(0.5f, 0f));
            doubleCollect.image.color = new Color(0.75f, 0.55f, 0.12f);

            // --- listener on the always-active HUD root ---
            var popupComponent = EnsureComponent<WelcomeBackPopup>(hud);
            var serialized = new SerializedObject(popupComponent);
            serialized.FindProperty("root").objectReferenceValue = popup;
            serialized.FindProperty("bodyText").objectReferenceValue = body;
            serialized.FindProperty("collectButton").objectReferenceValue = collect;
            serialized.FindProperty("doubleButton").objectReferenceValue = doubleCollect;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            popup.SetActive(false);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            log.AppendLine("Scene: OfflineProgressManager, AdManager, welcome-back popup wired and saved");
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
