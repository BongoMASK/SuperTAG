using UnityEngine;

public abstract class GameMode : MonoBehaviour {

    [SerializeField] public string gameModeName;

    [TextArea(3, 3)]
    [SerializeField] public string gameModeDescription;

    /// <summary>
    /// Override the function to set what should be done when a player tags another player
    /// </summary>
    public abstract void OnTag();

    /// <summary>
    /// Override the function to set what should be done when the round finishes
    /// </summary>
    public abstract void OnRoundEnd();

    /// <summary>
    /// Override the function to set what should be done when the timer hits zero
    /// </summary>
    public abstract void OnTimeIsZero();

}
