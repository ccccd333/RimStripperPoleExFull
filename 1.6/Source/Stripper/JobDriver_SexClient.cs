using RimWorld;
using rjw;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;

namespace Stripper
{
    public class JobDriver_SexClient : JobDriver_SexBaseReciever
    {

        protected override void DoSetup()
        {
            base.DoSetup();

            try
            {
                if (pawn.relations.OpinionOf(Partner) < 0)
                    ticks_between_hearts += 50;
                else if (pawn.relations.OpinionOf(Partner) > 60)
                    ticks_between_hearts -= 25;
            }
            catch
            {
                
            }

            this.FailOn(() => pawn.Drafted);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            DoSetup();
            if (Partner.CurJob.def == SPJobDefOf.SP_ServingVisitor)
            {
                this.FailOn(() => Partner.CurJob == null);
                yield return Toils_Reserve.Reserve(iTarget, 1, 0);

                var lovedToil = new Toil();

                lovedToil.defaultCompleteMode = ToilCompleteMode.Never;
                lovedToil.socialMode = RandomSocialMode.Off;
                lovedToil.handlingFacing = true;
                lovedToil.tickAction = () =>
                {
                    if (pawn.IsHashIntervalTick(ticks_between_hearts))
                        ThrowMetaIconF(pawn.Position, pawn.Map, FleckDefOf.Heart);
                };
                lovedToil.AddFinishAction(() =>
                {
                    if (xxx.is_human(pawn))
                    {
                        var comp = xxx.GetCompRJW(pawn);
                        if (comp != null)
                        {
                            comp.drawNude = false;
                            pawn.Drawer.renderer.SetAllGraphicsDirty();
                        }
                    }
                    GlobalTextureAtlasManager.TryMarkPawnFrameSetDirty(pawn);
                });

                lovedToil.FailOn(() => Partner.CurJob?.def != SPJobDefOf.SP_ServingVisitor);
                yield return lovedToil;
            }
        }

    }
}
