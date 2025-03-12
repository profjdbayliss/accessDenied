using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Mirror;
using System.Linq;
using Yarn.Unity;
using System.Xml;
using UnityEngine.PlayerLoop;
using System.ComponentModel;
using System.Text;

public class GameManager : MonoBehaviour, IRGObservable
{
    // Deck readers and resulting card lists
    public GameObject energyDeckReader;
    public GameObject waterDeckReader;
    WebCardReader webEnergyCardReader;
    WebCardReader webWaterCardReader;
    CardReader energyCardReader;
    CardReader waterCardReader;

    public List<ReadInCardData> energyCards;
    public List<ReadInCardData> waterCards;
    private bool mWaterReaderLoaded = false;
    private bool mPowerReaderLoaded = false;
    private bool mIsNetworkListReady = false;

    // UI elements to allow for more animations
    public UserInterface GameUI;

    // where are we in game phases?
    GamePhase mGamePhase = GamePhase.Start;
    GamePhase mPreviousGamePhase = GamePhase.Start;

    // Various turn and game info.
    bool myTurn = false;
    int turnTotal = 0;
    int mMaxTurn = 30;

    // set up the proper player cards and type
    PlayerType playerType = PlayerType.Energy;
    PlayerType opponentType = PlayerType.Water;
    
    public GameObject playerDeckList;
    TMPro.TMP_Dropdown playerDeckChoice;
    public bool gameStarted = false;

    // var's for game rules
    public readonly int MAX_DISCARDS = 2;
    public readonly int MAX_DEFENSE = 1;
    int mNumberDiscarded = 0;
    int mNumberDefense = 0;
    bool mIsDiscardAllowed = false;
    bool mIsDefenseAllowed = false;
    bool mAllowVulnerabilitiesPlayed = false;
    bool mAllowMitigationPlayed = false;
    bool mAllowConnections = false;
    bool mReceivedEndGame = false;
    bool mStartGameRun = false;
    bool mWaitingForInstantCardResolution = false;
    bool mRestarted = false;
    public GameObject PlayHaltButton;
    //public GameObject AllDiscardArea;
    Card mCacheInstantCard = null;
    Updates mCacheInstantCardPlayInfo;

    // has everything been set?
    bool isInit = false;

    // keep track of all game messages
    MessageQueue mMessageQueue = new MessageQueue();

    // network connections
    RGNetworkPlayerList mRGNetworkPlayerList;
    bool isServer = true; 

    // other classes observe this one's gameplay data
    List<IRGObserver> mObservers = new List<IRGObserver>(20);

    // var's we use so we don't have to switch between
    // the player types for generic stuff
    public CardPlayer actualPlayer;
    public CardPlayer opponentPlayer;
    public GameObject opponentPlayedZone;
    public TextMeshProUGUI mTurnText;
    public TextMeshProUGUI mPhaseText;
    public TextMeshProUGUI mPlayerName;
    public TextMeshProUGUI mPlayerDeckType;
    public TextMeshProUGUI mOpponentName;
    public TextMeshProUGUI mOpponentDeckType;
    public GameObject mEndPhaseButton;
    public GameObject gameCanvas;
    public GameObject startScreen;
    public GameObject waitingScreen;
    public GameObject tiles;

    // game status text
    public TextMeshProUGUI playerStatusText;
    public TextMeshProUGUI opponentStatusText;
    // active player
    public TextMeshProUGUI activePlayerText;
    public Color activePlayerColor;

    // Tutorial 
    public GameObject yarnSpinner;
    private DialogueRunner runner;
    private GameObject background;
    private bool skip;
    private bool skipClicked;


    // end game info
    public GameObject endGameCanvas;
    public GameObject InstantCardAlert;

    public TMP_Text endGameText;

    public int activePlayerNumber;

    public Camera cam;

    public TextMeshProUGUI titlee;

    // static instance for this class
    public static GameManager instance;

    static bool hasStartedAlready = false;


    public void Awake()
    {
        instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        mStartGameRun = false;
        Debug.Log("start run on GameManager");
        if (!hasStartedAlready)
        {
            startScreen.SetActive(true);
            webEnergyCardReader = energyDeckReader.GetComponent<WebCardReader>();
            webWaterCardReader = waterDeckReader.GetComponent<WebCardReader>();
            energyCardReader = energyDeckReader.GetComponent<CardReader>();
            waterCardReader = waterDeckReader.GetComponent<CardReader>();


            // Set dialogue runner for tutorial
            runner = yarnSpinner.GetComponent<DialogueRunner>();
            background = yarnSpinner.transform.GetChild(0).GetChild(0).gameObject;
           // Debug.Log(background);
            hasStartedAlready = true;
        } else
        {
            Debug.Log("start is being run multiple times!");
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
            actualPlayer.playerType = PlayerType.Energy;
            actualPlayer.DeckName = "power";
        }
        else if (playerType==PlayerType.Water)
        {
            actualPlayer.playerType = PlayerType.Water;
            actualPlayer.DeckName = "water";
        }

        // Initialize the deck info and set various
        // player zones active    
        actualPlayer.InitializeCards();
        actualPlayer.handDropZone.SetActive(true);
        actualPlayer.playerDropZone.SetActive(true);
        GameUI.AddCommand(UICommandType.ShowDiscard);
    }

    // Update is called once per frame
    void Update()
    {
        if (isInit)
        {
            if (gameStarted)
            {
                // we end the game when we're out of facilities immediately
                if ((actualPlayer.ActiveFacilities.Count + actualPlayer.FacilityIDs.Count)==0)
                {
                    mGamePhase = GamePhase.End;
                }

                HandlePhases(mGamePhase);        
            }
           
            // always notify observers in case there's a message
            // waiting to be processed.
            NotifyObservers();

        } else
        {

            // read water deck
            if (!mWaterReaderLoaded)
            {
                // WORK: comment web version out for now
                //if (webWaterCardReader != null && webWaterCardReader.IsDone)
                //{
                //    waterCards = webWaterCardReader.Cards;
                //    CardPlayer.AddCards(waterCards);
                //    Debug.Log("number of cards in all cards is: " + CardPlayer.cards.Count);
                //    mWaterReaderLoaded = true;
                //}
                //else 
                if (waterCardReader != null && waterCardReader.IsDone)
                {
                    waterCards = waterCardReader.Cards;
                    CardPlayer.AddCards(waterCards);
                    Debug.Log("number of cards in all cards is: " + CardPlayer.cards.Count);
                    mWaterReaderLoaded = true;
                }

            }
          
            if (!mPowerReaderLoaded)
            {
                //if (webEnergyCardReader != null && webEnergyCardReader.IsDone)
                //{
                //    energyCards = webEnergyCardReader.Cards;
                //    CardPlayer.AddCards(energyCards);
                //    Debug.Log("number of cards in all cards is: " + CardPlayer.cards.Count);
                //    mPowerReaderLoaded = true;
                //}
                //else
                if (energyCardReader != null && energyCardReader.IsDone)
                {
                    energyCards = energyCardReader.Cards;
                    CardPlayer.AddCards(energyCards);
                    Debug.Log("number of cards in all cards is: " + CardPlayer.cards.Count);
                    mPowerReaderLoaded = true;
                }
            }
           
            if (!mIsNetworkListReady)
            {
                // the network takes a while to start up and 
                // we wait for it.
                mRGNetworkPlayerList = RGNetworkPlayerList.instance;
                if (mRGNetworkPlayerList != null)
                {
                    mIsNetworkListReady = true;
                }
            }

            if (mIsNetworkListReady && mPowerReaderLoaded && mWaterReaderLoaded)
            {
                // player is initialized and ready to go
                // this follows the network init and also
                // takes a while to happen
                isInit = true;
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

            SkipTutorial();
        }

        switch (phase)
        {
            case GamePhase.Start:
                // start of game phase
                // handled with specialty code outside of this
                break;
            case GamePhase.DrawAndDiscard:
                if (phaseJustChanged && !skip)
                {

                    runner.StartDialogue("DrawAndDiscard");
                    background.SetActive(true);
                }

                if (phaseJustChanged)
                {
                    DisplayGameStatusPlayer("Discard phase begun.");
                    mIsDiscardAllowed = true;
                    GameUI.AddCommand(UICommandType.ShowDiscard);
                    // draw cards if necessary
                    actualPlayer.DrawCards();
                    // set the discard area to work if necessary
                    //actualPlayer.discardDropZone.SetActive(true);
                    mNumberDiscarded = 0;
                }
                else
                {
                    // draw cards if necessary
                    actualPlayer.DrawCards();

                    // check for discard and if there's a discard draw again
                    if (mNumberDiscarded == MAX_DISCARDS)
                    {
                        DisplayGameStatusPlayer(mPlayerName.text + " has reached the maximum discard number. Please hit end phase to continue.");
                    }
                    else
                    {
                        if (mIsDiscardAllowed)
                        {
                            mNumberDiscarded += actualPlayer.HandlePlayCard(GamePhase.DrawAndDiscard, opponentPlayer);
                        }
                    }
                }
                break;
            case GamePhase.Defense:
                if (phaseJustChanged && !skip)
                {
                    runner.StartDialogue("Defense"); 
                    background.SetActive(true);
                }

                if (phaseJustChanged)
                {
                    DisplayGameStatusPlayer("Defense phase: Please play a defense card if you have it.");
                    mIsDefenseAllowed = true;
                }

                if (!mIsDefenseAllowed)
                {
                    // do nothing - most common case
                }
                else
                if (mNumberDefense >= MAX_DEFENSE)
                {
                    mIsDefenseAllowed = false;
                    DisplayGameStatusPlayer(mPlayerName.text + " has played the maximum number of defense cards. Please hit end phase to continue.");
                }
                else
                if (!actualPlayer.CheckForCardsOfType(CardType.Defense, actualPlayer.HandCards))
                {
                    mIsDefenseAllowed = false;
                    // if player has no defense cards to play
                    // then let them know.
                    DisplayGameStatusPlayer(mPlayerName.text + " has no defense cards. Please hit end phase to continue.");
                }
                else if (mIsDefenseAllowed)
                {
                    mNumberDefense += actualPlayer.HandlePlayCard(GamePhase.Defense, opponentPlayer);
                }
                break;
            case GamePhase.Vulnerability:
                if (phaseJustChanged && !skip)
                {
                    runner.StartDialogue("Vulnerability");
                    background.SetActive(true);
                }
                if (!mWaitingForInstantCardResolution)
                {
                    if (!phaseJustChanged)
                    {
                        if (!mAllowVulnerabilitiesPlayed)
                        {
                            // do nothing - most common scenario
                        }
                        else
                        if (actualPlayer.GetAmountSpentOnVulnerabilities() >= actualPlayer.GetTotalFacilityValue())
                        {
                            mAllowVulnerabilitiesPlayed = false;
                            DisplayGameStatusPlayer(mPlayerName.text + " has spent their facility points. Please push End Phase to continue.");
                        }
                        else
                        {
                            actualPlayer.HandlePlayCard(GamePhase.Vulnerability, opponentPlayer);
                        }
                    }
                    else
                    if (phaseJustChanged
                        && !(actualPlayer.CheckForCardsOfType(CardType.Vulnerability, actualPlayer.HandCards) ||
                        actualPlayer.CheckForCardsOfType(CardType.LateralMovement, actualPlayer.HandCards) ||
                        actualPlayer.CheckForCardsOfType(CardType.Instant, actualPlayer.HandCards)))
                    {
                        mAllowVulnerabilitiesPlayed = false;
                        DisplayGameStatusPlayer(mPlayerName.text + " has no vulnerability cards. Please push End Phase to continue.");
                    }
                    else if (phaseJustChanged)
                    {
                        DisplayGameStatusPlayer("Vulnerability phase: Let the other player know they have a vulnerability.");

                        mAllowVulnerabilitiesPlayed = true;
                    }
                }
                break;
            case GamePhase.Mitigate:
                if (phaseJustChanged && !skip)
                {
                    runner.StartDialogue("Mitigate");
                    background.SetActive(true);
                }

                if (!phaseJustChanged)
                {
                    if (mAllowMitigationPlayed)
                    {
                        actualPlayer.HandlePlayCard(GamePhase.Mitigate, opponentPlayer);
                    }
                }
                if (phaseJustChanged
                    && !actualPlayer.CheckForCardsOfType(CardType.Mitigation, actualPlayer.HandCards))
                {
                    mAllowMitigationPlayed = false;
                    // if player has no cards to play
                    // let them know
                    DisplayGameStatusPlayer(mPlayerName.text + " has no Mitigation cards. Please push End Phase to continue.");
                }
                else if (phaseJustChanged)
                {
                    DisplayGameStatusPlayer("Mitigation phase: Please mitigate any vulnerabilities on your own facilities if possible.");

                    mAllowMitigationPlayed = true;
                }

                break;
            case GamePhase.Attack:
                if (phaseJustChanged && !skip)
                {
                    runner.StartDialogue("Attack");
                    background.SetActive(true);
                }

                if (phaseJustChanged)
                {
                    DisplayGameStatusPlayer("Attack dice have been rolled. Hit End Phase to continue.");
                    actualPlayer.HandleAttackPhase(opponentPlayer);
                    opponentPlayer.DiscardAllInactiveCards(DiscardFromWhere.MyPlayZone, false, -1);
                }

                break;
            case GamePhase.AddStation:
                if (phaseJustChanged && !skip)
                {
                    runner.StartDialogue("AddStation");
                    background.SetActive(true);
                }

                if (phaseJustChanged)
                {
                    // we only need one cycle for this particular
                    // phase as it's automated.
                    Card card = actualPlayer.DrawFacility(true, -1, -1, 0);
                    // send message about what facility got drawn                 
                    if (card != null)
                    {
                        AddMessage(new Message(CardMessageType.SendPlayedFacility, card.UniqueID, card.data.data.cardID));
                        DisplayGameStatusPlayer("Both players drew a station card. Please push End Phase to continue.");
                    }
                    else
                    {
                        EndPhase();
                    }

                }
                break;
            case GamePhase.AddConnections:
                if (phaseJustChanged && !skip)
                {
                    runner.StartDialogue("AddConnections");
                    background.SetActive(true);
                    skip = true;
                }
                if (!phaseJustChanged)
                {
                    actualPlayer.HandleConnections(false);
                    List<int> playsForMessage = new List<int>(5);
                    actualPlayer.GetNewConnectionsInMessageFormat(ref playsForMessage);

                    if (playsForMessage.Count > 0)
                    {
                        Message msg = new Message(CardMessageType.AddConnections, playsForMessage);
                        AddMessage(msg);
                    }
                }
                else
                {
                    DisplayGameStatusPlayer("Connection phase: Please connect your new facilities or add connections to old ones.");
                    mAllowConnections = true;
                    actualPlayer.HandleConnections(true);
                }

                break;
            case GamePhase.End:
                // end of game phase
                if (phaseJustChanged)
                {
                    Debug.Log("end game has happened. Sending message to other player.");
                    int playerScore = actualPlayer.GetFacilityScores() + actualPlayer.GetConnectionScores();
                    AddMessage(new Message(CardMessageType.EndGame));
                }
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
        if (!mStartGameRun)
        {
            Debug.Log("running start of game");
            // basic init of player
            SetupActors();

            // init various objects to be used in the game
            gameCanvas.SetActive(false);
            waitingScreen.SetActive(true);
            turnTotal = 0;
            mTurnText.text = "Turn: " + GetTurn();
            mPhaseText.text = "Phase: " + mGamePhase.ToString();
            mPlayerName.text = RGNetworkPlayerList.instance.localPlayerName;
            mPlayerDeckType.text = "" + playerType;


            mRGNetworkPlayerList.SetupGameManager(this);

            // means network init is done
            // and we're joined
            RegisterObserver(mRGNetworkPlayerList);
            isServer = mRGNetworkPlayerList.isServer;
            CardPlayer player = GameObject.FindObjectOfType<CardPlayer>();
            // tell everybody else of this player's type
            if (!isServer)
            {
                Message msg;
                List<int> tmpList = new List<int>(1);
                tmpList.Add((int)playerType);
                msg = new Message(CardMessageType.SharePlayerType, tmpList);
                AddMessage(msg);
            }
            else
            {
                RGNetworkPlayerList.instance.SetPlayerType(playerType);
            }
        }
        mStartGameRun = true;
        Debug.Log("start game set!");
    }

    public void TurnOffWaiting()
    {
        mStartGameRun = false;
        gameCanvas.SetActive(true);
        startScreen.SetActive(false); // Start menu isn't necessary now
        waitingScreen.SetActive(false);
    }

    public void RealGameStart()
    {
        Debug.Log("running 2nd start of game");

        // send out the starting message with all player info
        // and start the next phase
        if (isServer)
        {
            Message msg = RGNetworkPlayerList.instance.CreateStartGameMessage();
            AddMessage(msg);
        }

        // if it's a network rejoin we already have our facility
        if (actualPlayer.ActiveFacilities.Count==0 )
        {
            // draw our first 2 pt facility
            Card card = actualPlayer.DrawFacility(false, -1, -1, 2);
            // send message about what facility got drawn
            if (card != null)
            {
                AddMessage(new Message(CardMessageType.SendPlayedFacility, card.UniqueID, card.data.data.cardID));
            }
            else
            {
                Debug.Log("problem in drawing first facility as it's null!");
            }
        }
       
     
        // make sure to show all our cards
        //foreach (GameObject gameObjectCard in actualPlayer.HandCards.Values)
        //{
        //    gameObjectCard.SetActive(true);
        //}

        // set up the opponent name text
        if (RGNetworkPlayerList.instance.playerIDs.Count > 1)
        {
            Debug.Log("player ids greater than zero for realstart");
            if (RGNetworkPlayerList.instance.localPlayerID == 0)
            {
                mOpponentName.text = RGNetworkPlayerList.instance.playerNames[1];
                mOpponentDeckType.text = "" + RGNetworkPlayerList.instance.playerTypes[1];
                opponentType = RGNetworkPlayerList.instance.playerTypes[1];
               
            }
            else
            {
                mOpponentName.text = RGNetworkPlayerList.instance.playerNames[0];
                mOpponentDeckType.text = "" + RGNetworkPlayerList.instance.playerTypes[0];
                opponentType = RGNetworkPlayerList.instance.playerTypes[0];
            }

            // WORK: this assumes the players don't play the same type of deck
            if (opponentType == PlayerType.Energy)
            {
                opponentPlayer.playerType = PlayerType.Energy;
                opponentPlayer.DeckName = "power";
            }
            else
            {
                opponentPlayer.playerType = PlayerType.Water;
                opponentPlayer.DeckName = "water";
            }
            opponentPlayer.InitializeCards();
        }

        // in this game people go in parallel to each other
        // per phase
        myTurn = true;
        gameStarted = true;

        // go on to the next phase
        mGamePhase = GamePhase.DrawAndDiscard;
        TurnOffWaiting();
    }

    // display info about the game's status on the screen
    public void DisplayGameStatusPlayer(string message )
    {
        playerStatusText.text = message;
    }

    public void DisplayGameStatusOpponent(string message)
    {
        opponentStatusText.text = message;
    }

    // Sets the end game screen active and changes the text to match
    // the actual scoring.
    public void ShowEndGameCanvas()
    {
        mGamePhase = GamePhase.End;
        endGameCanvas.SetActive(true);
        endGameText.text = mPlayerName.text + "\r\n-----------------------\r\n"+
            "facility score: " + actualPlayer.GetFacilityScores() +
            "\r\nExtra connection score of: " + actualPlayer.GetConnectionScores() +
            "\r\n\r\n" + mOpponentName.text +"\r\n-----------------------\r\n" + 
            "facility score: " + opponentPlayer.GetFacilityScores() +
            "\r\nExtra connection score of: " + opponentPlayer.GetConnectionScores();
    }

    public bool HasReceivedEndGame()
    {
        return mReceivedEndGame;
    }

    public void SetReceivedEndGame(bool value)
    {
        mReceivedEndGame = value;
    }

    // WORK: there is no menu?????
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
                    playerType = PlayerType.Water;
                    break;
                default:
                    break;
            }

            // display player type on view???
            Debug.Log("player type set to be " + playerType);
        }
        
    }

    // Show the cards and game UI for player.
    public void ShowPlayUI()
    {
        actualPlayer.handDropZone.SetActive(true);
        //actualPlayer.discardDropZone.SetActive(true);
    }

    // Hide the cards and game UI for the player.
    public void HidePlayUI()
    {
        actualPlayer.handDropZone.SetActive(false);
        //actualPlayer.discardDropZone.SetActive(false);
    }

    // Ends the phase.
    public void EndPhase()
    {
        bool allowNextPhase = true;
        actualPlayer.ReturnCardsToHand();

        switch (mGamePhase)
        {
            case GamePhase.DrawAndDiscard:
                {
                    // make sure we have a full hand
                    actualPlayer.DrawCards();
                    // set the discard area to work if necessary
                    //actualPlayer.discardDropZone.SetActive(false);
                    GameUI.AddCommand(UICommandType.HideDiscard);
                    mIsDiscardAllowed = false;

                    // clear any remaining drops since we're ending the phase now
                    actualPlayer.ClearDropState();

                    Debug.Log("ending draw and discard game phase!");

                    // send a message with number of discards of the player
                    Message msg;
                    List<int> tmpList = new List<int>(1);
                    tmpList.Add(mNumberDiscarded);
                    msg = new Message(CardMessageType.ShareDiscardNumber, tmpList);
                    AddMessage(msg);
                }
                break;
            case GamePhase.Defense:
                {
                    SendUpdatesToOpponent(mGamePhase, actualPlayer);
                    // reset the defense var's for the next turn
                    mIsDefenseAllowed = false;
                    mNumberDefense = 0;
                    actualPlayer.ClearAllHighlightedFacilities();
                }
                break;
            case GamePhase.Vulnerability:
                {
                    // reset vulnerability allowance
                    mAllowVulnerabilitiesPlayed = false;
                    // we need to reset vulnerability costs to be used for next turn               
                    SendUpdatesToOpponent(mGamePhase, actualPlayer);
                    actualPlayer.ResetVulnerabilityCost();
                    opponentPlayer.ClearAllHighlightedFacilities();
                }
                break;
            case GamePhase.Mitigate:
                {
                    mAllowMitigationPlayed = false;
                    SendUpdatesToOpponent(mGamePhase, actualPlayer);
                    SendUpdatesToOpponent(mGamePhase, opponentPlayer);
                    actualPlayer.ClearAllHighlightedFacilities();
                }
                break;
            case GamePhase.Attack:
                {
                    SendUpdatesToOpponent(mGamePhase, actualPlayer);
                    Debug.Log("actual player updates were added to message queue for attack");
                    SendUpdatesToOpponent(mGamePhase, opponentPlayer);
                    Debug.Log("opponent updates were added to message queue for attack");
                }
                break;

            case GamePhase.AddConnections:
                {
                    if (actualPlayer.GetNewFacilityConnected())
                    {
                        List<int> playsForMessage = new List<int>(5);
                        actualPlayer.GetNewConnectionsInMessageFormat(ref playsForMessage);
                       
                        if (playsForMessage.Count > 0)
                        {
                            Message msg = new Message(CardMessageType.AddConnections, playsForMessage);
                            AddMessage(msg);
                        }
                        actualPlayer.ClearAllHighlightedFacilities();
                    } else
                    {
                        allowNextPhase = false;
                    }              
                }
                break;
            case GamePhase.End:
                break;
            default:
                break;
        }

        if (myTurn && allowNextPhase)
        {
            Debug.Log("ending the game phase in gamemanager!");
            mEndPhaseButton.SetActive(false);
            AddMessage(new Message(CardMessageType.EndPhase));
            myTurn = false;
        }
    }

    public void SendUpdatesToOpponent(GamePhase phase, CardPlayer player)
    {
        // send a message with defense cards played and where they were played
        Message msg;
        List<int> tmpList = new List<int>(4);
        Debug.Log("inside of sendupdatestoopponent");
        player.GetUpdatesInMessageFormat(ref tmpList, phase);
        Debug.Log("get updates in message format complete");
        msg = new Message(CardMessageType.SendCardUpdates, tmpList);
        AddMessage(msg);
        Debug.Log("message added to queue to be sent. Message size is " + tmpList.Count);

        if (phase==GamePhase.Attack && !player.Equals(opponentPlayer))
        {
            Message attackMessage;
            List<int> tmpList2 = new List<int>(4);
            Debug.Log("sending attack updates");
            player.GetAttackUpdatesInMessageFormat(ref tmpList2);
            Debug.Log("get updates in message format complete");
            attackMessage = new Message(CardMessageType.AttackUpdates, tmpList2);
            AddMessage(attackMessage);
            Debug.Log("message added to queue to be sent. Message size is " + tmpList.Count);
        }
    }

    
    public void AddConnectionsFromOpponent(ref List<FacilityConnectionInfo> updates, int originalFacilityUniqueID)
    {
        if (updates.Count == 0)
        {
            DisplayGameStatusOpponent("Opponent did not connect any facilities during the connection phase.");
        }
        else if (updates.Count == 1)
        {
            DisplayGameStatusOpponent("Opponent connected " + updates.Count + " facility in the connection phase");
        } else
        {
            DisplayGameStatusOpponent("Opponent connected " + updates.Count + " facilities in the connection phase");
        }
        opponentPlayer.AddConnections(ref updates, originalFacilityUniqueID);
    }

    public void AddUpdatesFromOpponent(ref List<Updates> updates, GamePhase phase)
    {
        Debug.Log("inside of addupdatesfromopponent method");
        if (phase == GamePhase.Attack)
        {
        }
        else
        if (updates.Count == 0 && phase != GamePhase.Attack)
        {
            DisplayGameStatusOpponent("Opponent did not play any cards in phase " + phase + ".");
        }
        else if (updates.Count == 1 && phase != GamePhase.Attack)
        {
            Updates update = updates[0];
            ReadInCardData card;
            CardFront front;
            if (CardPlayer.cards.TryGetValue(update.CardID, out card))
            {
                front = card.front;
                if (front != null)
                {
                    DisplayGameStatusOpponent("Opponent played one card in phase " + phase + ". The card was " +
                        front.title + ".");
                }
                else
                {
                    DisplayGameStatusOpponent("Opponent played one unknown card in phase " + phase + ".");
                }
            }
        }
        else
        {
            StringBuilder str = new StringBuilder("");
            str.Append("Opponent played " + updates.Count + " cards during phase " + phase +
                ". The names are: ");

            int count = updates.Count;
            for (int i = 0; i < count; i++)
            {
                Updates update = updates[i];
                ReadInCardData card;
                if (CardPlayer.cards.TryGetValue(update.CardID, out card))
                {
                    CardFront front = card.front;
                    if (front != null)
                    {
                        if (i < count - 1)
                        {

                            str.Append(front.title + ", ");
                        }
                        else
                        {
                            str.Append(front.title + ".");
                        }
                    }
                }

            }
            DisplayGameStatusOpponent(str.ToString());
        }

        if (updates.Count > 0)
        {
            switch (phase)
            {
                case GamePhase.Defense:
                    opponentPlayer.AddUpdates(ref updates, phase, actualPlayer);
                    break;
                case GamePhase.Vulnerability:
                    // This phase is more painful since it's an opponent card on top of a player facility
                    foreach (Updates update in updates)
                    {
                        if (update.WhatToDo == AddOrRem.Add)
                        {
                            // draw opponent card to place on player facility
                            // create card to be displayed
                            Card card = opponentPlayer.DrawCard(false, update.CardID, -1, ref opponentPlayer.DeckIDs, opponentPlayer.playerDropZone, true, ref opponentPlayer.ActiveCards);
                            Debug.Log("phase vuln opponent card with id : " + update.CardID + " should be in active opponent list.");
                            Debug.Log("opponent active list size is : " + opponentPlayer.ActiveCards.Count);
                            GameObject cardGameObject = opponentPlayer.ActiveCards[card.UniqueID];
                            actualPlayer.AddUpdate(update, cardGameObject, actualPlayer.playerDropZone, phase, false);

                            if (card.data.data.cardType == CardType.Instant)
                            {
                                // cache info for whenever we make a decision
                                mCacheInstantCard = card;
                                mCacheInstantCardPlayInfo = update;

                                // pop up the window for resolution and wait for a button to be selected
                                mWaitingForInstantCardResolution = true;
                                PlayHaltButton.SetActive(false);
                                mEndPhaseButton.SetActive(false);
                                
                                foreach (GameObject cardObj in actualPlayer.HandCards.Values)
                                {
                                    Card potentialHaltCard = cardObj.GetComponent<Card>();
                                    if (potentialHaltCard.data.data.cardType == CardType.Halt)
                                    {
                                        PlayHaltButton.SetActive(true);
                                        break;
                                    }
                                }
                                InstantCardAlert.SetActive(true);
                            }
                        }
                        else
                        {
                            bool getRidOfFacility = false;
                            // first get card type for each update
                            foreach (GameObject facility in opponentPlayer.ActiveFacilities.Values)
                            {
                                Card facilityCard = facility.GetComponent<Card>();
                                if (facilityCard.data.data.cardID == update.CardID)
                                {
                                    getRidOfFacility = true;
                                    break;
                                }
                            }
                            if (getRidOfFacility)
                            {
                                Debug.Log("should be getting rid of facility");
                                opponentPlayer.AddUpdate(update, null, actualPlayer.playerDropZone, GamePhase.Attack, true);
                            }
                            else
                            {
                                // this card is owned by the player
                                // NOTE: if facility died then this won't actually do anything except tell
                                // us the facility isn't there in a debug message
                                opponentPlayer.AddUpdate(update, null, actualPlayer.playerDropZone, GamePhase.Attack, false);
                            }
                        }
                    }
                    break;
                case GamePhase.Mitigate:
                    // mitigation is also an opponent card on a player facility
                    // note: it's not enough to just have the card id - need to also 
                    // know which facility it's connected to
                    foreach (Updates update in updates)
                    {
                        // draw opponent card to place on player facility
                        // create card to be displayed
                        Debug.Log("phase mitigate opponent card with id : " + update.CardID + " should be in active opponent list.");
                        Debug.Log("opponent active list size is : " + opponentPlayer.ActiveCards.Count);
                        opponentPlayer.AddUpdate(update, null, actualPlayer.playerDropZone, phase, false);
                    }
                    break;
                case GamePhase.Attack:
                    foreach (Updates update in updates)
                    {
                        bool getRidOfFacility = false;
                        // first get card type for each update
                        foreach (GameObject facility in opponentPlayer.ActiveFacilities.Values)
                        {
                            Card facilityCard = facility.GetComponent<Card>();
                            if (facilityCard.data.data.cardID == update.CardID)
                            {
                                getRidOfFacility = true;
                                break;
                            }
                        }
                        if (getRidOfFacility)
                        {
                            Debug.Log("should be getting rid of facility");
                            opponentPlayer.AddUpdate(update, null, actualPlayer.playerDropZone, phase, true);
                        }
                        else
                        {
                            // this card is owned by the player
                            // NOTE: if facility died then this won't actually do anything except tell
                            // us the facility isn't there in a debug message
                            opponentPlayer.AddUpdate(update, null, actualPlayer.playerDropZone, phase, false);
                        }

                        Debug.Log("phase attack needs to change card with id : " + update.CardID + " should be in active opponent list.");
                        Debug.Log("opponent active list size is : " + opponentPlayer.ActiveCards.Count);
                        Debug.Log("player active list size is : " + actualPlayer.ActiveCards.Count);

                    }
                    break;
                default:
                    break;
            }
        }

        Debug.Log("ending opponent update method");

    }

    public void HandleInstantPhasePlay()
    {
        mWaitingForInstantCardResolution = false;
        mEndPhaseButton.SetActive(true);
        InstantCardAlert.SetActive(false);

        // play card out of sequence
        actualPlayer.HandleInstantAttack(mCacheInstantCard, mCacheInstantCardPlayInfo, opponentPlayer);
        // we fake an attack phase since this will cause attack update messages
        SendUpdatesToOpponent(GamePhase.Attack, actualPlayer);
        
    }

    public void HandlePlayHalt()
    {
        Debug.Log("halt card played!");
        mWaitingForInstantCardResolution = false;
        mEndPhaseButton.SetActive(true);
        InstantCardAlert.SetActive(false);

        // mitigate card immediately
        actualPlayer.MitigateInstantAttack(mCacheInstantCardPlayInfo, opponentPlayer);
        SendUpdatesToOpponent(GamePhase.Mitigate, opponentPlayer);
    }

    public void AddAttackUpdatesFromOpponent(ref List<AttackUpdate> updates)
    {
        opponentPlayer.DisplayAttackUpdates(ref updates);
    }

    public void AddOpponentFacility(int facilityId, int uniqueId)
    {
        opponentPlayer.DrawFacility(false, facilityId, uniqueId, -1);
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
                if (turnTotal >= mMaxTurn)
                {
                        nextPhase = GamePhase.End;
                }
                break;
            case GamePhase.AddStation:
                nextPhase = GamePhase.AddConnections;
                break;
            case GamePhase.AddConnections:
                // end the game if we're out of cards or have
                // no stations left on the board
                if (actualPlayer.DeckIDs.Count == 0 || 
                    (actualPlayer.ActiveFacilities.Count == 0))
                {
                    nextPhase = GamePhase.End;
                } else
                {
                    nextPhase = GamePhase.DrawAndDiscard;
                }      
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
            myTurn = true;
            mGamePhase = GetNextPhase();
            mEndPhaseButton.SetActive(true);
        }

    }

    // Gets which turn it is.
    public int GetTurn()
    {
        return turnTotal;
    }

    public bool CanStationsBeHighlighted()
    {
        bool canBe = false;
        switch (mGamePhase)
        {
            case GamePhase.Start:
                canBe = false;
                break;
            case GamePhase.DrawAndDiscard:
                canBe = false;
                break;
            case GamePhase.Defense:
                canBe = true;
                break;
            case GamePhase.Vulnerability:
                canBe = true;
                break;
            case GamePhase.Mitigate:
                canBe = true;
                break;
            case GamePhase.Attack:
                canBe = false;
                break;
            case GamePhase.AddStation:
                canBe = false;
                break;
            case GamePhase.AddConnections:
                canBe = true;
                break;
            case GamePhase.End:
                canBe = false;
                break;
            default:
                break;
        }

        return canBe;
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
    //Sets dialogue to inactive
    private void SkipTutorial()
    {
        if (!yarnSpinner.activeInHierarchy) { return; }

        if ((skip && mPreviousGamePhase != GamePhase.Start && mGamePhase == GamePhase.DrawAndDiscard)
            || skipClicked)
        {
            skip = true;
            runner.Stop();
            yarnSpinner.SetActive(false);
            background.SetActive(false);
        }
    }

    public void SkipClick()
    {
        skipClicked = true;
        SkipTutorial();
    }

    public void ViewTutorial()
    {
        if (yarnSpinner.activeInHierarchy) { return; }

        runner.Stop();

        yarnSpinner.SetActive(true);
        background.SetActive(true);
        skipClicked = false;
        skip = false;
        Debug.Log(mGamePhase.ToString());
        runner.StartDialogue(mGamePhase.ToString());
    }

    public void ResetForNewGame()
    {
        actualPlayer.ResetForNewGame();
        opponentPlayer.ResetForNewGame();

        // where are we in game phases?
        mGamePhase = GamePhase.Start;
        mPreviousGamePhase = GamePhase.Start;

        // Various turn and game info.
        myTurn = false;
        turnTotal = 0;
        gameStarted = false;
        mNumberDiscarded = 0;
        mNumberDefense = 0;
        mIsDiscardAllowed = false;
        mIsDefenseAllowed = false;
        mAllowVulnerabilitiesPlayed = false;
        mAllowMitigationPlayed = false;
        mReceivedEndGame = false;
        mStartGameRun = false;

        // has everything been set?
        isInit = false;

        // keep track of all game messages
        mMessageQueue.Clear();

        // now start the game again
        startScreen.SetActive(true);
        gameCanvas.SetActive(false);
        endGameCanvas.SetActive(false);

        // set the network player ready to play again
        RGNetworkPlayerList.instance.ResetAllPlayersToNotReady();

        mRestarted = true;
    }
}
