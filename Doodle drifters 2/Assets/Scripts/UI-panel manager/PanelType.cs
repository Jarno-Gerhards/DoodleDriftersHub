/// <summary>
/// All UI panels in the game.
/// Each value maps to exactly one UIPanel subclass and one GameObject in the scene.
///
/// To add a new panel:
///   1. Add a value here.
///   2. Create a UIPanel subclass for it in UIPanels.cs.
///   3. Assign it in the Inspector on PanelManager.
///   4. Add the GameStateType mapping in PanelManager.GetPanelForState().
/// </summary>
public enum PanelType
{
    None,
    MainMenu,
    CharacterCreation,
    EnterDungeon,
    RoomNarration,
    Drawing,
    Voting,
    ResolutionNarration,
    BossIntro,
    EndScreen
}
