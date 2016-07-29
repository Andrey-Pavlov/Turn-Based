using UnityEngine;
using UnityEngine.Networking;

public class GUIController : NetworkBehaviour
{
	public CellGrid CellGrid;

	void Start()
	{
		Debug.Log("Press 'n' to end turn");
	}

	void Update()
	{
		if (isLocalPlayer)
		{
			if (Input.GetKeyDown(KeyCode.N))
			{
				if (this.playerControllerId == this.CellGrid.CurrentPlayerNumber)
				{
					CellGrid.EndTurn();//User ends his turn by pressing "n" on keyboard.
				}

			}
		}
	}
}
