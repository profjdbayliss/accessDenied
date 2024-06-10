using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System;
using System.Linq;
using System.Text;

// many messages actually have no arguments
public struct RGNetworkShortMessage : NetworkMessage
{
    public uint indexId;
    public uint type;
}

// for messages that need to update one or more arguments
public struct RGNetworkLongMessage : NetworkMessage
{
    public uint indexId;
    public uint type;
    // number of arguments
    public uint count;
    // the parameters for the message
    // -> ArraySegment to avoid unnecessary allocations
    public ArraySegment<byte> payload;
}

public class RGNetworkPlayerList : NetworkBehaviour, IRGObserver
{
    public static RGNetworkPlayerList instance;

    public int localPlayerID;
    public List<int> playerIDs = new List<int>();
    private GameManager manager;

    private List<bool> playerNetworkReadyFlags = new List<bool>();
    private List<bool> playerTurnTakenFlags = new List<bool>();
    private List<PlayerType> playerTypes = new List<PlayerType>();
    private List<string> playerNames = new List<string>();

    private void Awake()
    {
        instance = this;
        DontDestroyOnLoad(this);
        SetupHandlers();
        manager = FindObjectOfType<GameManager>();
    }

    public void AddPlayer(int id, string name)
    {
        if (isServer)
        {
            playerIDs.Add(id);
            playerNetworkReadyFlags.Add(true);
            playerTurnTakenFlags.Add(false);
            playerTypes.Add(PlayerType.Any);
            playerNames.Add(name);
        } 
    }

    public void SetPlayerType(PlayerType type)
    {
        if (isServer)
        {
            playerTypes[localPlayerID] = type;
            if (CheckReadyToStart())
            {
                Debug.Log("Ready to start server is last!!");
                GameManager.instance.RealGameStart();
            }
        }
    }

    public Message CreateStartGameMessage()
    {
        Message msg;
        List<byte> data = new List<byte>(100);
        int messageCount = playerIDs.Count;
        for (int i=0; i<messageCount; i++)
        {
            // note that the player id is actually its order in this
            // message
            byte[] type = BitConverter.GetBytes((int)playerTypes[i]);
            int nameSize = playerNames[i].Length;
            byte[] nameSizeBytes = BitConverter.GetBytes(nameSize);
            byte[] name = Encoding.ASCII.GetBytes(playerNames[i]);
            data.AddRange(type);
            data.AddRange(nameSizeBytes);
            data.AddRange(name);
        }
        msg = new Message(CardMessageType.StartGame, data);
        return (msg);
    }

    public void RemovePlayer(int id)
    {
        if (!isServer) return;

        playerIDs.Remove(id);
        playerNames.RemoveAt(id);
        playerTypes.RemoveAt(id);
        playerNetworkReadyFlags.RemoveAt(id);
        playerTurnTakenFlags.RemoveAt(id);
    }

    public void UpdateObserver(Message data)
    {
        // send messages here over network to appropriate place(s)
        switch (data.Type)
        {
            case CardMessageType.StartGame:
                if (isServer)
                {
                    // only servers start the game!
                    RGNetworkLongMessage msg = new RGNetworkLongMessage
                    {
                        indexId = (uint)localPlayerID,
                        type = (uint)data.Type,
                        count = (uint)playerIDs.Count,
                        payload = data.byteArguments.ToArray()
                    };
                    NetworkServer.SendToAll(msg);
                    Debug.Log("SERVER SENT a new player name and id to clients");
                }
                break;
            case CardMessageType.EndTurn:
                {
                    // turn taking is handled here because the list of players on 
                    // the network happens here
                    RGNetworkShortMessage msg = new RGNetworkShortMessage
                    {
                        indexId = (uint)localPlayerID,
                        type = (uint)data.Type
                    };
                    Debug.Log("update observer called end turn! ");
                    if (isServer)
                    {
                        // we've played so we're no longer on the ready list
                        int playerIndex = localPlayerID;

                        playerTurnTakenFlags[playerIndex] = true;
                        // find next player to ok to play and send them a message
                        int nextPlayerId = -1;
                        for (int i = playerIndex + 1; i < playerTurnTakenFlags.Count; i++)
                        {
                            if (!playerTurnTakenFlags[i])
                            {
                                nextPlayerId = i;
                                break;
                            }
                        }

                        if (nextPlayerId != -1)
                        {
                            // send the start turn ok to the next player
                            NetworkConnectionToClient connection = NetworkServer.connections[nextPlayerId];
                            msg.type = (uint)CardMessageType.StartTurn;
                            connection.Send(msg);
                            Debug.Log("next client should receive message " + nextPlayerId + " with connection id " + connection.identity.netId);
                        }
                        else
                        {
                            // need to increment the turn and set all the players to ready again
                            for (int i = 0; i < playerTurnTakenFlags.Count; i++)
                            {
                                playerTurnTakenFlags[i] = false;
                            }
                            // set turn to the server
                            manager.StartTurn();
                        }
                    }
                    else
                    {
                        NetworkClient.Send(msg);
                        Debug.Log("CLIENT ENDED TURN AND GAVE IT BACK TO SERVER");
                    }
                }
                break;
            case CardMessageType.IncrementTurn:
                Debug.Log("update observer called increment turn! ");
                if (isServer)
                {
                    RGNetworkShortMessage msg = new RGNetworkShortMessage
                    {
                        indexId = (uint)localPlayerID,
                        type = (uint)data.Type
                    };
                    NetworkServer.SendToAll(msg);
                    Debug.Log("sending turn increment to all clients");
                }
                break;
            case CardMessageType.SharePlayerType:
                {
                    // servers only receive types in separate messages
                    if (!isServer)
                    {
                        RGNetworkLongMessage msg = new RGNetworkLongMessage
                        {
                        indexId = (uint)localPlayerID,
                        type = (uint)data.Type,
                        count = (uint)data.arguments.Count,
                        payload = data.arguments.SelectMany<int, byte>(BitConverter.GetBytes).ToArray()
                        };
                        Debug.Log("update observer called share player type ");
                  
                        NetworkClient.Send(msg);
                        Debug.Log("CLIENT IS SHOWING THEIR PLAYER TYPE AS " + data.ToString());
                    }
                }
                break;
           
            case CardMessageType.EndGame:
                {
                    RGNetworkLongMessage msg = new RGNetworkLongMessage
                    {
                        indexId = (uint)localPlayerID,
                        type = (uint)data.Type,
                        count = (uint)data.arguments.Count(),
                        payload = data.arguments.SelectMany<int, byte>(BitConverter.GetBytes).ToArray()
                    };
                    if (isServer)
                    {
                        NetworkServer.SendToAll(msg);
                        Debug.Log("SERVER SENT GAME END MESSAGE FIRST");
                    }
                    else
                    {
                        NetworkClient.Send(msg);
                        Debug.Log("CLIENT SENT GAME END MESSAGE FIRST");
                    }
                }
                break;
        default:
                break;
    }

}

    public void OnClientReceiveShortMessage(RGNetworkShortMessage msg)
    {
        Debug.Log("CLIENT RECEIVED SHORT MESSAGE::: " + msg.indexId + " " +msg.type );
        uint senderId = msg.indexId;
        CardMessageType type = (CardMessageType)msg.type;

        // NOTE: SENDTOALL ALSO SENDS THE MESSAGE TO THE SERVER AGAIN, WHICH WE DON'T NEED
        if (!isServer)
        {
            switch (type)
            {
                case CardMessageType.StartTurn:
                    Debug.Log("received start turn message");
                    manager.StartTurn();
                    break;
                case CardMessageType.EndTurn:
                    // only the server should get and end turn message!
                    Debug.Log("client received end turn message!");
                    break;
                case CardMessageType.IncrementTurn:
                    // only the server should get and end turn message!
                    Debug.Log("client received increment turn message!");
                    manager.IncrementTurn();
                    break;             
                default:
                    Debug.Log("client received unknown message!");
                    break;
            }
        }
    }

    public void OnServerReceiveShortMessage(NetworkConnectionToClient client, RGNetworkShortMessage msg)
    {
        Debug.Log("SERVER RECEIVED SHORT MESSAGE::: " + msg.indexId + " " + msg.type);
        uint senderId = msg.indexId;
        CardMessageType type = (CardMessageType)msg.type;

        switch (type)
        {
            case CardMessageType.StartTurn:
                // nobody tells server to start a turn, so this shouldn't happen
                Debug.Log("server start turn message");
                break;
            case CardMessageType.EndTurn:
                // end turn is handled here because the player list is kept
                // in this class
                Debug.Log("server received end turn message");
                // note this player's turn has ended      
                int playerIndex = (int)senderId;
                playerTurnTakenFlags[playerIndex] = true;
                // find next player to ok to play and send them a message
                int nextPlayerId = -1;
                for (int i = playerIndex + 1; i < playerTurnTakenFlags.Count; i++)
                {
                    if (!playerTurnTakenFlags[i])
                    {
                        nextPlayerId = playerIDs[i];
                        break;
                    }
                }

                if (nextPlayerId != -1)
                {
                    // send my turn to the next player
                    NetworkConnectionToClient connection = NetworkServer.connections[nextPlayerId];
                    msg.indexId = (uint)nextPlayerId;
                    msg.type = (uint)CardMessageType.StartTurn;
                    connection.Send(msg);
                    Debug.Log("next client should receive message " + nextPlayerId);

                }
                else
                {
                    // need to increment the turn and set all the players to ready again
                    for (int i = 0; i < playerTurnTakenFlags.Count; i++)
                    {
                        playerTurnTakenFlags[i] = false;
                    }
                    manager.IncrementTurn();
                    manager.StartTurn();
                    Debug.Log("Turn is done - incrementing and starting again.");
                }
                break;
            case CardMessageType.IncrementTurn:
                Debug.Log("Server received increment message and did nothing.");
                break;
            default:
                break;
        }

    }

    public void OnClientReceiveLongMessage(RGNetworkLongMessage msg)
    {
        Debug.Log("CLIENT RECEIVED LONG MESSAGE::: " + msg.indexId + " " + msg.type);
        uint senderId = msg.indexId;
        CardMessageType type = (CardMessageType)msg.type;

        if (msg.indexId!=localPlayerID && !isServer)
        {
            // we don't send messages to ourself
            switch (type)
            {
                case CardMessageType.StartGame:
                    {
                        Debug.Log("client received message to start the game");
                        uint count = msg.count;
                        int element = 0;
                        for (int i = 0; i < count; i++)
                        { 
                            // the id is the order in the list
                            playerIDs.Add(i);

                            // then get player type
                            byte first = msg.payload.ElementAt(element);
                            byte second = msg.payload.ElementAt(element + 1);
                            byte third = msg.payload.ElementAt(element + 2);
                            byte fourth = msg.payload.ElementAt(element + 3);
                            int actualInt = first | (second << 8) | (third << 16) | (fourth << 24);
                            playerTypes.Add((PlayerType)actualInt);
                            // get length of player name
                            element += 4;
                            first = msg.payload.ElementAt(element);
                            second = msg.payload.ElementAt(element + 1);
                            third = msg.payload.ElementAt(element + 2);
                            fourth = msg.payload.ElementAt(element + 3);
                            actualInt = first | (second << 8) | (third << 16) | (fourth << 24);

                            // get player name
                            element += 4;
                            ArraySegment<byte> name = msg.payload.Slice(element, actualInt);
                            playerNames.Add(Encoding.ASCII.GetString(name));

                            element += actualInt;
                            Debug.Log("player being added : " + playerIDs[i] + " " + playerTypes[i] +
                                " " + playerNames[i]);
                        }

                    }
                    break;
                //case CardMessageType.ShowCards:
                //    uint count = msg.count;
                //    Debug.Log("client received a list of an opponents cards! " + count);

                //    List<int> cardIds = new List<int>((int)count);
                //    for (int i = 0; i < count * 4; i += 4)
                //    {
                //        byte first = msg.payload.ElementAt(i);
                //        byte second = msg.payload.ElementAt(i + 1);
                //        byte third = msg.payload.ElementAt(i + 2);
                //        byte fourth = msg.payload.ElementAt(i + 3);
                //        int actualInt = first | (second << 8) | (third << 16) | (fourth << 24);
                //        cardIds.Add(actualInt);
                //        Debug.Log(" :: " + actualInt + " :: ");
                //    }
                //    GameManager.instance.ShowOthersCards(cardIds);
                //    break;
                case CardMessageType.EndGame:
                    if (msg.count == 1)
                    {
                        int whoWins = BitConverter.ToInt32(msg.payload);
                        manager.EndGame(whoWins, false);
                        Debug.Log("received end game message and will now end game on client");

                    }
                    break;
                default:
                    break;
            }
        }
    }

    public bool CheckReadyToStart()
    {
        bool readyToStart = true;
        for (int i = 0; i < playerIDs.Count; i++)
        {
            if (playerTypes[i] == PlayerType.Any)
            {
                readyToStart = false;
                break;
            }
        }
        return readyToStart;
    }
    public void OnServerReceiveLongMessage(NetworkConnectionToClient client, RGNetworkLongMessage msg)
    {
        Debug.Log("SERVER RECEIVED LONG MESSAGE::: " + msg.indexId + " " + msg.type);
        uint senderId = msg.indexId;
        CardMessageType type = (CardMessageType)msg.type;

        if (msg.indexId != localPlayerID)
        {
            switch (type)
            {
                case CardMessageType.SharePlayerType:
                    {
                        uint count = msg.count;

                        Debug.Log("server received a player's type!" + count);
                        if (count == 1)
                        {
                            // turn the first element into an int
                            PlayerType playerType = (PlayerType)BitConverter.ToInt32(msg.payload);
                            int playerIndex = (int)msg.indexId;
                            playerTypes[playerIndex] = playerType;
                            playerTurnTakenFlags.Add(true);
                            Debug.Log("setting player type to " + playerType);

                            // check to see if we've got a player type for everybody!
                            if (CheckReadyToStart())
                            {
                                Debug.Log("Ready to start!");
                                GameManager.instance.RealGameStart();
                            }

                            // let the game manager display the new info
                            GameManager.instance.DisplayOtherPlayerTypes(playerNames[playerIndex],
                                 playerTypes[playerIndex]);
                        }
                    }
                    break;
                //case CardMessageType.ShowCards:  
                //    uint count = msg.count;
                //    Debug.Log("server received a list of an opponents cards!" + count);
                //    List<int> cardIds = new List<int>((int)count);
                //    for (int i = 0; i < count * 4; i += 4)
                //    {
                //        byte first = msg.payload.ElementAt(i);
                //        byte second = msg.payload.ElementAt(i + 1);
                //        byte third = msg.payload.ElementAt(i + 2);
                //        byte fourth = msg.payload.ElementAt(i + 3);
                //        int actualInt = first | (second << 8) | (third << 16) | (fourth << 24);
                //        cardIds.Add(actualInt);
                //        Debug.Log(" :: " + actualInt + " :: ");
                //    }
                //    GameManager.instance.ShowOthersCards(cardIds);
                //    break;
                case CardMessageType.EndGame:
                    if (msg.count == 1)
                    {
                        int whoWins = BitConverter.ToInt32(msg.payload);
                        manager.EndGame(whoWins, false);
                        Debug.Log("received end game message and will now end game on server");

                    }
                    break;
                default:
                    break;
            }
        }
    }

    public void SetupHandlers()
    {
        NetworkClient.RegisterHandler<RGNetworkShortMessage>(OnClientReceiveShortMessage);
        NetworkServer.RegisterHandler<RGNetworkShortMessage>(OnServerReceiveShortMessage);
        NetworkClient.RegisterHandler<RGNetworkLongMessage>(OnClientReceiveLongMessage);
        NetworkServer.RegisterHandler<RGNetworkLongMessage>(OnServerReceiveLongMessage);
    }
}
