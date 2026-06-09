using Verse;

namespace Stripper
{
    public class GameComponent_StripperPole : GameComponent
    {
        public static GameComponent_StripperPole Instance { get; private set; }

        public GameComponent_StripperPole(Game game) : base()
        {
            if (StripperMod.settings.debugLog)
            {
                Log.Message($"[StripperPole] GameComponent_StripperPole ctor");
            }
            StripperPoleHelper.ClearAvailableProstitutes();
            StripperPoleHelper.ClearAllPoles();
            Instance = this;
        }

        //public override void StartedNewGame()
        //{

        //}

        //public override void LoadedGame()
        //{
        //    StripperPoleHelper.ClearAvailableProstitutes();
        //}

        public override void ExposeData()
        {
            if (StripperMod.settings.debugLog)
            {
                Log.Message($"[StripperPole] GameComponent_StripperPole ExposeData");
            }
            base.ExposeData();
            StripperPoleHelper.ExposeData();
        }
    }
}
