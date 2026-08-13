using System;
using UnityEngine;

namespace SemiconCity.Game
{
    public static class SemiconCampaignAccess
    {
        public static bool IsUnlocked(int processNumber)
        {
            if (IsAutomatedTest()) return true;
            var state = SemiconGameState.Instance;
            return state != null && state.IsProcessUnlocked(processNumber);
        }

        public static string GetLockedPrompt(int processNumber, string processName)
        {
            return $"[LOCKED]  {processName}\n이전 공정품을 생산하면 {processNumber:00} 공정이 개방됩니다.";
        }

        public static void ShowLockedToast(int processNumber, string processName)
        {
            var hud = UnityEngine.Object.FindFirstObjectByType<SemiconHud>();
            hud?.ShowToast($"{processNumber:00} {processName} 잠금  ·  이전 공정품을 먼저 생산하세요.", 3f);
        }

        private static bool IsAutomatedTest()
        {
            return Array.Exists(Environment.GetCommandLineArgs(), argument =>
                argument.StartsWith("--semicon-", StringComparison.Ordinal) &&
                argument.EndsWith("-smoke-test", StringComparison.Ordinal));
        }
    }
}
