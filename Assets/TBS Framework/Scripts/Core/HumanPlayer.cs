using System;
using UnityEngine;
using UnityEngine.Networking;

public class HumanPlayer : Player
{
    private CellGrid _CellGrid;

    void Awake()
    {
        _CellGrid = GameObject.FindGameObjectWithTag("GameController").GetComponent<CellGrid>();
    }

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
                if (this.PlayerNumber == _CellGrid.CurrentPlayerNumber)
                {
                    _CellGrid.CmdEndTurn();//User ends his turn by pressing "n" on keyboard.
                }
            }
        }
    }

    public override void OnStartLocalPlayer()
    {
        CmdSpawnUnits();
    }

    public override void Play(CellGrid cellgrid)
    {
        _CellGrid.CellGridState = new CellGridStateWaitingForInput(_CellGrid);
    }

    [Command]
    public void CmdSpawnUnits()
    {
        var unitGenerator = _CellGrid.GetComponent<RandomNetUnitGenerator>();
        if (unitGenerator != null)
        {
            var Units = unitGenerator.SpawnUnits(this.PlayerNumber, this.connectionToClient);

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

        PlayerNumber = _CellGrid.CurrentNumberOfPlayers;
        ++_CellGrid.CurrentNumberOfPlayers;
    }

    private void OnUnitClicked(object sender, EventArgs e)
    {
        _CellGrid.CellGridState.OnUnitClicked(sender as Unit);
    }

    private void OnUnitDestroyed(object sender, AttackEventArgs e)
    {
        _CellGrid.OnUnitDestroyed();
    }
}