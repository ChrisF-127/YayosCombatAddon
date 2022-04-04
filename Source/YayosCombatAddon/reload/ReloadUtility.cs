using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace YayosCombatAddon
{
    class ReloadUtility
    {
        public static void TryForcedReloadFromInventory(Pawn pawn, IEnumerable<CompReloadable> comps)
        {
            Log.Message(pawn.ToString());
            var hasReloadJob = false;
            foreach (var comp in comps)
            {
                if (comp.RemainingCharges < comp.MaxCharges)
                {
                    var innerContainer = pawn.inventory.innerContainer;
                    var ar_ammo = innerContainer.Where((item) => item.def == comp.AmmoDef).ToList();
                    if (ar_ammo.Count > 0)
                    {
                        List<Thing> ar_dropThing = new List<Thing>();
                        int need = comp.MaxAmmoNeeded(true);
                        for (int i = ar_ammo.Count - 1; i >= 0; i--)
                        {
                            // drop
                            int count = Mathf.Min(need, ar_ammo[i].stackCount);
                            if (!innerContainer.TryDrop(ar_ammo[i], pawn.Position, pawn.Map, ThingPlaceMode.Direct, count, out Thing dropThing))
                                innerContainer.TryDrop(ar_ammo[i], pawn.Position, pawn.Map, ThingPlaceMode.Near, count, out dropThing);

                            if (count > 0)
                            {
                                need -= count;
                                ar_dropThing.Add(dropThing);
                            }
                            if (need <= 0)
                                break;
                        }

                        if (ar_dropThing.Count > 0)
                        {
// TODO make proper job which doesn't drop stuff on the ground as that might cause problems?
                            // pick up
                            Job j = JobMaker.MakeJob(JobDefOf.Reload, comp.parent);
                            j.targetQueueB = ar_dropThing.Select(t => new LocalTargetInfo(t)).ToList();
                            j.count = ar_dropThing.Sum(t => t.stackCount);
                            j.count = Math.Min(j.count, comp.MaxAmmoNeeded(true));
                            if (!hasReloadJob)
                            {
                                pawn.jobs.TryTakeOrderedJob(j);
                                hasReloadJob = true;
                            }
                            else
                                pawn.jobs.jobQueue.EnqueueLast(j);
                            pawn.jobs.jobQueue.EnqueueLast(JobMaker.MakeJob(JobDefOf.Goto, pawn.Position));
                        }
                    }
                }
            }
        }

        public static void TryForcedReloadFromSurrounding(Pawn pawn, IEnumerable<CompReloadable> comps)
        {
            Log.Message(pawn.ToString());
            var hasReloadJob = false;
            foreach (var comp in comps)
            {
                if (comp.RemainingCharges < comp.MaxCharges)
                {
                    if (yayoCombat.yayoCombat.supplyAmmoDist >= 0)
                    {
                        // If there is no ammo in your inventory, pick up the ammo item on the floor.
                        var desiredQuantity = new IntRange(comp.MinAmmoNeeded(false), comp.MaxAmmoNeeded(false));
                        var enoughAmmo = RefuelWorkGiverUtility.FindEnoughReservableThings(
                            pawn,
                            pawn.Position,
                            desiredQuantity,
                            t => t.def == comp.AmmoDef && IntVec3Utility.DistanceTo(pawn.Position, t.Position) <= yayoCombat.yayoCombat.supplyAmmoDist);

                        if (enoughAmmo != null && pawn.jobs.jobQueue.ToList().Count <= 0)
                        {
                            var j = JobGiver_Reload.MakeReloadJob(comp, enoughAmmo);
                            if (!hasReloadJob)
                            {
                                pawn.jobs.TryTakeOrderedJob(j);
                                hasReloadJob = true;
                            }
                            else
                                pawn.jobs.jobQueue.EnqueueLast(j);
                            pawn.jobs.jobQueue.EnqueueLast(JobMaker.MakeJob(JobDefOf.Goto, pawn.Position));
                        }
                    }
                }
            }
        }
    }
}
