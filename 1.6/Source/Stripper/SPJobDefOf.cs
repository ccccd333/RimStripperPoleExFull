using RimWorld;
using Verse;

namespace Stripper
{
    [DefOf]
    public static class SPJobDefOf
    {
        static SPJobDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(SPJobDefOf));
        }
        public static JobDef UseStripperPole;
        public static JobDef WatchStripperPole;
        public static JobDef SP_ServingVisitor;
        public static JobDef SP_SexClient;
    }
}
