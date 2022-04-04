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
        public static void TryForcedReloadFromInventory(CompReloadable comp)
        {
            if (comp.RemainingCharges < comp.MaxCharges)
            {
                Pawn p = comp.Wearer;

                var ar_ammo = p.inventory.innerContainer.Where((item) => item.def == comp.AmmoDef).ToList();
                if (ar_ammo.Count > 0)
                {
                    List<Thing> ar_dropThing = new List<Thing>();
                    int need = comp.MaxAmmoNeeded(true);
                    for (int i = ar_ammo.Count - 1; i >= 0; i--)
                    {
                        // drop
                        int count = Mathf.Min(need, ar_ammo[i].stackCount);
                        var innerContainer = p.inventory.innerContainer;
                        if (!innerContainer.TryDrop(ar_ammo[i], p.Position, p.Map, ThingPlaceMode.Direct, count, out Thing dropThing))
                            innerContainer.TryDrop(ar_ammo[i], p.Position, p.Map, ThingPlaceMode.Near, count, out dropThing);

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
                        // pick up
                        Job j = JobMaker.MakeJob(JobDefOf.Reload, comp.parent);
                        j.targetQueueB = ar_dropThing.Select(t => new LocalTargetInfo(t)).ToList();
                        j.count = ar_dropThing.Sum(t => t.stackCount);
                        j.count = Math.Min(j.count, comp.MaxAmmoNeeded(true));
                        p.jobs.TryTakeOrderedJob(j);
                        p.jobs.jobQueue.EnqueueLast(JobMaker.MakeJob(JobDefOf.Goto, p.Position));
                    }
                }
            }
        }

        public static void TryForcedReloadFromSurrounding(CompReloadable comp)
        {
            if (comp.RemainingCharges < comp.MaxCharges)
            {
                Pawn p = comp.Wearer;

                if (yayoCombat.yayoCombat.supplyAmmoDist >= 0)
                {
                    // If there is no ammo in your inventory, pick up the ammo item on the floor.
                    IntRange desiredQuantity = new IntRange(comp.MinAmmoNeeded(false), comp.MaxAmmoNeeded(false));
                    List<Thing> enoughAmmo = RefuelWorkGiverUtility.FindEnoughReservableThings(
                        p,
                        p.Position,
                        desiredQuantity,
                        t => t.def == comp.AmmoDef && IntVec3Utility.DistanceTo(p.Position, t.Position) <= yayoCombat.yayoCombat.supplyAmmoDist);

                    if (enoughAmmo != null && p.jobs.jobQueue.ToList().Count <= 0)
                    {
                        p.jobs.TryTakeOrderedJob(JobGiver_Reload.MakeReloadJob(comp, enoughAmmo));
                        p.jobs.jobQueue.EnqueueLast(JobMaker.MakeJob(JobDefOf.Goto, p.Position));
                    }
                }
            }
        }
    }
}
