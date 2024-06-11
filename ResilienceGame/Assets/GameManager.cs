using System.Collections;
using Unity.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Mirror;
using System.Diagnostics.Eventing.Reader;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrackBar;

public enum GamePhase
{
    Start,
    DrawAndDiscard,
    Defense,
    Vulnerability,
    Mitigate,
    Attack,
    AddStation,
    End
};

public class GameManager : MonoBehaviour, IDragHandler, IRGObservable
{
    public CardReader energyDeckReader;
    public CardReader waterDeckReader;
    public bool mCreateEnergyAtlas = false;
    public bool mCreateWaterAtlas = false;

    public List<Card> energyCards;
    public List<Card> waterCards;

    // where are we in game phases?
    GamePhase mGamePhase = GamePhase.Start;
    GamePhase mPreviousGamePhase = GamePhase.Start;
    bool myTurn = false;
    bool endGame = false;
    int turnTotal = 0;
    RGGameExampleUI gameView;

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

        CardReader reader = waterDeckReader.GetComponent<CardReader>();
        if (reader != null)
        {
            waterCards = reader.CSVRead(mCreateWaterAtlas);
            waterPlayer.cards = waterCards;
            waterPlayer.playerType = PlayerType.WaterAndWasteWater;
        }
        else
        {
            Debug.Log("reader is null");
        }
        reader = energyDeckReader.GetComponent<CardReader>();
        if (reader != null)
        {
            energyCards = reader.CSVRead(mCreateEnergyAtlas);
            energyPlayer.cards = energyCards;
            energyPlayer.playerType = PlayerType.Energy;

        }
        else
        {
            Debug.Log("reader is null");
        }

      

        Debug.Log("deck should be read");
    }


    public void setupActors()
    {

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

        actualPlayer.InitializeCards();
        actualPlayer.cardDropZone.SetActive(true);
        actualPlayer.handDropZone.SetActive(true);

    }

    // Update is called once per frame
    void Update()
    {
        if (isInit)
        {
            if (gameStarted)
            {
                HandlePhases(mGamePhase);
                //if (isServer)
                //{
                //    if (!endGame && turnTotal >= 4)
                //    {
                //        EndGame(0, true);
                //    }
                //}
                
            }
            else
            {
                //if (isServer)
                //{
                //    StartTurn();
                //}
            }
            NotifyObservers();

        } else
        {
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
                    isInit = true;
                }
            }
        }

    }

    public void HandlePhases(GamePhase phase)
    {
        mGamePhase = phase;
        if (!mGamePhase.Equals(mPreviousGamePhase))
        {
            mPhaseText.text = mGamePhase.ToString();
            mPreviousGamePhase = phase;
        }

        switch (phase)
        {
            case GamePhase.Start:
                // handled with specialty code outside of this
                break;
            case GamePhase.DrawAndDiscard:
                // draw cards if necessary
                actualPlayer.DrawCards();
                // set the discard area to work
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
                break;
            case GamePhase.End:
                break;
            default:
                break;
        }
    }

    public void StartGame()
    {
        setupActors();
        GameObject obj = GameObject.Find("ExampleGameUI");
        gameView = obj.GetComponent<RGGameExampleUI>();
        gameView.SetStartTeamInfo(actualPlayer);
        gameCanvas.SetActive(true);
        startScreen.SetActive(false); // Start menu isn't necessary now
        turnTotal = 0;
        mTurnText.text = "Turn: " + GetTurn();
        mPhaseText.text = "Phase: " + mGamePhase.ToString();
       
        activePlayerText.text = playerType + " Player";
        Debug.Log("set active player to be: " + playerType);
        activePlayerColor = new Color(0.0f, 0.4209991f, 1.0f, 1.0f);
        activePlayerText.color = activePlayerColor;
        //yarnSpinner.SetActive(true);
       
        Debug.Log("player active");

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

        mGamePhase = GamePhase.DrawAndDiscard;
        // in this game people go in parallel to each other
        // per phase
        myTurn = true;
        gameStarted = true;
    }

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

    public void Discard()
    {

    }

    public void DisplayOtherPlayerTypes( string playerName, PlayerType type)
    {
        // need to do something with a view here...
        Debug.Log("The player " + playerName + " joined with type " + type);
    }

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
        endGame = true;
    }

    public void EndPhase()
    {
        if (myTurn)
        {
            gameView.HidePlayUI();
            AddMessage(new Message(CardMessageType.EndPhase));
            myTurn = false;
        }
    }

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

    public void StartNextPhase()
    {
        if (!myTurn)
        {
            foreach (GameObject card in actualPlayer.GetComponent<CardPlayer>().HandList)
            {
                card.SetActive(true);
            }
            foreach (GameObject card in actualPlayer.GetComponent<CardPlayer>().ActiveCardList)
            {
                card.SetActive(true);
            }

            myTurn = true;
            mGamePhase = GetNextPhase();
            gameView.ShowPlayUI();
            Debug.Log("play ui shown");
        }

    }

    public int GetTurn()
    {
        return turnTotal;
    }

    public void ShowMyCardsToEverybody()
    {
        //Message msg;
        //List<int> tempCardIdList = new List<int>(5);
        //// malicious player
        //if (isServer)
        //{
        //   for (int i=0; i<maliciousActor.HandList.Count; i++ )
        //    {
        //        Card cardObj = maliciousActor.HandList[i].GetComponent<Card>();
        //        tempCardIdList.Add(cardObj.data.cardID);
        //    }
        //} else
        //{
        //    for (int i = 0; i < resiliencePlayer.HandList.Count; i++)
        //    {
        //        Card cardObj = resiliencePlayer.HandList[i].GetComponent<Card>();
        //        tempCardIdList.Add(cardObj.data.cardID);
        //    }           
        //}

        //msg = new Message(CardMessageType.ShowCards, tempCardIdList);
        //Debug.Log("showing cards: " + msg.arguments.Count);
        //AddMessage(msg);
    }

    public void ShowOthersCards(List<int> cardsToShow)
    {
        //if (allCardsPlayer == null)
        //{
        //    allCardsPlayer = gameObject.AddComponent<CardPlayer>();
        //    allCardsPlayer.cardReader = GameObject.FindObjectOfType<CardReader>();
        //    allCardsPlayer.manager = this;
        //    allCardsPlayer.cardPrefab = resiliencePlayer.cardPrefab;
        //    allCardsPlayer.Deck = new List<int>(5);
        //    allCardsPlayer.CardCountList = new List<int>(5);
        //    allCardsPlayer.HandList = new List<GameObject>(5);
        //    //allCardsPlayer.Facilities = new List<GameObject>(5);
        //    for (int i = 0; i < allCardsPlayer.cardReader.CardIDs.Length; i++)
        //    {
        //            allCardsPlayer.Deck.Add(i);
        //            allCardsPlayer.CardCountList.Add(allCardsPlayer.cardReader.CardCount[i]);
        //    }
        //    Debug.Log("cardread done!");
        //    allCardsPlayer.handDropZone = gameView.showCardHolder;
        //    //allCardsPlayer.InitializeFacilities();
        //    Debug.Log("drawing cards");
        //    for (int i = 0; i < cardsToShow.Count; i++)
        //    {
        //        allCardsPlayer.DrawCard(false, cardsToShow[i]);
        //    }
        //    Debug.Log("cards drawn");
        //} else
        //{
        //    for (int i = 0; i < allCardsPlayer.HandList.Count; i++)
        //    {
        //        allCardsPlayer.HandList[i].SetActive(false);
                
        //    }
        //    allCardsPlayer.HandList.Clear();
        //    for (int i = 0; i < cardsToShow.Count; i++)
        //    {
        //        allCardsPlayer.DrawCard(false, cardsToShow[i]);
        //    }
        //}

        Debug.Log("setting show to true");
        gameView.showCardHolder.SetActive(true);
        Debug.Log("stuff should show!");
    }

    public void AddMessage(Message msg)
    {
        mMessageQueue.Enqueue(msg);
    }

    public void RegisterObserver(IRGObserver o)
    {
        if (!mObservers.Exists(x => x == o))
        {
            mObservers.Add(o);
        }
    }

   
    public void RemoveObserver(IRGObserver o)
    { 
        if (mObservers.Exists(x => x == o) )
        {
            mObservers.Remove(o);
        }
    }

  
    public void NotifyObservers()
    {
        if (!mMessageQueue.IsEmpty())
        {
            while (!mMessageQueue.IsEmpty()) {
                Message m = mMessageQueue.Dequeue();
                foreach (IRGObserver o in mObservers)
                {
                    o.UpdateObserver(m);
                } 
            }
        }
    }
}
