using UnityEngine;

public class HumanPlayer : Player
{
	private CellGrid CellGrid;

	public override int PlayerNumber
	{
		get
		{
			return this.connectionToClient.connectionId;
		}
	}

	void Awake()
	{
		CellGrid = GameObject.FindGameObjectWithTag("GameController").GetComponent<CellGrid>();
	}

	public override void Play(CellGrid cellGrid)
	{
		CellGrid.CellGridState = new CellGridStateWaitingForInput(cellGrid);
	}
}