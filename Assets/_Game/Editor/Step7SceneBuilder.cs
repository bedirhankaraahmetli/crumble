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
    /// One-shot, idempotent Step 7 (juice) setup: FeverManager on _Bootstrap, CameraShake
    /// on the main camera, tap-dust + shatter-burst particle systems under the Tablet
    /// (manual Emit only — never instantiated per event), and the fever combo bar in the
    /// HUD. Writes a summary to Temp/crumble_step7_build.txt.
    /// </summary>
    public static class Step7SceneBuilder
    {
        private const string ResultsPath = "Temp/crumble_step7_build.txt";
        private const string ParticleMatPath = "Assets/_Game/Art/ParticleSprites.mat";

        [MenuItem("Crumble/Build Step 7 Scene")]
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
            Debug.Log("[Step7SceneBuilder]\n" + log);
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

            EnsureComponent<FeverManager>(bootstrap);

            var camera = GameObject.Find("Main Camera");
            EnsureComponent<CameraShake>(camera);

            // --- particle material (Sprites/Default renders vertex colors fine in URP) ---
            var mat = AssetDatabase.LoadAssetAtPath<Material>(ParticleMatPath);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Sprites/Default"));
                AssetDatabase.CreateAsset(mat, ParticleMatPath);
            }

            // --- particle systems under the tablet ---
            var tablet = GameObject.Find("Tablet");
            var shatterBurst = EnsureParticles(tablet.transform, "ShatterBurst", mat,
                lifeMin: 0.5f, lifeMax: 0.9f, speedMin: 2.5f, speedMax: 6f,
                sizeMin: 0.12f, sizeMax: 0.3f, gravity: 1.4f, radius: 1.4f);
            var tapDust = EnsureParticles(tablet.transform, "TapDust", mat,
                lifeMin: 0.25f, lifeMax: 0.5f, speedMin: 0.8f, speedMax: 2f,
                sizeMin: 0.05f, sizeMax: 0.12f, gravity: 0.6f, radius: 1.1f);

            var view = tablet.GetComponent<TabletView>();
            var viewSerialized = new SerializedObject(view);
            viewSerialized.FindProperty("shatterBurst").objectReferenceValue = shatterBurst;
            viewSerialized.FindProperty("tapDust").objectReferenceValue = tapDust;
            viewSerialized.ApplyModifiedPropertiesWithoutUndo();

            // --- fever bar under the stats line ---
            var hud = GameObject.Find("HUD");
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var bar = EnsureChild(hud.transform, "FeverBar");
            var barRt = (RectTransform)bar.transform;
            barRt.anchorMin = new Vector2(0.5f, 1f);
            barRt.anchorMax = new Vector2(0.5f, 1f);
            barRt.pivot = new Vector2(0.5f, 1f);
            barRt.anchoredPosition = new Vector2(0f, -355f);
            barRt.sizeDelta = new Vector2(820f, 34f);
            var barBg = EnsureComponent<Image>(bar);
            barBg.color = new Color(0.12f, 0.1f, 0.08f, 0.9f);
            barBg.raycastTarget = false;

            var fill = EnsureImage(bar.transform, "Fill", new Color(1f, 0.62f, 0.15f));
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = new Vector2(0f, 1f);
            fill.rectTransform.offsetMin = Vector2.zero;
            fill.rectTransform.offsetMax = Vector2.zero;

            var label = EnsureText(bar.transform, "Label", font, 26, Color.white);
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = Vector2.zero;
            label.rectTransform.offsetMax = Vector2.zero;
            label.text = "";

            var barView = EnsureComponent<FeverBarView>(bar);
            var barSerialized = new SerializedObject(barView);
            barSerialized.FindProperty("fill").objectReferenceValue = fill.rectTransform;
            barSerialized.FindProperty("fillImage").objectReferenceValue = fill;
            barSerialized.FindProperty("label").objectReferenceValue = label;
            barSerialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            log.AppendLine("Scene: FeverManager, CameraShake, particles, fever bar wired and saved");
        }

        private static ParticleSystem EnsureParticles(
            Transform parent, string name, Material material,
            float lifeMin, float lifeMax, float speedMin, float speedMax,
            float sizeMin, float sizeMax, float gravity, float radius)
        {
            var existing = parent.Find(name);
            var go = existing != null ? existing.gameObject : new GameObject(name);
            if (existing == null)
            {
                go.transform.SetParent(parent, false);
                go.transform.localPosition = Vector3.zero;
            }

            var ps = EnsureComponent<ParticleSystem>(go);
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifeMin, lifeMax);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speedMin, speedMax);
            main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
            main.gravityModifier = gravity;
            main.maxParticles = 256;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.enabled = false; // manual Emit() only

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = radius;

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.sortingOrder = 5;

            return ps;
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
    }
}
