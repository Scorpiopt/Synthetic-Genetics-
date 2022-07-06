using UnityEngine;
using Verse;
using System.Linq;
using HarmonyLib;
using RimWorld;
using System.Collections.Generic;

namespace SyntheticGenetics
{
    public class SyntheticGeneticsMod : Mod
    {
        public SyntheticGeneticsMod(ModContentPack content) : base(content)
        {
            new Harmony("SyntheticGenetics.Mod").PatchAll();
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
                var genomes = DefDatabase<ThingDef>.AllDefs.Where((ThingDef x) => x.thingCategories?.Any(x => x.defName.Contains("GR_GeneticMaterial")) ?? false).ToList();
                var genoframes = DefDatabase<ThingDef>.AllDefs.Where((ThingDef x) => x.thingCategories?.Any(x => x.defName.Contains("GR_Genoframes")) ?? false).ToList();
                foreach (var genome in genomes)
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
