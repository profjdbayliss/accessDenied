using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Mirror.Examples.Chat;
using TMPro;
using System;

public class RGNetworkManager : NetworkManager
{
    public GameObject playerListPrefab;
    bool mErrorHappened = false;

    public override void OnStartServer()
    {
        base.OnStartServer();

        GameObject obj = Instantiate(playerListPrefab);
        playerListPrefab.transform.localScale = Vector3.one;
        NetworkServer.Spawn(obj);
    }


    public override void OnStartClient()
    {
            base.OnStartClient();
    }
    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        base.OnServerAddPlayer(conn);

        int playerID = conn.connectionId;
        RGNetworkPlayer player = (RGNetworkPlayer)conn.identity.GetComponent<RGNetworkPlayer>();
        string name = (string)player.mPlayerName;
        RGNetworkPlayerList.instance.AddPlayer(playerID, name);
    }


    // Called by UI element NetworkAddressInput.OnValueChanged
    public void SetHostname(string hostname)
    {
        networkAddress = hostname;
    }

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        // remove player name from the HashSet
        if (conn.authenticationData != null)
            RGNetworkAuthenticator.playerNames.Remove((string)conn.authenticationData);

        RGNetworkPlayerList.instance.RemovePlayer(conn.connectionId);

        base.OnServerDisconnect(conn);
    }

    public override void OnClientDisconnect()
    {
        base.OnClientDisconnect();
        RGNetworkLoginUI.HasLoadedScene = false;
        RGNetworkLoginUI.s_instance.gameObject.SetActive(true);
        RGNetworkLoginUI.s_instance.playernameInput.text = "";
        RGNetworkLoginUI.s_instance.playernameInput.ActivateInputField();

    }

    public override void OnServerError(NetworkConnectionToClient conn, 
        TransportError error, string reason) { 
    Debug.Log("Caught an error! "+reason);
    }

    public override void OnClientError(TransportError error, string reason)
    {
        Debug.Log("Caught a client error! " + reason);
    }
}
