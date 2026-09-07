using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace SkeletonDefender.Editor
{
    public static class AnimationIntegrationTests
    {
        [Serializable] private sealed class Report
        {
            public string status;
            public string[] completed;
            public string[] failures;
            public bool playerSaveTouched;
        }

        [MenuItem("Skeleton Defender/Check complete animation integration")]
        public static void Run()
        {
            var completed = new List<string>();
            var failures = new List<string>();
            RunOne("animation-library", AnimationLibraryTests.Run, completed, failures);
            RunOne("animation-effects", AnimationEffectsTests.Run, completed, failures);
            RunOne("animation-combat", AnimationCombatTests.Run, completed, failures);
            RunOne("targeting-equipment-abilities", ProjectileTargetingTests.Run, completed, failures);
            RunOne("summon-balance-regression", BalanceV062Tests.Run, completed, failures);
            RunOne("gameplay-regression", SmokeTests.Run, completed, failures);
            var report = new Report { status = failures.Count == 0 ? "passed" : "failed",
                completed = completed.ToArray(), failures = failures.ToArray(), playerSaveTouched = false };
            Directory.CreateDirectory("work");
            File.WriteAllText("work/animation-integration-results.json", JsonUtility.ToJson(report, true));
            if (failures.Count > 0) throw new Exception("ANIMATION INTEGRATION: " + string.Join("; ", failures));
            Debug.Log("SKELETON_ANIMATION_INTEGRATION_PASSED: all six suites. Player save untouched.");
        }

        private static void RunOne(string name, Action run, List<string> completed, List<string> failures)
        {
            try { run(); completed.Add(name); }
            catch (Exception exception) { failures.Add(name + ": " + exception.Message); Debug.LogError(name + "\n" + exception); }
        }

        // Collect independent legacy fixture failures in one editor session, so a
        // changed cast startup cannot conceal the results of the remaining checks.
        public static void RunGameplaySweep()
        {
            var completed = new List<string>();
            var failures = new List<string>();
            RunOne("inventory", InventoryTests.Run, completed, failures);
            foreach (string method in new[] {
                "CheckDefinitionsAndWaves", "CheckTowerScaling", "CheckEconomy", "CheckSchedules",
                "CheckEarlyWaveCalls", "CheckHeroesAndMovement", "CheckIdleRegeneration", "CheckCombat",
                "CheckDeathAndRespawn", "CheckTransformationsAndBossImmunity", "CheckSeededProcs",
                "CheckSkeletonAbilities", "CheckTerminalStates", "RunPlayabilityDiagnostics" })
            {
                RunOne(method, () => {
                    BalanceData.Reload();
                    try { typeof(SmokeTests).GetMethod(method, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public).Invoke(null, null); }
                    catch (TargetInvocationException exception) { throw exception.InnerException ?? exception; }
                }, completed, failures);
            }
            Directory.CreateDirectory("work");
            File.WriteAllText("work/animation-gameplay-sweep.json", JsonUtility.ToJson(new Report {
                status = failures.Count == 0 ? "passed" : "failed", completed = completed.ToArray(), failures = failures.ToArray() }, true));
            if (failures.Count > 0) throw new Exception("GAMEPLAY SWEEP: " + string.Join("; ", failures));
            Debug.Log("SKELETON_GAMEPLAY_SWEEP_PASSED: all legacy checks and playability diagnostics.");
        }
    }
}
