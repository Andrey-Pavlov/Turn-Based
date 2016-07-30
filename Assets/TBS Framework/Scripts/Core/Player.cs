using UnityEngine;
using UnityEngine.Networking;

public abstract class Player : NetworkBehaviour
{
    [SyncVar]
    public int PlayerNumber;

    /// <summary>
    /// Method is called every turn. Allows player to interact with his units.
    /// </summary>
    public abstract void Play(CellGrid cellGrid);
}