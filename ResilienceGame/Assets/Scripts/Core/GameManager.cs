using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Mirror;
using System.Linq;
using Yarn.Unity;

public class GameManager : MonoBehaviour, IRGObservable
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
    PlayerType opponentType = PlayerType.Water;
    
    public GameObject playerDeckList;
    TMPro.TMP_Dropdown playerDeckChoice;
    public bool gameStarted = false;

    // var's for game rules
    public readonly int MAX_DISCARDS = 25;
    public int numberDiscarded = 0;
    public bool isDiscardAllowed = false;

    // has everything been set?
    bool isInit = false;

    // keep track of all game messages
    MessageQueue mMessageQueue = new MessageQueue();

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
    public GameObject tiles;

    // game status text
    public TextMeshProUGUI StatusText;
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
            waterPlayer.playerType = PlayerType.Water;
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

        // Set dialogue runner for tutorial
        runner = yarnSpinner.GetComponent<DialogueRunner>();
        background = yarnSpinner.transform.GetChild(0).GetChild(0).gameObject;
        Debug.Log(background);

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
        else if (playerType==PlayerType.Water)
        {
            actualPlayer = waterPlayer;
            actualPlayer.cards = waterCards;
            actualPlayer.playerType = PlayerType.Water;
        }

        // Initialize the deck info and set various
        // player zones active
        actualPlayer.InitializeCards();
        actualPlayer.discardDropZone.SetActive(true);
        actualPlayer.handDropZone.SetActive(true);
        actualPlayer.playerDropZone.SetActive(true);
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
            SkipTutorial();
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
                    //Starts in editor
                    //runner.StartDialogue("DrawAndDiscard");

                    isDiscardAllowed = true;
                    // draw cards if necessary
                    actualPlayer.DrawCards();
                    // set the discard area to work if necessary
                    actualPlayer.discardDropZone.SetActive(true);
                    numberDiscarded = 0;
                } else
                {
                    // draw cards if necessary
                    actualPlayer.DrawCards();

                    // check for discard and if there's a discard draw again
                    if (numberDiscarded == MAX_DISCARDS)
                    {
                        EndPhase();
                    }
                    else
                    {
                        numberDiscarded += actualPlayer.HandleDiscards();
                    }
                }
                break;
            case GamePhase.Defense:
                if (phaseJustChanged && !skip) 
                { 
                    runner.StartDialogue("Defense"); 
                    background.SetActive(true);
                }

                if (phaseJustChanged
                    && !actualPlayer.CheckForCardsOfType(CardType.Defense, actualPlayer.HandList))
                {
                    // if player has no defense cards to play
                    // then auto phase forward
                    DisplayGameStatus(mPlayerName.text + " has no defense cards. Autocompleting the defense phase.");
                    EndPhase();
                } else 
                {
                    actualPlayer.HandlePlayCard(GamePhase.Defense);
                }
                break;
            case GamePhase.Vulnerability:
                if (phaseJustChanged && !skip) 
                {
                    runner.StartDialogue("Vulnerability");
                    background.SetActive(true);
                }

                if (phaseJustChanged
                    && !actualPlayer.CheckForCardsOfType(CardType.Vulnerability, actualPlayer.HandList))
                {
                    // if player has no defense cards to play
                    // then auto phase forward
                    DisplayGameStatus(mPlayerName.text + " has no vulnerability cards. Autocompleting the vulnerability phase.");
                    EndPhase();
                }
                else
                {
                    actualPlayer.HandlePlayCard(GamePhase.Vulnerability);
                }
                break;
            case GamePhase.Mitigate:
                if (phaseJustChanged && !skip) 
                { 
                    runner.StartDialogue("Mitigate");
                    background.SetActive(true);
                }

                if (phaseJustChanged
                    && !actualPlayer.CheckForCardsOfType(CardType.Mitigation, actualPlayer.HandList))
                {
                    // if player has no defense cards to play
                    // then auto phase forward
                    DisplayGameStatus(mPlayerName.text + " has no Mitigation cards. Autocompleting the mitigation phase.");
                    EndPhase();
                }
                else
                {
                    actualPlayer.HandlePlayCard(GamePhase.Mitigate);
                }
                break;
            case GamePhase.Attack:
                if (phaseJustChanged && !skip) 
                { 
                    runner.StartDialogue("Attack");
                    background.SetActive(true);
                }
                break;
            case GamePhase.AddStation:
                if (phaseJustChanged && !skip)
                {
                    runner.StartDialogue("AddStation");
                    background.SetActive(true);
                    skip = true;
                }

                if (phaseJustChanged)
                {

                    // we only need one cycle for this particular
                    // phase as it's automated.
                    Card card = actualPlayer.DrawFacility(true, 0);
                    // send message about what facility got drawn                 
                    if (card != null)
                    {
                        List<int> facilityList = new List<int>(2);
                        facilityList.Add(card.UniqueID);
                        facilityList.Add(card.data.cardID);
                        Message facilityMessage = new Message(CardMessageType.SendPlayedFacility, facilityList);
                        AddMessage(facilityMessage);
                    }
                    EndPhase();
                    // network will end this phase when messages are sent
                }
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
        mPlayerName.text = RGNetworkPlayerList.instance.localPlayerName;
        mPlayerDeckType.text = "" + playerType;
       
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
       Card card = actualPlayer.DrawFacility(false, 2);
        // send message about what facility got drawn
        List<int> facilityList = new List<int>(2);
        if (card != null)
        {
            facilityList.Add(card.UniqueID);
            facilityList.Add(card.data.cardID);
            Message facilityMessage = new Message(CardMessageType.SendPlayedFacility, facilityList);
            AddMessage(facilityMessage);
        } else
        {
            Debug.Log("problem in drawing first facility as it's null!");
        }
     
        // make sure to show all our cards
        foreach (GameObject gameObjectCard in actualPlayer.HandList)
        {
            gameObjectCard.SetActive(true);
        }
        // don't think this does anything right now
        //foreach (GameObject card in actualPlayer.ActiveCardList)
        //{
        //    card.SetActive(true);
        //}

        // set up the opponent name text
        if (RGNetworkPlayerList.instance.playerIDs.Count > 0)
        {
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
            if (opponentType == PlayerType.Energy)
            {
                opponentPlayer = energyPlayer;
            }
            else
            {
                opponentPlayer = waterPlayer;
            }
            opponentPlayer.InitializeCards();
        }

        // go on to the next phase
        mGamePhase = GamePhase.DrawAndDiscard;

    }

    public void DisplayGameStatus(string message)
    {
        StatusText.text = message;
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
                    playerType = PlayerType.Water;
                    break;
                default:
                    break;
            }

            // display player type on view???
            Debug.Log("player type set to be " + playerType);
        }
        
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
        actualPlayer.discardDropZone.SetActive(true);
    }

    // Hide the cards and game UI for the player.
    public void HidePlayUI()
    {
        actualPlayer.handDropZone.SetActive(false);
        actualPlayer.discardDropZone.SetActive(false);
    }

    // Ends the phase.
    public void EndPhase()
    {
        switch (mGamePhase)
        {
            case GamePhase.DrawAndDiscard:
                {
                    // make sure we have a full hand
                    actualPlayer.DrawCards();
                    // set the discard area to work if necessary
                    actualPlayer.discardDropZone.SetActive(false);
                    isDiscardAllowed = false;

                    // clear any remaining drops since we're ending the phase now
                    actualPlayer.ClearDropState();

                    Debug.Log("ending draw and discard game phase!");

                    // send a message with number of discards of the player
                    Message msg;
                    List<int> tmpList = new List<int>(1);
                    tmpList.Add(numberDiscarded);
                    msg = new Message(CardMessageType.ShareDiscardNumber, tmpList);
                    AddMessage(msg);
                }
                break;
            case GamePhase.Defense:
                {
                    SendUpdatesToOpponent(mGamePhase);
                }
                break;
            case GamePhase.Vulnerability:
                {
                    SendUpdatesToOpponent(mGamePhase);
                }
                break;
            case GamePhase.Mitigate:
                {
                    SendUpdatesToOpponent(mGamePhase);
                }
                break;
            default:
                break;
        }

        if (myTurn)
        {
            Debug.Log("ending the game phase in gamemanager!");
            HidePlayUI();
            mEndPhaseButton.SetActive(false);
            AddMessage(new Message(CardMessageType.EndPhase));
            myTurn = false;
        }
    }

    public void SendUpdatesToOpponent(GamePhase phase)
    {
        // send a message with defense cards played and where they were played
        Message msg;
        List<int> tmpList = new List<int>(4);
        actualPlayer.GetUpdatesInMessageFormat(ref tmpList, phase);
        msg = new Message(CardMessageType.SendCardUpdates, tmpList);
        AddMessage(msg);
    }

    public bool CheckOpponentHighlightedStations()
    {
        return opponentPlayer.CheckHighlightedStations();
    }

    public GameObject GetOpponentHighlightedStation()
    {
        GameObject returnValue = null;

        if (CheckOpponentHighlightedStations())
        {
            returnValue = opponentPlayer.GetHighlightedStation();
        }

        return returnValue;
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
            mEndPhaseButton.SetActive(true);
            Debug.Log("play ui shown");
        }

    }

    public void AddOpponentUpdates(ref List<Updates> updates, GamePhase phase)
    {
        if (updates.Count == 0)
        {
            DisplayGameStatus("Opponent did not play any cards during their turn in phase " + phase);
        } else
        {
            DisplayGameStatus("Opponent played " + updates.Count + " cards during their turn in phase " + phase);
        }
        
        switch (phase)
        {
            case GamePhase.Defense:
                opponentPlayer.AddUpdates(ref updates, phase);
                break;
            case GamePhase.Vulnerability:
                // This phase is more painful since it's an opponent card on top of a player facility
                foreach(Updates update in updates)
                {
                    // draw opponent card to place on player facility
                    // create card to be displayed
                    Card card = opponentPlayer.DrawCard(false, update.CardID, -1, ref opponentPlayer.DeckIDs, opponentPlayer.playerDropZone, true, ref opponentPlayer.ActiveCardList, ref opponentPlayer.activeCardIDs);
                    Debug.Log("opponent card with id : " + update.CardID + " should be in active opponent list.");
                    Debug.Log("opponent active list size is : " + opponentPlayer.ActiveCardList.Count);
                    GameObject cardGameObject = opponentPlayer.ActiveCardList[opponentPlayer.ActiveCardList.Count - 1];
                    actualPlayer.AddUpdate(update, cardGameObject, actualPlayer.playerDropZone, phase);
                }
                break;
            case GamePhase.Mitigate:
                actualPlayer.AddUpdates(ref updates, phase);
                break;
            default:
                break;
        }
        
    }

    public void AddOpponentFacility(int facilityId, int uniqueId)
    {
       
        opponentPlayer.DrawCard(false, facilityId, uniqueId, ref opponentPlayer.FacilityIDs, opponentPlayedZone, 
            false, ref opponentPlayer.ActiveFacilities, ref opponentPlayer.ActiveFacilityIDs);
        foreach (GameObject card in opponentPlayer.ActiveFacilities)
        {
            card.SetActive(true);
        }
    }

    public GameObject GetOpponentActiveCardObject(int cardId)
    {
        GameObject cardObject = null;
        foreach(GameObject cardGameObject in opponentPlayer.ActiveCardList)
        {
            Card card = cardGameObject.GetComponent<Card>();
            Debug.Log("get opponent card list card under consideration with id : " + card.data.cardID + " with searched for card being " + cardId);
            if (card.data.cardID == cardId)
            {
                cardObject = cardGameObject;
                break;
            }
        }

        return cardObject;
    }

    public void DiscardOpponentActiveCard(int uniqueFacilityID, int cardID, bool sendAsMessage)
    {
        if (sendAsMessage)
        {
            opponentPlayer.DiscardSingleActiveCard(uniqueFacilityID, cardID, true);

            // send a message with defense cards played and where they were played
            Message msg;
            List<int> tmpList = new List<int>(4);
            opponentPlayer.GetUpdatesInMessageFormat(ref tmpList, mGamePhase);
            msg = new Message(CardMessageType.SendCardUpdates, tmpList);
            AddMessage(msg);
        }
        else
        {
            opponentPlayer.DiscardSingleActiveCard(uniqueFacilityID, cardID, false);
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
}
