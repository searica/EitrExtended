using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using static ItemDrop;

namespace EitrExtended.Patches;

[HarmonyPatch]
internal static class EitrRegenPatches
{

    private static readonly string[] tooltipTokens = ["$item_food_regen", "$item_food_duration", "$item_food_eitr", "$item_food_stamina"];

    /// <summary>
    ///     Transpiler to multiply eitrMultiplier value by extra eitr regen from this mod.
    /// </summary>
    /// <param name="instructions"></param>
    /// <returns></returns>
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(Player), nameof(Player.UpdateStats), typeof(float))]
    private static IEnumerable<CodeInstruction> Player_UpdateStats_EitrRegen(IEnumerable<CodeInstruction> instructions) 
    {
        // target this line: eitrMultiplier += GetEquipmentEitrRegenModifier();
        // IL_018c: ldloc.s 8
        // IL_018e: ldarg.0
        // IL_018f: call instance float32 Player::GetEquipmentEitrRegenModifier()
        // IL_0194: add
        // IL_0195: stloc.s 8
        CodeMatch[] matches = [
            new(OpCodes.Ldarg_0),
            new(OpCodes.Call, AccessTools.Method(typeof(Player), nameof(Player.GetEquipmentEitrRegenModifier))),
            new(OpCodes.Add),
        ];

        
        // insert call to get value of multiplier 
        CodeInstruction[] newCode = [
            new(OpCodes.Ldarg_0),
            Transpilers.EmitDelegate(GetEitrRegenMultiplier),
            new(OpCodes.Mul)
        ];

        return new CodeMatcher(instructions)
            .MatchEndForward(matches)
            .ThrowIfNotMatch("Failed to match in Player.UpdateStats!")
            .InsertAndAdvance(newCode)
            .ThrowIfInvalid("Failed to insert code in Player.UpdateStats!")
            .InstructionEnumeration();
    }

    /// <summary>
    ///     Calcualte Eitr regen multiplier from bonus eitr and flat increase.
    /// </summary>
    /// <param name="player"></param>
    /// <returns></returns>
    public static float GetEitrRegenMultiplier(Player player)
    {
        if (!EitrExtended.Instance.EnableEitrRegen.Value || !player)
        {
            return 1f;
        }

        float maxEitr = player.GetMaxEitr();
        if (!EitrExtended.Instance.ExtraEitrRegenFoodOnly.Value)
        {
            player.GetTotalFoodValue(out _, out _, out maxEitr);
        }
        float bonusEitr = maxEitr - BaseEitrPatches.GetBaseEitr(player);
        //float flatRegen = EitrExtended.Instance.ExtraEitrRegenFlat.Value / 100f;
        float extraRegen = EitrExtended.Instance.ExtraEitrRegen.Value * bonusEitr / 100f;
        return 1f + extraRegen;
    }

    private static float GetEitrRegenMultiplierForFood(float foodEitr)
    {
        return EitrExtended.Instance.ExtraEitrRegen.Value * foodEitr / 100f;
    }


    [HarmonyPostfix]
    [HarmonyPriority(Priority.VeryHigh)]
    [HarmonyAfter("shudnal.StaminaExtended")]
    [HarmonyBefore("shudnal.MyLittleUI")]
    [HarmonyPatch(typeof(ItemData), nameof(ItemData.GetTooltip), typeof(ItemData), typeof(int), typeof(bool), typeof(float), typeof(int))]
    private static void ItemDrop_ItemData_GetTooltip_EitrRegenTooltip_Postfix(ItemData item, ref string __result)
    {
        if (!EitrExtended.Instance.EnableEitrRegen.Value)
        {
            return; 
        }

        if (!IsEitrFood(item, out float foodEitr))
        {
            return;
        }
   
        int index = GetFoodToopTipStartIndex(__result);
        if (index == -1)
        {
            return;
        }

        string tooltip = String.Format(
            "\n$se_eitrregen: <color=#ffff80ff>{0:P1}</color> ($item_current:<color=yellow>{1:P1}</color>)",
            GetEitrRegenMultiplierForFood(foodEitr),
            GetEitrRegenMultiplier(Player.m_localPlayer)-1f
        );
        
        int i = __result.IndexOf("\n", index, StringComparison.InvariantCulture);
        if (i != -1)
        {
            __result.Insert(i, tooltip);
        }
        else
        {
            __result += tooltip;
        }
    }
    

    [HarmonyPatch(typeof(TextsDialog), nameof(TextsDialog.AddActiveEffects))]
    public static class TextsDialog_AddActiveEffects_SeasonTooltipWhenBuffDisabled
    {
        private static void Postfix(TextsDialog __instance)
        {
            if (!EitrExtended.Instance.EnableEitrRegen.Value || !Player.m_localPlayer)
            {
                return;
            }
             
            float multiplier = GetEitrRegenMultiplier(Player.m_localPlayer) - 1;
            if (multiplier < 0.01f)
            {
                return;
            }
                
            __instance.m_texts[0].m_text += Localization.instance.Localize(
                $"\n$se_eitrregen ({(EitrExtended.Instance.ExtraEitrRegenFoodOnly.Value ? "$item_food" : "$hud_misc")}): <color=orange>{multiplier:P1}</color>"
            );
        }
    }

    private static bool IsEitrFood(ItemData item, out float foodEitr)
    {
        foodEitr = 0f;
        if (!EitrExtended.Instance.EnableEitrRegen.Value || !Player.m_localPlayer)
        {
            return false;
        }
     
        if (item.m_shared.m_itemType == ItemData.ItemType.Consumable)
        {
            foodEitr = item.m_shared.m_foodEitr;
        }
            
        return foodEitr > 0f || item.m_shared.m_appendToolTip != null && IsEitrFood(item.m_shared.m_appendToolTip.m_itemData, out foodEitr);
    }

    private static int GetFoodToopTipStartIndex(string text)
    {
        int index = -1;
        foreach (string tailString in tooltipTokens)
        {
            index = text.IndexOf(tailString, StringComparison.InvariantCulture);
            if (index != -1)
            {
                break;
            }
        }

        return index;
    }

}


