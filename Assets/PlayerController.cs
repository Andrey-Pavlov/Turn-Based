using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class PlayerController : NetworkBehaviour {

	private CellGrid CellGrid;

	void Start()
	{
		Debug.Log("Press 'n' to end turn");

		CellGrid = GameObject.FindGameObjectWithTag("GameController").GetComponent<CellGrid>();
	}

	void Update()
	{
		if (isLocalPlayer)
		{
			if (Input.GetKeyDown(KeyCode.N))
			{
				if (this.playerControllerId == CellGrid.CurrentPlayerNumber)
				{
					CellGrid.EndTurn();//User ends his turn by pressing "n" on keyboard.
				}

			}
		}
	}
}
