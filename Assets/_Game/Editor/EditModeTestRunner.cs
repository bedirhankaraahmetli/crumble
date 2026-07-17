using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace Crumble.EditorTools
{
    /// <summary>
    /// Runs the EditMode test suite and writes a plain-text summary to
    /// Temp/crumble_test_results.txt so external tooling (CI, MCP) can read it.
    /// Also available manually via the "Crumble/Run EditMode Tests" menu.
    /// </summary>
    public static class EditModeTestRunner
    {
        public const string ResultsPath = "Temp/crumble_test_results.txt";

        // Held statically so the api instance and callbacks survive until the run finishes.
        private static TestRunnerApi s_api;

        private sealed class ResultCollector : ICallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun) { }

            public void RunFinished(ITestResultAdaptor result)
            {
                var sb = new StringBuilder();
                sb.AppendLine("STATUS: " + result.TestStatus);
                sb.AppendLine("PASSED: " + result.PassCount);
                sb.AppendLine("FAILED: " + result.FailCount);
                sb.AppendLine("SKIPPED: " + result.SkipCount);
                Collect(result, sb);
                File.WriteAllText(ResultsPath, sb.ToString());
                Debug.Log($"[EditModeTestRunner] {result.PassCount} passed, {result.FailCount} failed.");
            }

            private static void Collect(ITestResultAdaptor node, StringBuilder sb)
            {
                if (!node.HasChildren && node.TestStatus == TestStatus.Failed)
                {
                    sb.AppendLine("FAIL: " + node.FullName);
                    sb.AppendLine("  " + node.Message);
                }

                if (node.Children == null)
                {
                    return;
                }

                foreach (var child in node.Children)
                {
                    Collect(child, sb);
                }
            }

            public void TestStarted(ITestAdaptor test) { }
            public void TestFinished(ITestResultAdaptor result) { }
        }

        [MenuItem("Crumble/Run EditMode Tests")]
        public static void Run()
        {
            if (File.Exists(ResultsPath))
            {
                File.Delete(ResultsPath);
            }

            s_api = ScriptableObject.CreateInstance<TestRunnerApi>();
            s_api.RegisterCallbacks(new ResultCollector());
            s_api.Execute(new ExecutionSettings(new Filter { testMode = TestMode.EditMode }));
        }
    }
}
