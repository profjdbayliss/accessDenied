using Mirror;

public class RGNetworkPlayer : NetworkBehaviour
{
    // Note: any single var that's player specific should be here and sync'd
    [SyncVar] public string mPlayerName;
    [SyncVar] public int mPlayerID;

    public override void OnStartServer()
    {
        mPlayerName = (string)connectionToClient.authenticationData;
        mPlayerID = connectionToClient.connectionId;
        RGGameExampleUI.localPlayerName = mPlayerName;
        RGGameExampleUI.localPlayerID = mPlayerID;
    }

    public override void OnStartLocalPlayer()
    {      
        RGGameExampleUI.localPlayerName = mPlayerName;
        RGGameExampleUI.localPlayerID = mPlayerID;
        RGNetworkPlayerList.instance.localPlayerID = mPlayerID;
    }
}
