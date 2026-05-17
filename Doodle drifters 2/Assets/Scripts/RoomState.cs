/// <summary>
/// Holds all data for a single room run.
/// Created fresh by GameLoopOrchestrator at the start of each room.
/// </summary>
[System.Serializable]
public class RoomState
{
    public int    roomIndex;
    public string scenarioText;
    public string predictedObject;
    public float  confidence;
    public string solutionText;

    public RoomState(int index)
    {
        roomIndex     = index;
        scenarioText  = string.Empty;
        predictedObject = string.Empty;
        confidence    = 0f;
        solutionText  = string.Empty;
    }
}