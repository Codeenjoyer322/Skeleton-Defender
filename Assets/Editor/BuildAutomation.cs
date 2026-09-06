using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace SkeletonDefender.Editor
{
    // A local editor bridge with a fixed command list. Request files are data, never code.
    [InitializeOnLoad]
    public static class BuildAutomation
    {
        [Serializable]
        private sealed class Request
        {
            public string id = "";
            public string command = "";
        }

        [Serializable]
        private sealed class Result
        {
            public string id;
            public string command;
            public string status;
            public string startedUtc;
            public string finishedUtc;
            public string[] errors;
        }

        [Serializable]
        private sealed class BalanceExport
        {
            public string exportedUtc;
            public BalanceData balance;
            public MapWaveExport[] waves;
        }

        [Serializable]
        private sealed class MapWaveExport
        {
            public int map;
            public int wave;
            public SpawnEntry[] entries;
        }

        private static readonly string WorkDirectory = Path.Combine(Path.GetDirectoryName(Application.dataPath), "work");
        private static readonly string RequestPath = Path.Combine(WorkDirectory, "unity-request.json");
        private static readonly string ResultPath = Path.Combine(WorkDirectory, "unity-result.json");
        private static string lastRequestText;
        private static double nextPoll;
        private static bool busy;

        static BuildAutomation() { EditorApplication.update += Poll; }

        private static void Poll()
        {
            if (busy || EditorApplication.timeSinceStartup < nextPoll || EditorApplication.isCompiling ||
                EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer) return;
            nextPoll = EditorApplication.timeSinceStartup + 1;
            if (!File.Exists(RequestPath)) return;

            string json;
            try { json = File.ReadAllText(RequestPath); }
            catch (IOException) { return; }
            if (json == lastRequestText) return;

            Request request;
            try { request = JsonUtility.FromJson<Request>(json); }
            catch (ArgumentException)
            {
                // An external writer may still be replacing the file; retry a changed document.
                lastRequestText = json;
                PublishInvalid("Request must contain valid JSON with id and command.");
                return;
            }
            if (request == null || string.IsNullOrWhiteSpace(request.id) || request.id.Length > 128)
            {
                lastRequestText = json;
                PublishInvalid("Request id must be a nonempty string no longer than 128 characters.");
                return;
            }

            string receiptDirectory = Path.Combine(WorkDirectory, "unity-requests");
            Directory.CreateDirectory(receiptDirectory);
            string receiptPath = Path.Combine(receiptDirectory, Hash(request.id) + ".json");
            if (File.Exists(receiptPath))
            {
                // Completed and interrupted requests are never replayed after a domain reload.
                lastRequestText = json;
                AtomicWrite(ResultPath, File.ReadAllText(receiptPath));
                return;
            }

            var result = new Result {
                id = request.id, command = request.command, status = "running",
                startedUtc = DateTime.UtcNow.ToString("O"), errors = Array.Empty<string>()
            };
            if (!Claim(receiptPath, JsonUtility.ToJson(result, true))) return;
            lastRequestText = json;
            busy = true;
            AtomicWrite(ResultPath, JsonUtility.ToJson(result, true));
            var errors = new List<string>();
            Application.LogCallback capture = (message, stack, kind) =>
            {
                if ((kind == LogType.Error || kind == LogType.Exception || kind == LogType.Assert) && errors.Count < 40)
                    errors.Add(message);
            };
            Application.logMessageReceived += capture;
            try
            {
                switch (request.command)
                {
                    case "compile":
                        CompilationPipeline.RequestScriptCompilation(RequestScriptCompilationOptions.CleanBuildCache);
                        nextPoll = EditorApplication.timeSinceStartup + 2;
                        break;
                    case "tests": SmokeTests.Run(); break;
                    case "build-all": BuildTools.BuildAll(); break;
                    case "build-web": BuildTools.BuildWeb(); break;
                    case "build-windows": BuildTools.BuildWindows(); break;
                    case "export-balance": ExportBalance(); break;
                    default: throw new ArgumentException("Unsupported command. Allowed: compile, tests, build-all, build-web, build-windows, export-balance.");
                }
                result.status = errors.Count > 0 ? "failed" : request.command == "compile" ? "accepted" : "succeeded";
            }
            catch (Exception exception)
            {
                result.status = "failed";
                errors.Add(exception.ToString());
                Debug.LogException(exception);
            }
            finally
            {
                Application.logMessageReceived -= capture;
                result.errors = errors.ToArray();
                result.finishedUtc = DateTime.UtcNow.ToString("O");
                string completed = JsonUtility.ToJson(result, true);
                try { AtomicWrite(receiptPath, completed); AtomicWrite(ResultPath, completed); }
                finally { busy = false; }
            }
        }

        public static void ExportBalance()
        {
            BalanceData.Reload();
            BalanceData balance = BalanceData.Current;
            var waves = new List<MapWaveExport>();
            for (int map = 1; map <= balance.mapCount; map++)
                for (int wave = 1; wave <= balance.wavesPerMap; wave++)
                    waves.Add(new MapWaveExport { map = map, wave = wave, entries = GameModel.BuildSpawnPlan(map, wave).ToArray() });
            var export = new BalanceExport { exportedUtc = DateTime.UtcNow.ToString("O"), balance = balance, waves = waves.ToArray() };
            Directory.CreateDirectory(WorkDirectory);
            AtomicWrite(Path.Combine(WorkDirectory, "balance-export.json"), JsonUtility.ToJson(export, true));
            Debug.Log("SKELETON_BALANCE_EXPORTED: " + waves.Count + " map/wave plans.");
        }

        private static bool Claim(string path, string json)
        {
            string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            File.WriteAllText(temporary, json, new UTF8Encoding(false));
            try { File.Move(temporary, path); return true; }
            catch (IOException) when (File.Exists(path)) { File.Delete(temporary); return false; }
        }

        private static void AtomicWrite(string path, string text)
        {
            string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            File.WriteAllText(temporary, text, new UTF8Encoding(false));
            if (File.Exists(path)) File.Replace(temporary, path, null);
            else File.Move(temporary, path);
        }

        private static string Hash(string id)
        {
            using (SHA256 hash = SHA256.Create())
                return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(id))).Replace("-", "").ToLowerInvariant();
        }

        private static void PublishInvalid(string error)
        {
            Directory.CreateDirectory(WorkDirectory);
            AtomicWrite(ResultPath, JsonUtility.ToJson(new Result {
                status = "failed", finishedUtc = DateTime.UtcNow.ToString("O"), errors = new[] { error }
            }, true));
        }
    }
}
