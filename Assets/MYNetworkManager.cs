using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class MYNetworkManager : NetworkManager
{
	public MYNetworkManager() : base()
	{
	}

	public override void OnServerAddPlayer(NetworkConnection conn, short playerControllerId)
	{
		base.OnServerAddPlayer(conn, playerControllerId);

		conn.playerControllers[0].gameObject.GetComponent<HumanPlayer>();
	}
}
