using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System;
using System.Linq;
using System.Text;
using UnityEngine.InputSystem.Utilities;

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

public struct PlayerInfo
{
    public int playerID;
    public bool playerNetworkReadyFlag;
    public bool playerTurnTakenFlag;
    public PlayerType playerType;
}

public class RGNetworkPlayerList : NetworkBehaviour, IRGObserver
{
    public static RGNetworkPlayerList instance;

    public int localPlayerID;
    public string localPlayerName;
    public Dictionary<string, PlayerInfo> players = new Dictionary<string, PlayerInfo>(10);
    Dictionary<int, string> playerConnectionToName = new Dictionary<int, string>(10);
    //public List<int> playerIDs = new List<int>();
    public GameManager manager;

    //private List<bool> playerNetworkReadyFlags = new List<bool>();
    //private List<bool> playerTurnTakenFlags = new List<bool>();
    //public List<PlayerType> playerTypes = new List<PlayerType>();
    //public List<string> playerNames = new List<string>();

    private void Awake()
    {
        instance = this;
        DontDestroyOnLoad(this);
        SetupHandlers();   
    }

    public void Start()
    {
        Debug.Log("start run on RGNetworkPlayerList.cs");
    }

    public void SetupGameManager(GameManager man)
    {
        manager = man;
    }

    public void AddPlayer(int id, string name)
    {
        if (isServer)
        {
            Debug.Log("adding player to server : " + id);
            
            if (!players.ContainsKey(name))
            {
                players.TryAdd(name, new PlayerInfo
                {
                    playerID = id,
                    playerNetworkReadyFlag = true,
                    playerTurnTakenFlag = false,
                    playerType = PlayerType.Any,
                });
                playerConnectionToName.TryAdd(id, name);
            } else
            {
                // update the id because this is a reconnection
                PlayerInfo player;
                if(players.TryGetValue(name, out player))
                {
                    int priorID = player.playerID;
                    player.playerID = id;
                    players[name] = player;
                    if(playerConnectionToName.ContainsKey(priorID))
                    {
                        playerConnectionToName.Remove(priorID);
                        playerConnectionToName.TryAdd(id,name);
                    } else
                    {
                        Debug.Log("Error: connection wasn't in player conn dictionary.");
                    }
                } else
                {
                    Debug.Log("Error in setting new id value for existing player " + name);
                }
                
            }
            
            //playerIDs.Add(id);
            //playerNetworkReadyFlags.Add(true);
            //playerTurnTakenFlags.Add(false);
            //playerTypes.Add(PlayerType.Any);
            //playerNames.Add(name);
            
        } 
    }

    public void ResetAllPlayersToNotReady()
    {
        PlayerInfo player;
        if (players.Count > 1)
        {
            foreach (string key in players.Keys.ToList<string>())
            {
                if (players.TryGetValue(key, out player))
                {
                    player.playerType = PlayerType.Any;
                    players[key] = player;
                }
            }
        }
       
        //for (int i = 0; i < playerIDs.Count; i++)
        //{
        //    playerTypes[i] = PlayerType.Any;
        //}
    }

    public void SetPlayerType(PlayerType type)
    {
        if (isServer)
        {
            PlayerInfo player;
            if (players.TryGetValue(localPlayerName, out player))
            {
                Debug.Log("player type for local player set to: " + type);
                player.playerType = type;
                players[localPlayerName] = player;
            }
            //playerTypes[localPlayerID] = type;
            if (CheckReadyToStart())
            {
                manager.RealGameStart();

                // get the turn taking flags ready to go again
                if (players.Count > 1)
                {
                    foreach (string key in players.Keys.ToList<string>())
                    {
                        if (players.TryGetValue(key, out player))
                        {
                            player.playerTurnTakenFlag = false;
                            players[key] = player;
                        }
                    }
                }
               
                //for (int i = 0; i < playerTurnTakenFlags.Count; i++)
                //{
                //    playerTurnTakenFlags[i] = false;
                //}
            } else
            {
                Debug.Log("not ready to start!");
            }
        }
    }

    public Message CreateStartGameMessage()
    {
        Message msg;
        List<byte> data = new List<byte>(100);
        //int messageCount = players.Count;
        //for (int i=0; i<messageCount; i++)
        if (players.Count > 0)
        {
            foreach (string key in players.Keys)
            {
                // note that the player id is actually its order in this
                // message
                PlayerInfo player = players[key];
                byte[] type = BitConverter.GetBytes((int)player.playerType);
                int nameSize = key.Length;
                byte[] nameSizeBytes = BitConverter.GetBytes(nameSize);
                byte[] name = Encoding.ASCII.GetBytes(key);
                data.AddRange(type);
                data.AddRange(nameSizeBytes);
                data.AddRange(name);
            }
        }
      
        msg = new Message(CardMessageType.StartGame, data);
        return (msg);
    }

    public void RemovePlayer(int id)
    {
        if (!isServer) return;
        if(playerConnectionToName.ContainsKey(id))
        {
            playerConnectionToName.Remove(id);
        }
        //// get the index of the id
        //int index = playerIDs.FindIndex(x => x == id);
        //if (index != -1)
        //{
        //    playerIDs.Remove(id);
        //    playerNames.RemoveAt(index);
        //    playerTypes.RemoveAt(index);
        //    playerNetworkReadyFlags.RemoveAt(index);
        //    playerTurnTakenFlags.RemoveAt(index);
        //}
    }

    public int GetIntFromByteArray(int indexStart, ArraySegment<byte> payload)
    {
        int returnValue = 0;
        byte first = payload.ElementAt(indexStart);
        byte second = payload.ElementAt(indexStart + 1);
        byte third = payload.ElementAt(indexStart + 2);
        byte fourth = payload.ElementAt(indexStart + 3);
        returnValue = first | (second << 8) | (third << 16) | (fourth << 24);
        return returnValue;
    }

    public void UpdateObserver(Message data)
    {
        // send messages here over network to appropriate place(s)
        switch (data.Type)
        {
            case CardMessageType.StartGame:
                {
                    if (isServer)
                    {
                        // only servers start the game!
                        RGNetworkLongMessage msg = new RGNetworkLongMessage
                        {
                            indexId = (uint)localPlayerID,
                            type = (uint)data.Type,
                            count = (uint)players.Count,
                            payload = data.byteArguments.ToArray()
                        };
                        NetworkServer.SendToAll(msg);
                        Debug.Log("SERVER SENT a new player name and id to clients");
                    }
                }
                break;
            case CardMessageType.EndPhase:
                {
                    // turn taking is handled here because the list of players on 
                    // the network happens here
                    RGNetworkShortMessage msg = new RGNetworkShortMessage
                    {
                        indexId = (uint)localPlayerID,
                        type = (uint)data.Type
                    };
                    Debug.Log("update observer called end phase! ");
                    if (isServer)
                    {
                        // we've played so we're no longer on the ready list
                        //int playerIndex = localPlayerID;
                        PlayerInfo player;
                        if (players.TryGetValue(localPlayerName, out player))
                        {
                            player.playerTurnTakenFlag = true;
                            players[localPlayerName] = player;
                            //playerTurnTakenFlags[playerIndex] = true;
                        }

                        // find next player to play and send them a message
                        int nextPlayerId = -1;
                        string nextPlayerName = "";

                        if (players.Count > 1)
                        {
                            foreach (string key in players.Keys)
                            {
                                player = players[key];
                                if (!player.playerTurnTakenFlag)
                                {
                                    nextPlayerName = key;
                                    nextPlayerId = player.playerID;
                                    Debug.Log("first player not done is " + player.playerID);
                                    break;
                                }
                            }
                        }
                        
                        //for (int i = 0; i < playerTurnTakenFlags.Count; i++)
                        //{
                        //    if (!playerTurnTakenFlags[i])
                        //    {
                        //        nextPlayerId = i;
                        //        Debug.Log("first player not done is " + i);
                        //        break;
                        //    }
                        //}

                        if (nextPlayerId == -1)
                        {
                            Debug.Log("update observer everybody has ended phase!");
                            GamePhase nextPhase = manager.GetNextPhase();

                            // need to increment the turn and set all the players to ready again
                            //for (int i = 0; i < playerTurnTakenFlags.Count; i++)
                            //{
                            //    playerTurnTakenFlags[i] = false;
                            //}
                            if (players.Count > 1)
                            {
                                foreach (string key in players.Keys.ToList<string>())
                                {
                                    player = players[key];
                                    player.playerTurnTakenFlag = false;
                                    players[key] = player;
                                }
                            }

                            // tell all the clients to go to the next phase
                            msg.type = (uint)CardMessageType.StartNextPhase;
                            NetworkServer.SendToAll(msg);

                            // server needs to start their next phase too
                            manager.StartNextPhase();

                            if (nextPhase == GamePhase.DrawAndDiscard)
                            {
                                manager.IncrementTurn();
                                Debug.Log("Turn is done - incrementing and starting again.");
                            }
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
                {
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
            case CardMessageType.ShareDiscardNumber:
                {
                    RGNetworkLongMessage msg = new RGNetworkLongMessage
                    {
                        indexId = (uint)localPlayerID,
                        type = (uint)data.Type,
                        count = (uint)data.arguments.Count,
                        payload = data.arguments.SelectMany<int, byte>(BitConverter.GetBytes).ToArray()
                    };

                    if (!isServer)
                    {                   
                        NetworkClient.Send(msg);
                        Debug.Log("CLIENT IS SHOWING THEIR DISCOUNT AMOUNT AS " + data.ToString());
                    } else
                    {
                        // share it with everybody
                        NetworkServer.SendToAll(msg);
                    }
                }
                break;
            case CardMessageType.SendCardUpdates:
                {
                    RGNetworkLongMessage msg = new RGNetworkLongMessage
                    {
                        indexId = (uint)localPlayerID,
                        type = (uint)data.Type,
                        count = (uint)data.arguments.Count,
                        payload = data.arguments.SelectMany<int, byte>(BitConverter.GetBytes).ToArray()
                    };
                    Debug.Log("update observer called share updates");

                    if (isServer)
                    {
                        // send to all
                        NetworkServer.SendToAll(msg);
                        Debug.Log("SERVER SENT GAME PLAY UPDATES");
                    }
                    else
                    {
                        NetworkClient.Send(msg);
                        Debug.Log("CLIENT SENT GAMEPLAY UPDATES");
                    }
                }
                break;
            case CardMessageType.AddConnections:
                {
                    RGNetworkLongMessage msg = new RGNetworkLongMessage
                    {
                        indexId = (uint)localPlayerID,
                        type = (uint)data.Type,
                        count = (uint)data.arguments.Count,
                        payload = data.arguments.SelectMany<int, byte>(BitConverter.GetBytes).ToArray()
                    };
                    Debug.Log("update observer called add connections");

                    if (isServer)
                    {
                        // send to all
                        NetworkServer.SendToAll(msg);
                        Debug.Log("SERVER SENT ADD CONNECTIONS");
                    }
                    else
                    {
                        NetworkClient.Send(msg);
                        Debug.Log("CLIENT SENT ADD CONNECTIONS");
                    }
                }
                break;
            case CardMessageType.AttackUpdates:
                {
                    RGNetworkLongMessage msg = new RGNetworkLongMessage
                    {
                        indexId = (uint)localPlayerID,
                        type = (uint)data.Type,
                        count = (uint)data.arguments.Count,
                        payload = data.arguments.SelectMany<int, byte>(BitConverter.GetBytes).ToArray()
                    };
                    Debug.Log("update observer called share updates");

                    if (isServer)
                    {
                        // send to all
                        NetworkServer.SendToAll(msg);
                        Debug.Log("SERVER SENT ATTACK UPDATES");
                    }
                    else
                    {
                        NetworkClient.Send(msg);
                        Debug.Log("CLIENT SENT ATTACK UPDATES");
                    }
                }
                break;
            case CardMessageType.EndGame:
                {
                    RGNetworkShortMessage msg = new RGNetworkShortMessage
                    {
                        indexId = (uint)localPlayerID,
                        type = (uint)data.Type
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
            case CardMessageType.SendPlayedFacility:
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
                        Debug.Log("SERVER SENT PLAYED FACILITY MESSAGE");
                    }
                    else
                    {
                        NetworkClient.Send(msg);
                        Debug.Log("CLIENT SENT PLAYED FACILITY MESSAGE");
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
                case CardMessageType.StartNextPhase:
                    Debug.Log("received start next phase message");
                    manager.StartNextPhase();
                    break;
                case CardMessageType.EndPhase:
                    // only the server should get and end turn message!
                    Debug.Log("client received end phase message and it shouldn't!");
                    break;
                case CardMessageType.IncrementTurn:
                    Debug.Log("client received increment turn message!");
                    manager.IncrementTurn();
                    break;
                case CardMessageType.EndGame:
                    {
                        if (!manager.HasReceivedEndGame())
                        {
                            manager.SetReceivedEndGame(true);
                            manager.AddMessage(new Message(CardMessageType.EndGame));
                            manager.ShowEndGameCanvas();
                            Debug.Log("received end game message and will now end game on server");
                        }  
                    }
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
            case CardMessageType.StartNextPhase:
                // nobody tells server to start a turn, so this shouldn't happen
                Debug.Log("server start next phase message when it shouldn't!");
                break;
            case CardMessageType.EndPhase:
                // end turn is handled here because the player list is kept
                // in this class
                Debug.Log("server received end phase message from sender: " + senderId);

                // note this player's turn has ended
                PlayerInfo player;
                int playerIndex = (int)senderId;
                string playerName = playerConnectionToName[playerIndex];
                players.TryGetValue(playerName, out player);
                Debug.Log("player index : " +playerIndex + " " + playerName);
                player.playerTurnTakenFlag = true;
                players[playerName] = player;
                Debug.Log("got here");
                // find next player to ok to play and send them a message
                int nextPlayerId = -1;
                string nextPlayerName = "";

                if (players.Count > 1)
                {
                    foreach (string key in players.Keys)
                    {
                        player = players[key];
                        if (!player.playerTurnTakenFlag)
                        {
                            nextPlayerName = key;
                            nextPlayerId = player.playerID;
                            break;
                        }
                    }
                }
               
                //int nextPlayerId = -1;
                //for (int i = 0; i < playerTurnTakenFlags.Count; i++)
                //{
                //    if (!playerTurnTakenFlags[i])
                //    {
                //        nextPlayerId = playerIDs[i];
                //        break;
                //    }
                //}
                if (nextPlayerId == -1)
                {
                    Debug.Log("got here 3");
                    GamePhase nextPhase = manager.GetNextPhase();
                    Debug.Log("getting next phase : " + nextPhase);

                    // need to increment the turn and set all the players to ready again
                    //for (int i = 0; i < playerTurnTakenFlags.Count; i++)
                    //{
                    //    playerTurnTakenFlags[i] = false;
                    //}
                    if(players.Count > 1)
                    {
                        foreach (string key in players.Keys.ToList<string>())
                        {
                            player = players[key];
                            player.playerTurnTakenFlag = false;
                            players[key] = player;
                        }
                    }
                   
                    Debug.Log("sending start next phase");
                    // tell all the clients to go to the next phase
                    msg.indexId = (uint)localPlayerID;
                    msg.type = (uint)CardMessageType.StartNextPhase;
                    NetworkServer.SendToAll(msg);
                    Debug.Log("doing the next phase");
                    // server needs to start next phase as well
                    manager.StartNextPhase();
                    Debug.Log("checking to make sure it's not the next turn");
                    if (nextPhase == GamePhase.DrawAndDiscard)
                    {
                        manager.IncrementTurn();
                        Debug.Log("Turn is done - incrementing and starting again.");
                    }
                    Debug.Log("next phase stuff done");

                }
                break;
            case CardMessageType.IncrementTurn:
                Debug.Log("Server received increment message and did nothing.");
                break;
            case CardMessageType.EndGame:
                {
                    if (!manager.HasReceivedEndGame())
                    {
                        manager.SetReceivedEndGame(true);
                        manager.AddMessage(new Message(CardMessageType.EndGame));
                        manager.ShowEndGameCanvas();
                        Debug.Log("received end game message and will now end game on server");
                    }
                }
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
            Debug.Log("client received a legit message");

            // we don't send messages to ourself
            switch (type)
            {
                case CardMessageType.StartGame:
                    {
                       
                        uint count = msg.count;
                        Debug.Log("client received message to start the game " + count);
                        int element = 0;
                        PlayerInfo player;
                        for (int i = 0; i < count; i++)
                        {
                            // the id is the order in the list
                            //int existingPlayer = playerIDs.FindIndex(x => x == i);
                            //int actualInt = existingPlayer; 
                            
                            //if (existingPlayer == -1)
                            //{
                            // player type
                            int    playerType = GetIntFromByteArray(element, msg.payload);                           
                             //    playerIDs.Add(i);
                                // then get player type

                           //     playerTypes.Add((PlayerType)actualInt);
                            // get length of player name
                                element += 4;
                                int nameLength = GetIntFromByteArray(element, msg.payload);

                                // get player name
                                element += 4;
                                ArraySegment<byte> name = msg.payload.Slice(element, nameLength);
                                string playerName = (Encoding.ASCII.GetString(name));

                                element += nameLength;

                            if (players.TryGetValue(playerName, out player))
                            {
                                player.playerType = (PlayerType)playerType;
                                player.playerID = (int)senderId;
                                players[playerName] = player;
                            }
                            else
                            {
                                player.playerID = (int)senderId;
                                player.playerType = (PlayerType)playerType;
                                player.playerNetworkReadyFlag = true;
                                player.playerTurnTakenFlag = false;
                                players.TryAdd(playerName, player);
                            }
                            
                            if(playerConnectionToName.ContainsKey((int)senderId)) {
                                playerConnectionToName[(int)senderId] = playerName;
                            } else
                            {
                                playerConnectionToName.TryAdd((int)senderId, playerName);
                            }
                                Debug.Log("player being added : " + senderId + " " + playerType +
                                    " " + playerName);
                            //} else
                            //{
                            //    // when a game is reset we only need the player type again
                            //    Debug.Log("existing player is being reset");
                            //    actualInt = GetIntFromByteArray(element, msg.payload);
                            //    playerTypes[existingPlayer] = (PlayerType)actualInt;
                            //    Debug.Log("player " + playerNames[existingPlayer] + " already exists! new type is: " + playerTypes[existingPlayer]);
                            //}
                            

                           
                        }
                        // now start the next phase
                        manager.RealGameStart();
                    }
                    break;
                case CardMessageType.ShareDiscardNumber:
                    {
                        uint count = msg.count;

                        Debug.Log("client received a player's discard amount!" + count);
                        if (count == 1)
                        {
                            // turn the first element into an int
                            int discardCount = BitConverter.ToInt32(msg.payload);
                            int playerIndex = (int)msg.indexId;
                            string playerName = playerConnectionToName[playerIndex];
                            Debug.Log("setting player discard to " + discardCount);

                            // share with other players
                            //NetworkServer.SendToAll(msg);

                            // let the game manager display the new info
                            manager.DisplayGameStatusOpponent("Player " + playerName +
                                " discarded " + discardCount + " cards.");
                        }
                    }
                    break;
                case CardMessageType.SendCardUpdates:
                    {
                        int element = 0;
                        List<Updates> updates = new List<Updates>(6);
                        
                        int numberOfUpdates = GetIntFromByteArray(element, msg.payload);
                        Debug.Log("number of updates received is " + numberOfUpdates);
                        element += 4; 
                        GamePhase gamePhase =(GamePhase)GetIntFromByteArray(element, msg.payload);
                        Debug.Log("game phase for updates is " + gamePhase);
                        element += 4;
                        for (int i = 0; i < numberOfUpdates; i++)
                        {
                            int whatToDo = GetIntFromByteArray(element, msg.payload);
                            element += 4;
                            int facilityId = GetIntFromByteArray(element, msg.payload);
                            element += 4;
                            int cardId = GetIntFromByteArray(element, msg.payload);
                            element += 4;
                            
                            updates.Add(new Updates
                            {
                                WhatToDo=(AddOrRem)whatToDo,
                                UniqueFacilityID=facilityId,
                                CardID=cardId
                            });
                            Debug.Log("client received update message from opponent containing : " + facilityId + " and cardid " + cardId + "for game phase " + gamePhase);
                        }
                        manager.AddUpdatesFromOpponent(ref updates, gamePhase);
                        
                    }
                    break;
                case CardMessageType.SendPlayedFacility:
                    {
                        int element = 0;
                        if (msg.count == 2)
                        {
                            Debug.Log("client received facility message with 2 args");
                            int uniqueId = GetIntFromByteArray(element, msg.payload);
                            element += 4;             
                            int facilityId = GetIntFromByteArray(element, msg.payload);

                            manager.AddOpponentFacility(facilityId, uniqueId);
                            Debug.Log("received facility message from opponent with unique id " + uniqueId + " and card facility id " + facilityId);

                        }
                        Debug.Log("facility played with message count: " + msg.count);
                    }
                    break;
                case CardMessageType.AddConnections:
                    {
                        int element = 0;
                        List<FacilityConnectionInfo> updates = new List<FacilityConnectionInfo>(7);
                        int numberOfUpdates = GetIntFromByteArray(element, msg.payload);
                        element += 4;
                        int originalFacilityUniqueID = GetIntFromByteArray(element, msg.payload);
                        element += 4;
                        for (int i = 0; i < numberOfUpdates; i++)
                        {
                            int facilityId = GetIntFromByteArray(element, msg.payload);
                            element += 4;
                            int facilityZone = GetIntFromByteArray(element, msg.payload);
                            element += 4;

                            updates.Add(new FacilityConnectionInfo
                            {
                                UniqueFacilityID = facilityId,
                                WhichFacilityZone = facilityZone,
                            });
                            Debug.Log("client received connection message from opponent containing : " + facilityId + " and zone " + facilityZone);
                        }
                        manager.AddConnectionsFromOpponent(ref updates, originalFacilityUniqueID);

                    }
                    break;
                case CardMessageType.AttackUpdates:
                    {
                        int element = 0;
                        List<AttackUpdate> updates = new List<AttackUpdate>(6);

                        int numberOfUpdates = GetIntFromByteArray(element, msg.payload);
                        Debug.Log("number of updates received is " + numberOfUpdates);
                        element += 4;
                        for (int i = 0; i < numberOfUpdates; i++)
                        {
                            int facilityId = GetIntFromByteArray(element, msg.payload);
                            element += 4;
                            int amountChanged = GetIntFromByteArray(element, msg.payload);
                            element += 4;

                            updates.Add(new AttackUpdate
                            {
                              
                                UniqueFacilityID = facilityId,
                                ChangeInValue = amountChanged,
                            });
                            Debug.Log("client received update message from opponent containing : " + facilityId + " and attackchange " + amountChanged);
                        }
                        manager.AddAttackUpdatesFromOpponent(ref updates);

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
        if (players.Count < 2)
        {
            readyToStart = false;
        } else
        {
            PlayerInfo player;
            if (players.Count > 1)
            {
                foreach (string key in players.Keys)
                {
                    player = players[key];
                    if (player.playerType == PlayerType.Any)
                    {
                        Debug.Log("playertype is set to ANY, so can't start game");
                        readyToStart = false;
                        break;
                    }
                }
            }
           
            //for (int i = 0; i < playerIDs.Count; i++)
            //{
            //    if (playerTypes[i] == PlayerType.Any)
            //    {
            //        readyToStart = false;
            //        break;
            //    }
            //}
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
                        PlayerInfo player;

                        Debug.Log("server received a player's type!" + count);
                        if (count == 1)
                        {
                            // turn the first element into an int
                            PlayerType playerType = (PlayerType)BitConverter.ToInt32(msg.payload);
                            int playerIndex = (int)msg.indexId;
                            string playerName = playerConnectionToName[playerIndex];
                            player = players[playerName];
                            player.playerType = playerType;
                            player.playerTurnTakenFlag = true;
                            players[playerName] = player;
                            Debug.Log("setting opponent player type to " + playerType);

                            // check to see if we've got a player type for everybody!
                            if (CheckReadyToStart())
                            {
                                Debug.Log("Ready to start!");
                                manager.RealGameStart();
                                // get the turn taking flags ready to go again
                                if (players.Count > 1)
                                {
                                    foreach (string key in players.Keys.ToList<string>())
                                    {
                                        player = players[key];
                                        player.playerTurnTakenFlag = false;
                                        players[key] = player;
                                    }
                                }
                               
                                //for (int i=0; i<playerTurnTakenFlags.Count; i++)
                                //{
                                //    playerTurnTakenFlags[i] = false;
                                //}
                            }
                        }
                    }
                    break;
                case CardMessageType.ShareDiscardNumber:
                    {
                        uint count = msg.count;

                        Debug.Log("server received a player's discard amount!" + count);
                        if (count == 1)
                        {
                            // turn the first element into an int
                            int discardCount = BitConverter.ToInt32(msg.payload);
                            int playerIndex = (int)msg.indexId;
                            string playerName = playerConnectionToName[playerIndex];
                            Debug.Log("setting player discard to " + discardCount);

                            // share with other players
                            //NetworkServer.SendToAll(msg);

                            // let the game manager display the new info
                            manager.DisplayGameStatusOpponent("Player " + playerName + 
                                " discarded " + discardCount + " cards.");
                        }
                    }
                    break;
                case CardMessageType.SendCardUpdates:
                    {
                        int element = 0;
                        List<Updates> updates = new List<Updates>(6);
                        int numberOfUpdates = GetIntFromByteArray(element, msg.payload);
                        element += 4;
                        GamePhase gamePhase = (GamePhase)GetIntFromByteArray(element, msg.payload);
                        element += 4;

                        for (int i = 0; i < numberOfUpdates; i++)
                        {
                            int whatToDo = GetIntFromByteArray(element, msg.payload);
                            element += 4;
                            int facilityId = GetIntFromByteArray(element, msg.payload);
                            element += 4;
                            int cardId = GetIntFromByteArray(element, msg.payload);
                            element += 4;
                            updates.Add(new Updates
                            {
                                WhatToDo = (AddOrRem)whatToDo,
                                UniqueFacilityID = facilityId,
                                CardID = cardId
                            });
                            Debug.Log(whatToDo + " server received update message from opponent containing : " + facilityId + " and cardid " + cardId);

                        }
                        manager.AddUpdatesFromOpponent(ref updates, gamePhase);
                        Debug.Log("received update message from opponent of size " + numberOfUpdates);
                    }
                    break;
               
                case CardMessageType.SendPlayedFacility:
                    {
                        int element = 0;
                        if (msg.count == 2)
                        {
                            int uniqueId = GetIntFromByteArray(element, msg.payload);
                            element += 4;
                            int facilityId = GetIntFromByteArray(element, msg.payload);
                            element += 4;
                            manager.AddOpponentFacility(facilityId, uniqueId);
                            Debug.Log("received facility message from opponent with unique id " + uniqueId + " and card facility id " + facilityId);

                        }
                    }
                    break;
                case CardMessageType.AddConnections:
                    {
                        int element = 0;
                        List<FacilityConnectionInfo> updates = new List<FacilityConnectionInfo>(7);
                        int numberOfUpdates = GetIntFromByteArray(element, msg.payload);
                        Debug.Log("Number of updates in connection message is " + numberOfUpdates);
                        element += 4;
                        int originalFacilityUniqueID = GetIntFromByteArray(element, msg.payload);
                        element += 4;
                        for (int i = 0; i < numberOfUpdates; i++)
                        {
                            int facilityId = GetIntFromByteArray(element, msg.payload);
                            element += 4;
                            int facilityZone = GetIntFromByteArray(element, msg.payload);
                            element += 4;

                            updates.Add(new FacilityConnectionInfo
                            {
                                UniqueFacilityID = facilityId,
                                WhichFacilityZone = facilityZone,
                            });
                            Debug.Log("client received connection message from opponent containing : " + facilityId + " and zone " + facilityZone);
                        }
                        manager.AddConnectionsFromOpponent(ref updates, originalFacilityUniqueID);

                    }
                    break;
                case CardMessageType.AttackUpdates:
                    {
                        int element = 0;
                        List<AttackUpdate> updates = new List<AttackUpdate>(6);

                        int numberOfUpdates = GetIntFromByteArray(element, msg.payload);
                        Debug.Log("number of updates received is " + numberOfUpdates);
                        element += 4;
                        for (int i = 0; i < numberOfUpdates; i++)
                        {
                            int facilityId = GetIntFromByteArray(element, msg.payload);
                            element += 4;
                            int amountChanged = GetIntFromByteArray(element, msg.payload);
                            element += 4;

                            updates.Add(new AttackUpdate
                            {

                                UniqueFacilityID = facilityId,
                                ChangeInValue = amountChanged,
                            });
                            Debug.Log("client received update message from opponent containing : " + facilityId + " and attackchange " + amountChanged);
                        }
                        manager.AddAttackUpdatesFromOpponent(ref updates);

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
