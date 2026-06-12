using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Stripper
{
    [DefOf]
    public static class SPInteractionDefOf
    {
        static SPInteractionDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(SPInteractionDefOf));
        }
        public static InteractionDef SP_DancePerformed;
        public static InteractionDef SP_InviteToDance;
    }

}
