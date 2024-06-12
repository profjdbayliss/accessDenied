using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Mirror;

public class GameManager : MonoBehaviour, IDragHandler, IRGObservable
{
    // Deck readers and resulting card lists.
    public CardReader energyDeckReader;
    public CardReader waterDeckReader;
    public bool mCreateEnergyAtlas = false;
    public bool mCreateWaterAtlas = false;
    public List<Card> energyCards;
    public List<Card> waterCards;

    // where are we in game phases?
    GamePhase mGamePhase = GamePhase.Start;
    GamePhase mPreviousGamePhase = GamePhase.Start;

    // Various turn and game info.
    bool myTurn = false;
    int turnTotal = 0;

    // set up the proper player cards and type
    PlayerType playerType = PlayerType.Energy;
    
    public GameObject playerDeckList;
    TMPro.TMP_Dropdown playerDeckChoice;
    public bool gameStarted = false;

    // has everything been set?
    bool isInit = false;

    // keep track of all game messages
    MessageQueue mMessageQueue = new MessageQueue();
    Message mMessage;

    // network connections
    RGNetworkPlayerList mRGNetworkPlayerList;
    bool isServer = true; 

    // other classes observe this one's gameplay data
    List<IRGObserver> mObservers = new List<IRGObserver>(20);

    // player types
    public CardPlayer energyPlayer;
    public CardPlayer waterPlayer;

    // var's we use so we don't have to switch between
    // the player types for generic stuff
    public CardPlayer actualPlayer;
    public TextMeshProUGUI mTurnText;
    public TextMeshProUGUI mPhaseText;
    public GameObject gameCanvas;
    public GameObject startScreen;
    public GameObject tiles;

    // active player
    public TextMeshProUGUI activePlayerText;
    public Color activePlayerColor;

    // WORK
    public GameObject yarnSpinner;

    // end game info
    public GameObject endGameCanvas;
    public TMP_Text endGameText;

    public int activePlayerNumber;

    public Camera cam;

    public TextMeshProUGUI titlee;

    // static instance for this class
    public static GameManager instance;

    public void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        startScreen.SetActive(true);

        // read water deck
        CardReader reader = waterDeckReader.GetComponent<CardReader>();
        if (reader != null)
        {
            waterCards = reader.CSVRead(mCreateWaterAtlas);
            waterPlayer.cards = waterCards;
            waterPlayer.playerType = PlayerType.WaterAndWasteWater;
        }
        else
        {
            Debug.Log("Water deck reader is null.");
        }

        // read energy deck
        reader = energyDeckReader.GetComponent<CardReader>();
        if (reader != null)
        {
            energyCards = reader.CSVRead(mCreateEnergyAtlas);
            energyPlayer.cards = energyCards;
            energyPlayer.playerType = PlayerType.Energy;

        }
        else
        {
            Debug.Log("Energy deck reader is null.");
        }

    }

    // Set up the main player of the game

    public void SetupActors()
    {
        // we should know when choice they
        // wanted by now and can set up
        // appropriate values
        if (playerType==PlayerType.Energy)
        {
            actualPlayer = energyPlayer;
            actualPlayer.cards = energyCards;
            actualPlayer.playerType = PlayerType.Energy;
        }
        else if (playerType==PlayerType.WaterAndWasteWater)
        {
            actualPlayer = waterPlayer;
            actualPlayer.cards = waterCards;
            actualPlayer.playerType = PlayerType.WaterAndWasteWater;
        }

        // Initialize the deck info and set various
        // player zones active
        actualPlayer.InitializeCards();
        actualPlayer.cardDropZone.SetActive(true);
        actualPlayer.handDropZone.SetActive(true);
        actualPlayer.playerPlayedZone.SetActive(true);
    }

    // Update is called once per frame
    void Update()
    {
        if (isInit)
        {
            if (gameStarted)
            {
                HandlePhases(mGamePhase);

                // if we want to end the game early
                //if (isServer)
                //{
                //    if (!endGame && turnTotal >= 4)
                //    {
                //        EndGame(0, true);
                //    }
                //}
                
            }
           
            // always notify observers in case there's a message
            // waiting to be processed.
            NotifyObservers();

        } else
        {
            // the network takes a while to start up and 
            // we wait for it.
            mRGNetworkPlayerList = RGNetworkPlayerList.instance;
            if (mRGNetworkPlayerList != null)
            {
                // means network init is done
                // and we're joined
                RegisterObserver(mRGNetworkPlayerList);
                isServer = mRGNetworkPlayerList.isServer;
                CardPlayer player = GameObject.FindObjectOfType<CardPlayer>();
                if (player != null)
                {
                    // player is initialized and ready to go
                    // this follows the network init and also
                    // takes a while to happen
                    isInit = true;
                }
            }
        }

    }

    // Handle all the card game phases with
    // this simple state machine.
    public void HandlePhases(GamePhase phase)
    {
        // keep track of 
        bool phaseJustChanged = false;
        mGamePhase = phase;
        if (!mGamePhase.Equals(mPreviousGamePhase))
        {
            phaseJustChanged = true;
            mPhaseText.text = mGamePhase.ToString();
            mPreviousGamePhase = phase;
        }

        switch (phase)
        {
            case GamePhase.Start:
                // start of game phase
                // handled with specialty code outside of this
                break;
            case GamePhase.DrawAndDiscard:
                if (phaseJustChanged)
                {
                    // draw cards if necessary
                    actualPlayer.DrawCards();
                    // set the discard area to work if necessary
                } else
                {
                    // check for discard and if there's a discard draw again
                }
                break;
            case GamePhase.Defense:
                break;
            case GamePhase.Vulnerability:
                break;
            case GamePhase.Mitigate:
                break;
            case GamePhase.Attack:
                break;
            case GamePhase.AddStation:
                // we only need one cycle for this particular
                // phase as it's automated.
                actualPlayer.DrawFacility(true, 0);
                EndPhase();
                // network messages will take care of forwarding the phase
                break;
            case GamePhase.End:
                // end of game phase
                break;
            default:
                break;
        }
    }

    // Called when pressing the button to start
    // Doesn't actually start the game until ALL
    // the players connected have pressed their start buttons.
    public void StartGame()
    {
        // basic init of player
        SetupActors();

        // init various objects to be used in the game
        gameCanvas.SetActive(true);
        startScreen.SetActive(false); // Start menu isn't necessary now
        turnTotal = 0;
        mTurnText.text = "Turn: " + GetTurn();
        mPhaseText.text = "Phase: " + mGamePhase.ToString();
       
        //activePlayerText.text = playerType + " Player";
        //Debug.Log("set active player to be: " + playerType);
        //activePlayerColor = new Color(0.0f, 0.4209991f, 1.0f, 1.0f);
        ///activePlayerText.color = activePlayerColor;
        //yarnSpinner.SetActive(true);
       
        // tell everybody else of this player's type
        if (!isServer)
        {
            Message msg;
            List<int> tmpList = new List<int>(1);
            tmpList.Add((int)playerType);
            msg = new Message(CardMessageType.SharePlayerType, tmpList);
            AddMessage(msg);
        } else
        {
            RGNetworkPlayerList.instance.SetPlayerType(playerType);
        }
    }

    public void RealGameStart()
    {
        // send out the starting message with all player info
        // and start the next phase
        if (isServer)
        {
            Message msg = RGNetworkPlayerList.instance.CreateStartGameMessage();
            AddMessage(msg);
        }

        // in this game people go in parallel to each other
        // per phase
        myTurn = true;
        gameStarted = true;

        // draw our first 2 pt facility
        actualPlayer.DrawFacility(false, 2);

        // make sure to show all our cards
        foreach (GameObject card in actualPlayer.HandList)
        {
            card.SetActive(true);
        }
        foreach (GameObject card in actualPlayer.ActiveCardList)
        {
            card.SetActive(true);
        }

        // go on to the next phase
        mGamePhase = GamePhase.DrawAndDiscard;

    }

    // WORK needs to be redone
    public void OnDrag(PointerEventData pointer)
    {
        if (tiles.gameObject.activeSelf) // Check to see if the gameobject this is attached to is active in the scene
        {
            // Create a vector2 to hold the previous position of the element and also set our target of what we want to actually drag.
            Vector2 tempVec2 = default(Vector2);
            RectTransform target = tiles.gameObject.GetComponent<RectTransform>();
            Vector2 tempPos = target.transform.localPosition;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(target, pointer.position - pointer.delta, pointer.pressEventCamera, out tempVec2) == true) // Check the older position of the element and see if it was previously
            {
                Vector2 tempNewVec = default(Vector2); // Create a new Vec2 to track the current position of the object
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(target, pointer.position, pointer.pressEventCamera, out tempNewVec) == true)
                {
                    tempPos.x += tempNewVec.x - tempVec2.x;
                    tempPos.y = tiles.transform.localPosition.y;
                    tiles.transform.localPosition = tempPos;
                }
            }
        }
    }

    // WORK rewrite for this card game
    public void ShowEndGameCanvas(int gameState)
    {
        endGameCanvas.SetActive(true);
        if(gameState == 1)
        {
            endGameText.text = "One of the facilities is down.\nMalicious Player Win";
        }
        else if(gameState == 2)
        {
            endGameText.text = "Out of time.\nResilience Player(s) Win";
        }
    }

    // WORK there is no menu?????
    public void BackToMenu()
    {

        if (NetworkServer.active && NetworkClient.isConnected)
        {
            NetworkManager.singleton.StopHost();
        }
        else if (NetworkServer.active)
        {
            NetworkManager.singleton.StopServer();
        }
        else if (NetworkClient.isConnected)
        {
            NetworkManager.singleton.StopClient();
        }
        Destroy(RGNetworkManager.singleton.gameObject);
       // SceneManager.LoadScene(0);

    }

    // Called by dropdown list box to set up the player type
    public void SetPlayerType()
    {
        if (playerDeckChoice == null)
        {
            playerDeckChoice = playerDeckList.GetComponent<TMPro.TMP_Dropdown>();
            if (playerDeckChoice == null)
            {
                Debug.Log("deck choice is null!");
            }
        }

        if (playerDeckChoice != null)
        {
            // set this player's type
            switch (playerDeckChoice.value)
            {
                case 0:
                    playerType = PlayerType.Energy;
                    break;
                case 1:
                    playerType = PlayerType.WaterAndWasteWater;
                    break;
                default:
                    break;
            }

            // display player type on view???
            Debug.Log("player type set to be " + playerType);
        }
        
    }

    // called whenever the player discards a card?
    public void Discard()
    {

    }

    // WORK Displays opponent info - need to put on GUI

    public void DisplayOtherPlayerTypes( string playerName, PlayerType type)
    {
        // need to do something with a view here...
        Debug.Log("The player " + playerName + " joined with type " + type);
    }

    // WORK rewrite in terms of win conditions
    public void EndGame(int whoWins, bool sendMessage)
    {
        if (sendMessage)
        {
            List<int> winner = new List<int>(1);
            winner.Add(whoWins);
            mMessageQueue.Enqueue(new Message(CardMessageType.EndGame, winner));
            ShowEndGameCanvas(whoWins);
        }
        else
        {
            ShowEndGameCanvas(whoWins);
        }
        //endGame = true;
    }

    // Show the cards and game UI for player.
    public void ShowPlayUI()
    {
        actualPlayer.handDropZone.SetActive(true);
        actualPlayer.cardDropZone.SetActive(true);
    }

    // Hide the cards and game UI for the player.
    public void HidePlayUI()
    {
        actualPlayer.handDropZone.SetActive(false);
        actualPlayer.cardDropZone.SetActive(false);
    }

    // Ends the phase.
    public void EndPhase()
    {
        if (myTurn)
        {
            HidePlayUI();
            AddMessage(new Message(CardMessageType.EndPhase));
            myTurn = false;
        }
    }

    // Gets the next phase.
    public GamePhase GetNextPhase()
    {
        GamePhase nextPhase = GamePhase.Start;

        switch(mGamePhase)
        {
            case GamePhase.Start:
                nextPhase = GamePhase.DrawAndDiscard;
                break;
            case GamePhase.DrawAndDiscard:
                nextPhase = GamePhase.Defense;
                break;
            case GamePhase.Defense:
                nextPhase = GamePhase.Vulnerability;
                break;
            case GamePhase.Vulnerability:
                nextPhase = GamePhase.Mitigate;
                break;
            case GamePhase.Mitigate:
                nextPhase = GamePhase.Attack;
                break;
            case GamePhase.Attack:
                nextPhase = GamePhase.AddStation;
                break;
            case GamePhase.AddStation:
                nextPhase = GamePhase.DrawAndDiscard;
                break;
            case GamePhase.End:
                nextPhase = GamePhase.End;
                break;
            default:
                break;
        }

        return nextPhase;
    }

    // Increments a turn. Note that turns consist of multiple phases.
    public void IncrementTurn()
    {
        turnTotal++;
        mTurnText.text = "Turn: " + GetTurn();
        if (isServer)
        {
            Debug.Log("server adding increment turn message");
            AddMessage(new Message(CardMessageType.IncrementTurn));
        }
    }

    // Starts the next phase.
    public void StartNextPhase()
    {
        if (!myTurn)
        {
            foreach (GameObject card in actualPlayer.HandList)
            {
                card.SetActive(true);
            }
            foreach (GameObject card in actualPlayer.ActiveCardList)
            {
                card.SetActive(true);
            }

            myTurn = true;
            mGamePhase = GetNextPhase();
            ShowPlayUI();
            Debug.Log("play ui shown");
        }

    }

    // Gets which turn it is.
    public int GetTurn()
    {
        return turnTotal;
    }

    // Adds a message to the message queue for the network.
    public void AddMessage(Message msg)
    {
        mMessageQueue.Enqueue(msg);
    }

    // Registers an observer of the message queue.
    public void RegisterObserver(IRGObserver o)
    {
        if (!mObservers.Exists(x => x == o))
        {
            mObservers.Add(o);
        }
    }
   
    // Registers and observer of the message queue.
    public void RemoveObserver(IRGObserver o)
    { 
        if (mObservers.Exists(x => x == o) )
        {
            mObservers.Remove(o);
        }
    }

    // Notifies all observers that there is a message.
    public void NotifyObservers()
    {
        if (!mMessageQueue.IsEmpty())
        {
            while (!mMessageQueue.IsEmpty())
            {
                Message m = mMessageQueue.Dequeue();
                foreach (IRGObserver o in mObservers)
                {
                    o.UpdateObserver(m);
                }
            }
        }
    }
}
