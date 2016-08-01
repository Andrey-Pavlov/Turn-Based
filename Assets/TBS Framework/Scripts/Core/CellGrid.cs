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
        get
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

    public List<Player> Players
    {
        get
        {
            return GameObject.FindObjectsOfType<Player>().ToList();
        }
    }

    public List<Cell> Cells
    {
        get
        {
            return GameObject.FindObjectsOfType<Cell>().ToList();
        }
    }

    public List<Unit> Units
    {
        get
        {
            return GameObject.FindObjectsOfType<Unit>().ToList();
        }
    }

    private System.Random _rnd = new System.Random();

    private List<Cell> GenerateCells()
    {
        var cells = new List<Cell>();

        for (int y = 0; y < 9; y++)
        {
            for (int x = 0; x < 9; x++)
            {
                var obj = Instantiate<GameObject>(MapPrefab);

                obj.transform.position = new Vector3(x, y, 0f);

                var cell1 = obj.GetComponent<Cell>();
                cell1.OffsetCoord = new Vector2(x, y);

                if (cell1 != null)
                {
                    cells.Add(cell1);

                    NetworkServer.Spawn(cell1.gameObject);
                }
                else
                    Debug.LogError("Invalid object in cells paretn game object");
            }
        }

        return cells;
    }

    #region Obstacles

    public int ObstaclesAmount;
    public GameObject ObstaclePrefab;

    private void GenerateObstacles(List<Cell> cells)
    {
        List<Cell> freeCells = cells.FindAll(h => h.GetComponent<Cell>().IsTaken == false);
        freeCells = freeCells.OrderBy(h => _rnd.Next()).ToList();

        for (int i = 0; i < Mathf.Clamp(ObstaclesAmount, ObstaclesAmount, freeCells.Count); i++)
        {
            var cell = freeCells.ElementAt(i);
            cell.GetComponent<Cell>().IsTaken = true;

            var obstacle = Instantiate(ObstaclePrefab);
            obstacle.transform.position = cell.transform.position + new Vector3(0, 0, -1f);

            NetworkServer.Spawn(obstacle);
        }
    }

    #endregion

    public override void OnStartServer()
    {
        List<Cell> cells = GenerateCells();

        GenerateObstacles(cells);

        StartCoroutine(StartGameCoroutine());
    }

    IEnumerator StartGameCoroutine()
    {
        while (Players.Count == 0)
        {
            yield return null;
        }

        StartGame();
    }

    private void InitGame()
    {
        foreach (var cell in Cells)
        {
            cell.CellClicked += OnCellClicked;
            cell.CellHighlighted += OnCellHighlighted;
            cell.CellDehighlighted += OnCellDehighlighted;
        }

        foreach (var unit in Units)
        {
            unit.UnitClicked += OnUnitClicked;
            unit.UnitDestroyed += OnUnitDestroyed;
        }
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

    /// <summary>
    /// Method is called once, at the beggining of the game.
    /// </summary>
    public void StartGame()
    {
        if (GameStarted != null)
            GameStarted.Invoke(this, new EventArgs());

        Action(true);
    }

    /// <summary>
    /// Method makes turn transitions. It is called by player at the end of his turn.
    /// </summary>
    [Command]
    public void CmdEndTurn()
    {
        RpcEndTurn();
    }

    [ClientRpc]
    public void RpcEndTurn()
    {
        EndPreviousTurn();

        StartNextTurn();
    }

    private void EndPreviousTurn()
    {
        if (Units.Select(u => u.PlayerNumber).Distinct().Count() == 0)
        {
            return;
        }

        Units.FindAll(u => u.PlayerNumber.Equals(CurrentPlayerNumber)).ForEach(u => { u.OnTurnEnd(); });

        if (TurnEnded != null)
            TurnEnded.Invoke(this, new EventArgs());
    }

    private void StartNextTurn()
    {
        CurrentPlayerNumber = (CurrentPlayerNumber + 1) % MaxNumberOfPlayers;
        while (Units.FindAll(u => u.PlayerNumber.Equals(CurrentPlayerNumber)).Count == 0)
        {
            CurrentPlayerNumber = (CurrentPlayerNumber + 1) % MaxNumberOfPlayers;
        }//Skipping players that are defeated.

        Action();
    }

    private void Action(bool isStart = false)
    {
        var currentPlayer = Players.First(p => p.PlayerNumber.Equals(CurrentPlayerNumber));

        if (currentPlayer.isLocalPlayer)
        {
            Units.FindAll(u => u.PlayerNumber.Equals(CurrentPlayerNumber)).ForEach(u => { u.OnTurnStart(); });
            Players.First(p => p.PlayerNumber.Equals(CurrentPlayerNumber)).Play(this);

            if(isStart)
            {
                InitGame();
            }
        }
    }

    #region Spawn Units

    public GameObject UnitPrefab;
    public int UnitsPerPlayer;

    public void SpawnUnits(HumanPlayer player)
    {
        List<Cell> freeCells = Cells.FindAll(h => h.GetComponent<Cell>().IsTaken == false);
        freeCells = freeCells.OrderBy(h => _rnd.Next()).ToList();

        for (int j = 0; j < UnitsPerPlayer; j++)
        {
            var cell = freeCells.ElementAt(0);

            var unit = Instantiate(UnitPrefab);
            unit.transform.position = cell.transform.position + new Vector3(0, 0, 0);

            freeCells.RemoveAt(0);
            cell.GetComponent<Cell>().IsTaken = true;

            unit.GetComponent<Unit>().PlayerNumber = player.PlayerNumber;
            unit.GetComponent<Unit>().Cell = cell.GetComponent<Cell>();
            unit.GetComponent<Unit>().Initialize();

            var unitClass = unit.GetComponent<Unit>();

            Units.Add(unitClass);

            CmdSpawn(unit, player.gameObject);
        }

    }

    [Command]
    void CmdSpawn(GameObject gameObject, GameObject player)
    {
        NetworkServer.SpawnWithClientAuthority(gameObject, player);
    }

    #endregion
}
