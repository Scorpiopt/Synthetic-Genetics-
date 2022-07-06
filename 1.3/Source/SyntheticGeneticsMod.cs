using UnityEngine;
using Verse;
using System.Linq;
using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using GeneticRim;

namespace SyntheticGenetics
{
    [DefOf]
    public static class SG_DefOf
    {
        public static RecipeDef SG_GenoframeRecipe;
    }

    public class SyntheticGeneticsMod : Mod
    {
        public SyntheticGeneticsMod(ModContentPack content) : base(content)
        {
            new Harmony("SyntheticGenetics.Mod").PatchAll();
        }
    }

    [HarmonyPatch(typeof(GenRecipe), "MakeRecipeProducts")]
    public static class MakeRecipeProducts_Patch
    {
        private static IEnumerable<Thing> Postfix(IEnumerable<Thing> __result, RecipeDef recipeDef, Pawn worker, List<Thing> ingredients, Thing dominantIngredient, IBillGiver billGiver, Precept_ThingStyle precept = null)
        {
            foreach (var result in __result)
            {
                yield return result;
            }
            if (recipeDef == SG_DefOf.SG_GenoframeRecipe)
            {
                float efficiency = ((recipeDef.efficiencyStat != null) ? worker.GetStatValue(recipeDef.efficiencyStat) : 1f);
                if (recipeDef.workTableEfficiencyStat != null)
                {
                    Building_WorkTable building_WorkTable = billGiver as Building_WorkTable;
                    if (building_WorkTable != null)
                    {
                        efficiency *= building_WorkTable.GetStatValue(recipeDef.workTableEfficiencyStat);
                    }
                }

                ThingDefCountClass thingDefCountClass = new ThingDefCountClass { count = 1, thingDef = Core.genoframes.RandomElement() };
                Thing thing = ThingMaker.MakeThing(stuff: (!thingDefCountClass.thingDef.MadeFromStuff) ? null : dominantIngredient.def, def: thingDefCountClass.thingDef);
                thing.stackCount = Mathf.CeilToInt((float)thingDefCountClass.count * efficiency);
                if (dominantIngredient != null && recipeDef.useIngredientsForColor)
                {
                    thing.SetColor(dominantIngredient.DrawColor, reportFailure: false);
                }
                CompIngredients compIngredients = thing.TryGetComp<CompIngredients>();
                if (compIngredients != null)
                {
                    for (int l = 0; l < ingredients.Count; l++)
                    {
                        compIngredients.RegisterIngredient(ingredients[l].def);
                    }
                }
                CompFoodPoisonable compFoodPoisonable = thing.TryGetComp<CompFoodPoisonable>();
                if (compFoodPoisonable != null)
                {
                    if (Rand.Chance(worker.GetRoom()?.GetStat(RoomStatDefOf.FoodPoisonChance) ?? RoomStatDefOf.FoodPoisonChance.roomlessScore))
                    {
                        compFoodPoisonable.SetPoisoned(FoodPoisonCause.FilthyKitchen);
                    }
                    else if (Rand.Chance(worker.GetStatValue(StatDefOf.FoodPoisonChance)))
                    {
                        compFoodPoisonable.SetPoisoned(FoodPoisonCause.IncompetentCook);
                    }
                }
                yield return GenRecipe.PostProcessProduct(thing, recipeDef, worker, precept);
            }
        }
    }

    [HarmonyPatch(typeof(DefGenerator), "GenerateImpliedDefs_PreResolve")]
    public static class GenerateImpliedDefs_PreResolve_Patch
    {
        public static bool generated;
        public static void Prefix()
        {
            if (!generated)
            {
                generated = true;
                foreach (var genome in DefDatabase<ThingDef>.AllDefs.Where((ThingDef x) => x.thingCategories?.Any(x => x.defName.Contains("GR_GeneticMaterial")) ?? false))
                {
                    var def = GetRecipeDef(genome);
                    def.PostLoad();
                    DefDatabase<RecipeDef>.Add(def);
                }
            }
        }

        private static RecipeDef GetRecipeDef(ThingDef genome)
        {
            return new RecipeDef
            {
                workSpeedStat = StatDefOf.GeneralLaborSpeed,
                effectWorking = DefDatabase<EffecterDef>.GetNamed("Cremate"),
                soundWorking = SoundDef.Named("Recipe_Cremate"),
                workSkill = SkillDefOf.Intellectual,
                jobString = "SyntheticGenetics.MakeRecipeJobLabel".Translate(genome.label),
                unfinishedThingDef = ThingDef.Named("GR_GenoframeUnfinished"),
                researchPrerequisite = ResearchProjectDef.Named("SG_SyntheticMadness"),
                fixedIngredientFilter = new ThingFilter
                {
                    categories = new List<string>
                    {
                        "MeatRaw"
                    },
                    thingDefs = new List<ThingDef>
                    {
                        ThingDef.Named("ComponentIndustrial"),
                        ThingDef.Named("Plasteel"),
                    }
                },
                defName = "SG_Make_" + genome.defName,
                label = "SyntheticGenetics.MakeRecipeLabel".Translate(genome.label),
                description = "SyntheticGenetics.MakeRecipeDescription".Translate(genome.label),
                workAmount = 3500,
                ingredients = new List<IngredientCount>
                {
                    new IngredientCount
                    {
                        filter = new ThingFilter
                        {
                            categories = new List<string>
                            {
                                "MeatRaw"
                            }
                        },
                        count = 200
                    },
                    new IngredientCount
                    {
                        filter = new ThingFilter
                        {
                            thingDefs = new List<ThingDef>
                            {
                                ThingDef.Named("ComponentIndustrial")
                            }
                        },
                        count = 5
                    },
                    new IngredientCount
                    {
                        filter = new ThingFilter
                        {
                            thingDefs = new List<ThingDef>
                            {
                                ThingDef.Named("Plasteel")
                            }
                        },
                        count = 15
                    }
                },
                products = new List<ThingDefCountClass>
                {
                    new ThingDefCountClass
                    {
                        thingDef = genome,
                        count = 1
                    }
                },
                recipeUsers = new List<ThingDef>
                {
                    ThingDef.Named("GR_GeneticsTinkeringTable")
                },
                    skillRequirements = new List<SkillRequirement>
                {
                    new SkillRequirement
                    {
                        skill = SkillDefOf.Intellectual,
                        minLevel = 5
                    }
                }
            };
        }
    }
}
