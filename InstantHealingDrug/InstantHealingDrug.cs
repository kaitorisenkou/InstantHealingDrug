using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Reflection;
using System.Reflection.Emit;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using HarmonyLib;
using RimWorld;

namespace InstantHealingDrug {
    [StaticConstructorOnStartup]
    public class InstantHealingDrug {
        static InstantHealingDrug() {
            Log.Message("[InstantHealingDrug] Now active");
            var harmony = new Harmony("kaitorisenkou.InstantHealingDrug");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            Log.Message("[InstantHealingDrug] Harmony patch complete!");
        }
    }

    [HarmonyPatch(typeof(JobGiver_TakeCombatEnhancingDrug), "TryGiveJob")]
    public static class TCED_TryGiveJob_Patch {
        [HarmonyPrefix]
        static void Prefix(Pawn pawn, ref bool __state, ref bool ___onlyIfInDanger, ref Job __result) {
            __state = ___onlyIfInDanger;
            Thing thing = pawn.inventory.FindCombatEnhancingDrug();
            if (thing == null) return;
            CompDrug compDrug = thing.TryGetComp<CompDrug>();
            if (compDrug == null) return;
            if (compDrug.Props is CompProperties_DrugInstantHeal)
                ___onlyIfInDanger = true;

        }
        [HarmonyPostfix]
        static void Postfix(ref bool __state, ref bool ___onlyIfInDanger) {
            Log.Message("[IHD]" + __state);
            ___onlyIfInDanger = __state;
        }
    }
}
