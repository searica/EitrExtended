using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;
using Logging;
using Jotunn.Extensions;

namespace EitrExtended.Patches;

[HarmonyPatch]
internal static class BaseEitrPatches
{
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(Player), nameof(Player.GetTotalFoodValue))]
    private static IEnumerable<CodeInstruction> ChangeBaseEitrTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        // Target code
        // eitr = 0f;
        // IL_0010: ldarg.3
        // IL_0011: ldc.r4 0.0
        // IL_0016: stind.r4
        CodeMatch[] matches = [new(OpCodes.Ldc_R4, 0f)];
        CodeInstruction[] newCode = [
            new(OpCodes.Ldarg_0),
            new(OpCodes.Call, AccessTools.Method(typeof(BaseEitrPatches), nameof(GetBaseEitr)))
        ];

        return new CodeMatcher(instructions)
            .MatchStartForward(matches)
            .ThrowIfNotMatch("Failed to match code in ChangeBaseEitrTranspiler!")
            .RemoveInstruction()
            .InsertAndAdvance(newCode)
            .ThrowIfInvalid("Failed to insert code in ChangeBaseEitrTranspiler!")
            .InstructionEnumeration();
    }

    /// <summary>
    ///     Calculate base eitr 
    /// </summary>
    /// <param name="player"></param>
    /// <returns></returns>
    public static float GetBaseEitr(Player player)
    {
        if (!EitrExtended.Instance.EnableEitrBase.Value || !player || !player.GetSkills())
        {
            return 0f;
        }

        float baseEitr = 0f;
        if (TryGetSkillLevel(player, Skills.SkillType.BloodMagic, out float bloodMagic)) 
        {
            baseEitr += EitrExtended.Instance.BloodMagicBaseCoeff.Value * Mathf.Pow(bloodMagic, EitrExtended.Instance.BloodMagicBasePower.Value);
        }
        if (TryGetSkillLevel(player, Skills.SkillType.ElementalMagic, out float elementMagic))
        {
            baseEitr += EitrExtended.Instance.ElementMagicBaseCoeff.Value * Mathf.Pow(elementMagic, EitrExtended.Instance.ElementMagicBasePower.Value);
        }
        return Mathf.Floor(baseEitr);
    }

    private static bool TryGetSkillLevel(Player player, Skills.SkillType skillType, out float skillLevel)
    {
        if (player.GetSkills().m_skillData.ContainsKey(skillType))
        {
            skillLevel = player.GetSkillLevel(skillType);
            return true;
        }
        skillLevel = 0f;
        return false;
    }

    /// <summary>
    ///     Calls GetTotalFoodValue (which is patched by a transpiler)
    ///     and updates max eitr accordingly.
    /// </summary>
    /// <param name="player"></param>
    private static void UpdateBaseEitr(Player player)
    {
        if (!player) 
        {
            return; 
        }
        try
        {
            player.GetTotalFoodValue(out float _, out float _, out float eitr);
            player.SetMaxEitr(eitr, true);
        }
        catch (Exception e)
        {
            Log.LogWarning($"While updating base eitr caught exception {e}");
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Player), nameof(Player.Awake))]
    private static void Player_Awake_Postfix(Player __instance)
    {
        UpdateBaseEitr(__instance);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Player), nameof(Player.OnSkillLevelup))]
    private static void Player_OnSkillLevelUp_Postfix(
        Player __instance, 
        Skills.SkillType skill
    )
    {
        if (IsMagicSkill(skill))
        {
            UpdateBaseEitr(__instance);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Skills), nameof(Skills.Awake))]
    private static void Skills_Awake_Postfix(Skills __instance)
    {
        UpdateBaseEitr(Player.m_localPlayer);
    }


    [HarmonyPostfix]
    [HarmonyPatch(typeof(Terminal), nameof(Terminal.TryRunCommand))]
    private static void Terminal_TryRunCommand_Postfix(Terminal __instance, string text)
    {
        if (text.ToLower().ContainsAny("puke", "skill"))
        {
            UpdateBaseEitr(Player.m_localPlayer);
        }
    }

    private static bool IsMagicSkill(Skills.SkillType skillType)
    {
        return skillType is Skills.SkillType.ElementalMagic or Skills.SkillType.BloodMagic;
    }

}
