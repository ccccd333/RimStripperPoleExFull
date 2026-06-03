using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI.Group;

namespace Stripper
{
    public static class StripperPaymentHelper
    {
        public const float BasePrice = 20f;
        public const float BeautyPricePerPoint = 25f;

        public static int PriceOfPerformance(Pawn performer)
        {
            if (performer == null)
            {
                return 0;
            }

            float beauty = performer.GetStatValue(StatDefOf.PawnBeauty);
            return Math.Max(1, (int)Math.Round(BasePrice + beauty * BeautyPricePerPoint));
        }

        public static int PayClientToPerformer(Pawn client, Pawn performer, int price, HashSet<Pawn> watchers)
        {
            if (client == null || performer == null || price <= 0)
            {
                return 0;
            }

            int amountLeft = PayFromInventory(client, performer, price);
            if (amountLeft <= 0)
            {
                return price;
            }

            // 本人のシルバーが足りない場合、一緒に見ていた中から肩代わりさせる
            if (watchers != null)
            {
                foreach (Pawn member in watchers)
                {
                    if (member == client || member == null || !member.Spawned || member.Dead || member.Faction != client.Faction)
                    {
                        continue;
                    }

                    // 代わりに支払う
                    amountLeft = PayFromInventory(member, performer, amountLeft);
                    if (amountLeft <= 0)
                    {
                        break;
                    }
                }
            }

            return price - amountLeft;
        }

        private static int PayFromInventory(Pawn payer, Pawn performer, int amountLeft)
        {
            if (amountLeft <= 0 || payer?.inventory?.innerContainer == null)
            {
                return amountLeft;
            }

            foreach (Thing silver in payer.inventory.innerContainer.Where(x => x.def == ThingDefOf.Silver).ToList())
            {
                if (amountLeft <= 0)
                {
                    break;
                }

                int dropAmount = Math.Min(silver.stackCount, amountLeft);
                if (payer.inventory.innerContainer.TryDrop(silver, performer.Position, performer.Map, ThingPlaceMode.Near, dropAmount, out _))
                {
                    amountLeft -= dropAmount;
                }
            }

            return amountLeft;
        }
    }
}
