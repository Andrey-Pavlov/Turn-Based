using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

class RandomNetUnitGenerator : NetworkBehaviour
{
    private System.Random _rnd = new System.Random();

    public Transform UnitsParent;

    public GameObject UnitPrefab;
    public int UnitsPerPlayer;

    public CellGrid CellGrid;

    public List<Unit> SpawnUnits(int playerNumber, NetworkConnection conn)
    {
        List<Unit> ret = new List<Unit>();

        List<Cell> freeCells = CellGrid.Cells.FindAll(h => h.GetComponent<Cell>().IsTaken == false);
        freeCells = freeCells.OrderBy(h => _rnd.Next()).ToList();

        for (int j = 0; j < UnitsPerPlayer; j++)
        {
            var cell = freeCells.ElementAt(0);
            freeCells.RemoveAt(0);
            cell.GetComponent<Cell>().IsTaken = true;

            var unit = Instantiate(UnitPrefab);
            unit.transform.position = cell.transform.position + new Vector3(0, 0, 0);
            unit.GetComponent<Unit>().PlayerNumber = playerNumber;
            unit.GetComponent<Unit>().Cell = cell.GetComponent<Cell>();
            unit.GetComponent<Unit>().Initialize();
            unit.transform.parent = UnitsParent;

            NetworkServer.SpawnWithClientAuthority(unit, conn);

            ret.Add(unit.GetComponent<Unit>());
        }
        return ret;
    }
}

