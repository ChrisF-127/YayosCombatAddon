using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace YayosCombatAddon
{
	public class PawnColumnWorker_CarryAmmo : PawnColumnWorker
	{
		public override void DoCell(Rect rect, Pawn pawn, PawnTable table)
		{
			if (pawn.inventoryStock != null)
			{
				// lazy way for finding the appropiate def: just named them the same
				var group = DefDatabase<InventoryStockGroupDef>.AllDefs.First((def) => def.defName == this.def.defName);

				int localI = 0;
				if (yayoCombat.YayoCombatCore.ammo)
				{
					float num = rect.width - 4f;
					int num2 = Mathf.FloorToInt(num / 3f);
					float x = rect.x;

					// Type
					Widgets.Dropdown(
						new Rect(
							x,
							rect.y + 2f,
							num2,
							rect.height - 4f),
						pawn,
						p => p.inventoryStock.GetDesiredThingForGroup(group),
						p => GenerateThingButtons(p, group),
						buttonIcon: pawn.inventoryStock.GetDesiredThingForGroup(group).uiIcon,
						paintable: true);

					// Amount
					string buffer = null;
					localI = pawn.inventoryStock.GetDesiredCountForGroup(group);
					Widgets.TextFieldNumeric(
						new Rect(
							x + num2 + 4f,
							rect.y + 2f,
							Mathf.FloorToInt(num * (2f / 3f)),
							rect.height - 4f),
						ref localI,
						ref buffer,
						max: 1e4f);
				}
				pawn.inventoryStock.SetCountForGroup(group, localI);
			}
		}

		private IEnumerable<Widgets.DropdownMenuElement<ThingDef>> GenerateThingButtons(Pawn pawn, InventoryStockGroupDef group)
		{
			foreach (ThingDef thing in group.thingDefs)
			{
				yield return new Widgets.DropdownMenuElement<ThingDef>
				{
					option = new FloatMenuOption(thing.LabelCap, delegate
					{
						pawn.inventoryStock.SetThingForGroup(group, thing);
					}),
					payload = thing
				};
			}
		}

		public override int GetMinWidth(PawnTable table)
		{
			return yayoCombat.YayoCombatCore.ammo ? Mathf.Max(base.GetMinWidth(table), Mathf.CeilToInt(54f)) : 0;
		}

		public override int GetOptimalWidth(PawnTable table)
		{
			return yayoCombat.YayoCombatCore.ammo ? Mathf.Clamp(Mathf.CeilToInt(104f), GetMinWidth(table), GetMaxWidth(table)) : 0;
		}

		public override int GetMinHeaderHeight(PawnTable table)
		{
			return yayoCombat.YayoCombatCore.ammo ? Mathf.Max(base.GetMinHeaderHeight(table), 65) : 0;
		}

		public override int Compare(Pawn a, Pawn b)
		{
			return GetValueToCompare(a).CompareTo(GetValueToCompare(b));
		}

		private int GetValueToCompare(Pawn pawn)
		{
			if (pawn.inventoryStock != null)
			{
				// lazy way for finding the appropiate def: just named them the same
				var group = DefDatabase<InventoryStockGroupDef>.AllDefs.First((def) => def.defName == this.def.defName);
				return pawn.inventoryStock.GetDesiredCountForGroup(group);
			}
			return int.MinValue;
		}
	}
}
