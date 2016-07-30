using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class MYNetworkManager : NetworkManager
{
    public MYNetworkManager() : base()
    {

    }

    public override void OnServerAddPlayer(NetworkConnection conn, short playerControllerId)
    {
        base.OnServerAddPlayer(conn, playerControllerId);
    }
}
