using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace SkeletonDefender.Editor
{
    // Small isolated subsystem fixtures; the comparison runner separately uses legal player actions.
    public static class BalanceV062Tests
    {
        private static void Check(bool condition, string message)
        { if (!condition) throw new InvalidOperationException("V062 CHECK: " + message); }
        private static object Call(GameModel model, string name, params object[] args) =>
            typeof(GameModel).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(model, args);
        private static List<Enemy> Pending(GameModel model) => (List<Enemy>)typeof(GameModel)
            .GetField("pendingSpawns", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(model);
        private static Enemy Owner(GameModel model)
        {
            Enemy owner = model.CreateEnemy(SkeletonKind.Tutankhamun, 200);
            model.Enemies.Add(owner); return owner;
        }

        // This suite checks summoning limits and ownership at the authored release;
        // AnimationCombatTests separately verifies real fixed-tick anticipation and pause.
        private static void ResolveAbility(GameModel model, Enemy owner, float elapsed)
        {
            if (owner.Animation.IsBusy) owner.Animation.Advance(owner.Animation.Duration);
            Call(model, "UpdateEnemyAbility", owner, elapsed);
            if (model.PendingAnimationContacts > 0)
            {
                owner.Animation.Advance(owner.Animation.Duration);
                Call(model, "UpdateAnimationContacts", 0f);
            }
        }

        [MenuItem("Skeleton Defender/Check bounded summoning (0.6.2)")]
        public static void Run()
        {
            BalanceData.Reload();
            SkeletonDefinition rule = BalanceData.Current.Skeleton((int)SkeletonKind.Tutankhamun);
            SkeletonDefinition sarco = BalanceData.Current.Skeleton((int)SkeletonKind.Sarcophagus);
            float oldInterval = rule.summonInterval, oldHp = rule.summonedHp, oldSarcoHp = sarco.summonedHp;
            int oldLimit = rule.summonLimit, oldAlive = rule.summonMaxAlive;
            try
            {
                rule.summonInterval = 3; rule.summonLimit = 2; rule.summonMaxAlive = 1; rule.summonedHp = 12;
                var model = new GameModel(); model.StartWave();
                Enemy firstOwner = Owner(model);
                ResolveAbility(model, firstOwner, 2.9f);
                Check(firstOwner.SummonsCreated == 0 && Pending(model).Count == 0, "First summon came before its interval");
                ResolveAbility(model, firstOwner, .11f);
                Check(firstOwner.SummonsCreated == 1 && model.LivingSummons(firstOwner.Id) == 1 && Pending(model).Count == 1,
                    "First child was not counted while pending");
                Enemy first = Pending(model)[0];
                Check(first.SummonerId == firstOwner.Id && first.IsSummoned && first.SpawnOrdinal == 0 && first.WaveNumber == firstOwner.WaveNumber &&
                    Mathf.Abs(first.Hp - 12) < .001f && Mathf.Abs(first.MaxHp - 12) < .001f,
                    "Summoner ownership, wave ownership or child HP override is wrong");
                ResolveAbility(model, firstOwner, 3f);
                Check(Pending(model).Count == 1 && firstOwner.SummonsCreated == 1 && Mathf.Abs(firstOwner.SpecialCooldown - 3) < .001f,
                    "Pending cap was ignored, consumed lifetime budget, or failed to restart the polling interval");
                Enemy secondOwner = Owner(model);
                ResolveAbility(model, secondOwner, 3f);
                Check(Pending(model).Count == 2 && model.LivingSummons(firstOwner.Id) == 1 && model.LivingSummons(secondOwner.Id) == 1,
                    "One summoner's cap blocked another summoner");
                Call(model, "FlushSpawns");
                first.FrogRemaining = 3;
                ResolveAbility(model, firstOwner, 3f);
                Check(firstOwner.SummonsCreated == 1, "A living frog stopped counting toward its parent's cap");
                Call(model, "Kill", first);
                Check(model.LivingSummons(firstOwner.Id) == 0 && model.LivingSummons(secondOwner.Id) == 1,
                    "Dead children or another parent's child were counted incorrectly");
                ResolveAbility(model, firstOwner, 2.9f);
                Check(firstOwner.SummonsCreated == 1, "Clearing a cap released an immediate accumulated summon");
                ResolveAbility(model, firstOwner, .11f);
                Check(firstOwner.SummonsCreated == 2 && Pending(model).Count == 1, "Summoning did not resume on the next scheduled interval");
                Enemy second = Pending(model)[0]; Call(model, "FlushSpawns"); Call(model, "Kill", second);
                ResolveAbility(model, firstOwner, 30f);
                Check(firstOwner.SummonsCreated == 2 && model.LivingSummons(firstOwner.Id) == 0,
                    "A released alive cap bypassed the lifetime budget");
                Enemy orphan = model.Enemies.Find(enemy => enemy.SummonerId == secondOwner.Id && !enemy.Dead);
                Call(model, "Kill", secondOwner);
                Check(orphan != null && !orphan.Dead && model.Enemies.Contains(orphan), "Children disappeared when their summoner died");
                Check(Mathf.Abs(model.CreateEnemy(SkeletonKind.Normal).MaxHp - BalanceData.Current.Skeleton((int)SkeletonKind.Normal).hp) < .001f,
                    "Mummy HP override changed original normal skeletons");

                // Rebirth shares wave ownership infrastructure, but is not a timed mummy summon.
                sarco.summonedHp = 2;
                Enemy bearer = model.CreateEnemy(SkeletonKind.Sarcophagus, 300); model.Enemies.Add(bearer);
                Call(model, "Kill", bearer);
                Enemy copy = Pending(model).Find(enemy => enemy.Skeleton == SkeletonKind.Sarcophagus);
                Check(copy != null && copy.MaxHp == sarco.hp && !copy.HasSarcophagus && copy.SummonerId == bearer.Id && bearer.SummonsCreated == 0,
                    "Timed-summon HP/budget rules incorrectly changed sarcophagus rebirth");

                rule.summonLimit = 0; rule.summonMaxAlive = 0; rule.summonedHp = 0;
                var legacy = new GameModel(); Enemy unlimited = Owner(legacy);
                for (int i = 0; i < 5; i++) ResolveAbility(legacy, unlimited, 3f);
                Check(unlimited.SummonsCreated == 5 && Pending(legacy).Count == 5 &&
                    Pending(legacy)[0].MaxHp == BalanceData.Current.Skeleton((int)SkeletonKind.Normal).hp,
                    "Zero-valued new fields did not preserve legacy unlimited summoning and inherited HP");
                Debug.Log("SKELETON_V062_CHECKS_PASSED: first interval, pending/alive/frog ownership, cap resume, lifetime budget, child HP, owner death, sarcophagus and legacy zero defaults.");
            }
            finally
            {
                rule.summonInterval = oldInterval; rule.summonLimit = oldLimit; rule.summonMaxAlive = oldAlive; rule.summonedHp = oldHp;
                sarco.summonedHp = oldSarcoHp;
            }
        }
    }
}
