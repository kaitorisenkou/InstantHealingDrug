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
        public static Type VerbSelfHediffType;
        public static Type VerbSHPropType;
        public static FieldInfo VSH_inDangerField;

        static InstantHealingDrug() {
            Log.Message("[InstantHealingDrug] Now active");
            var harmony = new Harmony("kaitorisenkou.InstantHealingDrug");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            Log.Message("[InstantHealingDrug] Harmony patch complete!");

            VerbSelfHediffType = AccessTools.TypeByName("SelfHediffVerb.Verb_SelfHediff");
            if (VerbSelfHediffType != null) {
                Log.Message("[InstantHealingDrug] SelfHediffVerb found");
                VerbSHPropType = AccessTools.TypeByName("SelfHediffVerb.VerbProperties_SelfHediff");
                VSH_inDangerField = AccessTools.Field(VerbSHPropType, "inDanger");
            } else {
                Log.Message("[InstantHealingDrug] SelfHediffVerb NOT found");
            }
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
        static void Postfix(Pawn pawn, ref bool __state, ref bool ___onlyIfInDanger, ref Job __result) {
            //Log.Message("[IHD]" + __state);
            ___onlyIfInDanger = __state;
            if (__result != null ||
                InstantHealingDrug.VerbSelfHediffType == null ||
                pawn == null ||
                pawn.VerbTracker == null ||
                pawn.VerbTracker.AllVerbs == null)
                return;

            bool harm = Find.TickManager.TicksGame - pawn.mindState.lastHarmTick > 2500 || Find.TickManager.TicksGame - pawn.mindState.lastTakeCombatEnhancingDrugTick < 20000;
            if (harm) return;

            IEnumerable<Verb> equipmentVerbs = pawn.equipment.AllEquipmentVerbs.Concat(pawn.apparel.AllApparelVerbs);

            //Log.Message(string.Join(",", equipmentVerbs.Select(t => t.GetType().ToString())));

            var selfHediffVerb = equipmentVerbs.FirstOrDefault(
                t => 
                InstantHealingDrug.VerbSelfHediffType.IsAssignableFrom(t.GetType()) && 
                (bool)InstantHealingDrug.VSH_inDangerField.GetValue(t.verbProps)
                );
            if (selfHediffVerb == null) {
                return;
            }

            Job job = JobMaker.MakeJob(JobDefOf.UseVerbOnThingStatic, pawn);
            job.verbToUse = selfHediffVerb;
            __result = job;
            pawn.mindState.lastTakeCombatEnhancingDrugTick = Find.TickManager.TicksGame;
        }
    }
}
