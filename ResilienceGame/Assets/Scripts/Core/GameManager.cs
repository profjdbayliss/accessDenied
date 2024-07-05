using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Mirror;
using System.Linq;
using Yarn.Unity;
using System.Xml;

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

    static bool hasStartedAlready = false;


    public void Awake()
    {
        instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("start run on GameManager");
        if (!hasStartedAlready)
        {
            startScreen.SetActive(true);

            // read water deck
            CardReader reader = waterDeckReader.GetComponent<CardReader>();
            if (reader != null)
            {
                waterCards = reader.CSVRead(mCreateWaterAtlas);
                CardPlayer.AddCards(waterCards);
                waterPlayer.playerType = PlayerType.Water;
                waterPlayer.DeckName = "water";
                Debug.Log("number of cards in all cards is: " + CardPlayer.cards.Count);
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
                CardPlayer.AddCards(energyCards);
                energyPlayer.playerType = PlayerType.Energy;
                energyPlayer.DeckName = "power";
                Debug.Log("number of cards in all cards is: " + CardPlayer.cards.Count);

            }
            else
            {
                Debug.Log("Energy deck reader is null.");
            }

            // Set dialogue runner for tutorial
            runner = yarnSpinner.GetComponent<DialogueRunner>();
            background = yarnSpinner.transform.GetChild(0).GetChild(0).gameObject;
            Debug.Log(background);
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
            actualPlayer = energyPlayer;
            actualPlayer.playerType = PlayerType.Energy;
            actualPlayer.DeckName = "power";
        }
        else if (playerType==PlayerType.Water)
        {
            actualPlayer = waterPlayer;
            actualPlayer.playerType = PlayerType.Water;
            actualPlayer.DeckName = "water";
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
                        if (isDiscardAllowed)
                        {
                            numberDiscarded += actualPlayer.HandleDiscard(actualPlayer.HandCards, actualPlayer.handDropZone, -1, false);
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

                if (phaseJustChanged
                    && !actualPlayer.CheckForCardsOfType(CardType.Defense, actualPlayer.HandCards))
                {
                    // if player has no defense cards to play
                    // then auto phase forward
                    DisplayGameStatus(mPlayerName.text + " has no defense cards. Autocompleting the defense phase.");
                    EndPhase();
                } else 
                {
                    actualPlayer.HandlePlayCard(GamePhase.Defense, opponentPlayer);
                }
                break;
            case GamePhase.Vulnerability:
                if (phaseJustChanged && !skip) 
                {
                    runner.StartDialogue("Vulnerability");
                    background.SetActive(true);
                }

                if (phaseJustChanged
                    && !actualPlayer.CheckForCardsOfType(CardType.Vulnerability, actualPlayer.HandCards))
                {
                    // if player has no defense cards to play
                    // then auto phase forward
                    DisplayGameStatus(mPlayerName.text + " has no vulnerability cards. Autocompleting the vulnerability phase.");
                    EndPhase();
                }
                else
                {
                    actualPlayer.HandlePlayCard(GamePhase.Vulnerability, opponentPlayer);
                }
                break;
            case GamePhase.Mitigate:
                if (phaseJustChanged && !skip) 
                { 
                    runner.StartDialogue("Mitigate");
                    background.SetActive(true);
                }

                if (phaseJustChanged
                    && !actualPlayer.CheckForCardsOfType(CardType.Mitigation, actualPlayer.HandCards))
                {
                    // if player has no defense cards to play
                    // then auto phase forward
                    DisplayGameStatus(mPlayerName.text + " has no Mitigation cards. Autocompleting the mitigation phase.");
                    EndPhase();
                }
                else
                {
                    actualPlayer.HandlePlayCard(GamePhase.Mitigate, opponentPlayer);
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
                    actualPlayer.HandleAttackPhase(opponentPlayer);
                    opponentPlayer.DiscardAllInactiveCards();
                }  
                EndPhase();
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
                        AddMessage(new Message(CardMessageType.SendPlayedFacility, card.UniqueID, card.data.cardID));
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

       
        // draw our first 2 pt facility
       Card card = actualPlayer.DrawFacility(false, 2);
        // send message about what facility got drawn
        if (card != null)
        {     
            AddMessage(new Message(CardMessageType.SendPlayedFacility, card.UniqueID, card.data.cardID));
        } else
        {
            Debug.Log("problem in drawing first facility as it's null!");
        }
     
        // make sure to show all our cards
        foreach (GameObject gameObjectCard in actualPlayer.HandCards.Values)
        {
            gameObjectCard.SetActive(true);
        }

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
                opponentPlayer.DeckName = "power";
            }
            else
            {
                opponentPlayer = waterPlayer;
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

    }

    // display info about the game's status on the screen
    public void DisplayGameStatus(string message)
    {
        StatusText.text = message;
    }
   

    // WORK: rewrite for this card game
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
                    SendUpdatesToOpponent(mGamePhase, actualPlayer);
                    actualPlayer.ResetDefenseNumber();
                }
                break;
            case GamePhase.Vulnerability:
                {
                    // we need to reset vulnerability costs to be used for next turn               
                    SendUpdatesToOpponent(mGamePhase, actualPlayer);
                    actualPlayer.ResetVulnerabilityCost();
                }
                break;
            case GamePhase.Mitigate:
                {
                    SendUpdatesToOpponent(mGamePhase, actualPlayer);
                    SendUpdatesToOpponent(mGamePhase, opponentPlayer);
                }
                break;
            case GamePhase.Attack:
                {
                    SendUpdatesToOpponent(mGamePhase, actualPlayer);
                    SendUpdatesToOpponent(mGamePhase, opponentPlayer);
                }
                break;
            default:
                break;
        }

        if (myTurn)
        {
            Debug.Log("ending the game phase in gamemanager!");
            //HidePlayUI();
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
        player.GetUpdatesInMessageFormat(ref tmpList, phase);
        msg = new Message(CardMessageType.SendCardUpdates, tmpList);
        AddMessage(msg);
    }

    public void AddUpdatesFromOpponent(ref List<Updates> updates, GamePhase phase)
    {
        if (updates.Count == 0)
        {
            DisplayGameStatus("Opponent did not play any cards during their turn in phase " + phase);
        }
        else
        {
            DisplayGameStatus("Opponent played " + updates.Count + " cards during their turn in phase " + phase);
        }

        switch (phase)
        {
            case GamePhase.Defense:
                opponentPlayer.AddUpdates(ref updates, phase, actualPlayer);
                break;
            case GamePhase.Vulnerability:
                // This phase is more painful since it's an opponent card on top of a player facility
                foreach (Updates update in updates)
                {
                    // draw opponent card to place on player facility
                    // create card to be displayed
                    Card card = opponentPlayer.DrawCard(false, update.CardID, -1, ref opponentPlayer.DeckIDs, opponentPlayer.playerDropZone, true, ref opponentPlayer.ActiveCards);
                    Debug.Log("phase vuln opponent card with id : " + update.CardID + " should be in active opponent list.");
                    Debug.Log("opponent active list size is : " + opponentPlayer.ActiveCards.Count);
                    GameObject cardGameObject = opponentPlayer.ActiveCards[card.UniqueID];
                    actualPlayer.AddUpdate(update, cardGameObject, actualPlayer.playerDropZone, phase, false);
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
                    foreach(GameObject facility in opponentPlayer.ActiveFacilities.Values)
                    {
                        Card facilityCard = facility.GetComponent<Card>();
                        if (facilityCard.data.cardID == update.CardID)
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

    public void AddOpponentFacility(int facilityId, int uniqueId)
    {
        opponentPlayer.DrawCard(false, facilityId, uniqueId, ref opponentPlayer.FacilityIDs, opponentPlayedZone,
            false, ref opponentPlayer.ActiveFacilities);
    }

    //public void DiscardOpponentActiveCard(int uniqueFacilityID, CardIDInfo cardInfo, bool sendAsMessage)
    //{
    //    if (sendAsMessage)
    //    {
    //        opponentPlayer.DiscardSingleActiveCard(uniqueFacilityID, cardInfo, true);

    //        // send a message with defense cards played and where they were played
    //        Message msg;
    //        List<int> tmpList = new List<int>(4);
    //        opponentPlayer.GetUpdatesInMessageFormat(ref tmpList, mGamePhase);
    //        msg = new Message(CardMessageType.SendCardUpdates, tmpList);
    //        AddMessage(msg);
    //    }
    //    else
    //    {
    //        opponentPlayer.DiscardSingleActiveCard(uniqueFacilityID, cardInfo, false);
    //    }

    //}

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
            //foreach (GameObject card in actualPlayer.HandCards.Values)
            //{
            //    card.SetActive(true);
            //    Card trueCard = card.GetComponent<Card>();
            //    Debug.Log("setting hand card visible: " + trueCard.front.title + " with id " + trueCard.UniqueID);
            //}
            //foreach (GameObject card in actualPlayer.ActiveCards.Values)
            //{
            //    card.SetActive(true);
            //    Card trueCard = card.GetComponent<Card>();
            //    Debug.Log("setting active card visible: " + trueCard.front.title + " with id " + trueCard.UniqueID);
            //}

            myTurn = true;
            mGamePhase = GetNextPhase();
            //ShowPlayUI();
            mEndPhaseButton.SetActive(true);
            Debug.Log("play ui shown");
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
}
