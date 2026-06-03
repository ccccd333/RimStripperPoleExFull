using Verse;

namespace Stripper
{
    public class GameComponent_StripperPole : GameComponent
    {
        public static GameComponent_StripperPole Instance { get; private set; }

        public GameComponent_StripperPole(Game game) : base()
        {
            Instance = this;
        }

        public override void StartedNewGame()
        {
            StripperPoleHelper.ClearAvailableProstitutes();
        }

        public override void LoadedGame()
        {
            StripperPoleHelper.ClearAvailableProstitutes();
        }
    }
}
