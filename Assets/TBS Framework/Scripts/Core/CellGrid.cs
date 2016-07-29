using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine.Networking;
using System.Collections;

/// <summary>
/// CellGrid class keeps track of the game, stores cells, units and players objects. It starts the game and makes turn transitions. 
/// It reacts to user interacting with units or cells, and raises events related to game progress. 
/// </summary>
public class CellGrid : NetworkBehaviour
{
	public event EventHandler GameStarted;
	public event EventHandler GameEnded;
	public event EventHandler TurnEnded;

	private CellGridState _cellGridState;//The grid delegates some of its behaviours to cellGridState object.
	public CellGridState CellGridState
	{
		private get
		{
			return _cellGridState;
		}
		set
		{
			if (_cellGridState != null)
				_cellGridState.OnStateExit();
			_cellGridState = value;
			_cellGridState.OnStateEnter();
		}
	}

	public int MaxNumberOfPlayers = 2;

	public Player CurrentPlayer
	{
		get { return Players.First(p => p.PlayerNumber.Equals(CurrentPlayerNumber)); }
	}

	[SyncVar]
	public int CurrentPlayerNumber;

	//public Transform PlayersParent;
	public GameObject MapPrefab;

	public Queue<Player> Players { get; private set; }
	public List<Cell> Cells { get; private set; }
	public List<Unit> Units { get; private set; }

	public override void OnStartServer()
	{
		Players = new Queue<Player>();
		//for (int i = 0; i < PlayersParent.childCount; i++)
		//{
		//    var player = PlayersParent.GetChild(i).GetComponent<Player>();
		//    if (player != null)
		//        Players.Add(player);
		//    else
		//        Debug.LogError("Invalid object in Players Parent game object");
		//}
		//CurrentPlayerNumber = Players.Min(p => p.PlayerNumber);

		Cells = new List<Cell>();
		//for (int i = 0; i < transform.childCount; i++)
		//{
		//    var cell = transform.GetChild(i).gameObject.GetComponent<Cell>();
		//    if (cell != null)
		//        Cells.Add(cell);
		//    else
		//        Debug.LogError("Invalid object in cells paretn game object");
		//}

		for (int y = 0; y < 9; y++)
		{
			for (int x = 0; x < 9; x++)
			{
				var obj = Instantiate<GameObject>(MapPrefab);

				obj.transform.position = new Vector3(x, y, 0f);

				var cell = obj.GetComponent<Cell>();
				cell.OffsetCoord = new Vector2(x, y);

				if (cell != null)
					Cells.Add(cell);
				else
					Debug.LogError("Invalid object in cells paretn game object");

				NetworkServer.Spawn(obj);
			}
		}

		foreach (var cell in Cells)
		{
			cell.CellClicked += OnCellClicked;
			cell.CellHighlighted += OnCellHighlighted;
			cell.CellDehighlighted += OnCellDehighlighted;
		}

		StartCoroutine(StartGameCoroutine());
	}

	public IEnumerator StartGameCoroutine()
	{
		while (Players.Count == 0)
		{
			yield return 0;
		}

		var currentPlayer = Players.Peek();
		CurrentPlayerNumber = currentPlayer.PlayerNumber;

		StartGame();
	}

#region Events

	private void OnCellDehighlighted(object sender, EventArgs e)
	{
		CellGridState.OnCellDeselected(sender as Cell);
	}
	private void OnCellHighlighted(object sender, EventArgs e)
	{
		CellGridState.OnCellSelected(sender as Cell);
	}
	private void OnCellClicked(object sender, EventArgs e)
	{
		CellGridState.OnCellClicked(sender as Cell);
	}

	private void OnUnitClicked(object sender, EventArgs e)
	{
		CellGridState.OnUnitClicked(sender as Unit);
	}

	private void OnUnitDestroyed(object sender, AttackEventArgs e)
	{
		Units.Remove(sender as Unit);
		var totalPlayersAlive = Units.Select(u => u.PlayerNumber).Distinct().ToList(); //Checking if the game is over
		if (totalPlayersAlive.Count == 1)
		{
			if (GameEnded != null)
				GameEnded.Invoke(this, new EventArgs());
		}
	}

	#endregion

	

	public void CmdOnMyPlayerConnected(Player player)
	{
		Players.Enqueue(player);

		SpawnUnits();
	}

	void SpawnUnits()
	{
		var unitGenerator = GetComponent<RandomNetUnitGenerator>();
		if (unitGenerator != null)
		{
			Units = unitGenerator.SpawnUnits(CurrentPlayer, Cells);
			foreach (var unit in Units)
			{
				unit.UnitClicked += OnUnitClicked;
				unit.UnitDestroyed += OnUnitDestroyed;
			}
		}
		else
		{
			Debug.LogError("No IUnitGenerator script attached to cell grid");
		}
	}

	/// <summary>
	/// Method is called once, at the beggining of the game.
	/// </summary>
	public void StartGame()
	{
		if (GameStarted != null)
			GameStarted.Invoke(this, new EventArgs());

		Action();
	}

	/// <summary>
	/// Method makes turn transitions. It is called by player at the end of his turn.
	/// </summary>
	public void EndTurn()
	{
		EndPreviousTurn();

		StartNextTurn();
	}

	private void EndPreviousTurn()
	{
		if (Units.Select(u => u.PlayerNumber).Distinct().Count() == 1)
		{
			return;
		}
		CellGridState = new CellGridStateTurnChanging(this);

		Units.FindAll(u => u.PlayerNumber.Equals(CurrentPlayerNumber)).ForEach(u => { u.OnTurnEnd(); });

		if (TurnEnded != null)
			TurnEnded.Invoke(this, new EventArgs());
	}

	private void StartNextTurn()
	{
		//CurrentPlayerNumber = (CurrentPlayerNumber + 1) % MaxNumberOfPlayers;
		CurrentPlayerNumber = Players.Peek().PlayerNumber;
		while (Units.FindAll(u => u.PlayerNumber.Equals(CurrentPlayerNumber)).Count == 0)
		{
			CurrentPlayerNumber = Players.Peek().PlayerNumber;
		}//Skipping players that are defeated.

		Action();
	}

	private void Action()
	{
		Units.FindAll(u => u.PlayerNumber.Equals(CurrentPlayerNumber)).ForEach(u => { u.OnTurnStart(); });
		Players.First(p => p.PlayerNumber.Equals(CurrentPlayerNumber)).Play(this);
	}
}
