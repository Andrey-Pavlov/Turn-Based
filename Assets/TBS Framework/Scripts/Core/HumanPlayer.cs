using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Linq;

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
        if (Input.GetKeyDown(KeyCode.N))
        {
            if (this.PlayerNumber == _CellGrid.CurrentPlayerNumber)
            {
                _CellGrid.CmdEndTurn();//User ends his turn by pressing "n" on keyboard.
            }
        }
    }

    public override void OnStartLocalPlayer()
    {
        this.PlayerNumber = _CellGrid.Players.Count - 1;

        _CellGrid.SpawnUnits(this);
    }

    public override void Play(CellGrid cellgrid)
    {
        _CellGrid.CellGridState = new CellGridStateWaitingForInput(_CellGrid);
    }
}