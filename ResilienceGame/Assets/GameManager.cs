using System.Collections;
using Unity.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Unity.VectorGraphics;
using UnityEngine.SceneManagement;
using Mirror;
using System;
using System.Linq;
using UnityEditor;
using static System.Net.Mime.MediaTypeNames;

public class GameManager : MonoBehaviour, IDragHandler, IRGObservable
{
    // is it my turn?
    bool myTurn = false;
    bool endGame = false;
    int turnTotal = 0;
    RGGameExampleUI gameView;

    // set up the proper player cards and type
    Card.Type playerType = Card.Type.Energy;
    public GameObject playerDeckList;
    Dropdown playerDeckChoice;
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

    // Establish necessary fields
    public GameObject[] allPlayers;
    public GameObject MalActorObject;
    public CardPlayer resiliencePlayer;
    public CardPlayer maliciousActor;
    CardPlayer allCardsPlayer;
    public bool playerActive = false;
    public GameObject playerMenu;
    public GameObject maliciousActorMenu;

    

    public GameObject gameCanvas;
    public GameObject startScreen;

    public float turnCount;

    public TMP_Text fundText;
    public TextMeshProUGUI activePlayerText;

    public GameObject yarnSpinner;

    public Color activePlayerColor;

    public GameObject continueButton;

    public GameObject maliciousPlayerEndMenu;
    public GameObject resilientPlayerEndMenu;

    public GameObject endGameCanvas;
    public TMP_Text endGameText;

    public int activePlayerNumber;

    public Camera cam;

    public GameObject map;

    public GameObject tiles;

    //public FacilityEvents facilityEvents;
    //public bool criticalEnabled;

    public TextMeshProUGUI titlee;

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
    }


    public void setupActors()
    {

        if (isServer)
        {
            // this is the malicious player
            CardPlayer baseRes = GameObject.FindObjectOfType<CardPlayer>();
            baseRes.gameObject.SetActive(false);
            CardPlayer baseMal = GameObject.FindObjectOfType<CardPlayer>();
            baseMal.InitializeCards();
            maliciousActor = this.gameObject.AddComponent<CardPlayer>();

            this.gameObject.transform.localScale = new Vector3(1, 1, 1);
            baseMal.handDropZone.transform.localScale = new Vector3(1, 1, 1);
            Debug.Log("RGNETWORKPLAYER SIZE:" + this.gameObject.transform.localScale);

            maliciousActor.funds = baseMal.funds;
            maliciousActor.Deck = baseMal.Deck; 
            maliciousActor.CardCountList = baseMal.CardCountList;
            maliciousActor.cardReader = baseMal.cardReader;
            maliciousActor.cardDropZone = baseMal.cardDropZone;
            baseMal.cardDropZone.transform.parent = maliciousActor.transform;
            maliciousActor.handDropZone = baseMal.handDropZone;
            baseMal.handDropZone.transform.parent = maliciousActor.transform;
            maliciousActor.cardPrefab = baseMal.cardPrefab;
            maliciousActor.HandList = baseMal.HandList;
            maliciousActor.ActiveCardList = baseMal.ActiveCardList;
            maliciousActor.activeCardIDs = baseMal.activeCardIDs;
            maliciousActor.manager = baseMal.manager;
            maliciousActor.facilitiesActedUpon = baseMal.facilitiesActedUpon;
            maliciousActor.targetIDList = baseMal.targetIDList;
            baseMal.gameObject.SetActive(false);
            GameObject centralMap = GameObject.Find("Central Map");
            this.gameObject.transform.SetParent(centralMap.transform);
            GameObject obj = GameObject.Find("RGTitle");
            obj.GetComponent<TextMeshProUGUI>().text = "M " + maliciousActor.Deck.Count;
            maliciousActor.cardDropZone.SetActive(true);
            maliciousActor.handDropZone.SetActive(true);

            //forcing the networkplayer to be 1 by 1 by 1 to make future calculations easier
            this.gameObject.transform.localScale = new Vector3(1, 1, 1);
            maliciousActor.handDropZone.transform.localScale = new Vector3(1, 1, 1);
            baseMal.handDropZone.transform.localScale = new Vector3(1, 1, 1);
            Debug.Log("RGNETWORKPLAYER SIZE:" + this.gameObject.transform.localScale);
            Debug.Log(this.gameObject.name);
            Debug.Log(maliciousActor.Deck.Count);
        } else
        {
            // regular blue type player
            CardPlayer baseMal = GameObject.FindObjectOfType<CardPlayer>();
            baseMal.gameObject.SetActive(false);
            CardPlayer baseRes = GameObject.FindObjectOfType<CardPlayer>();
            resiliencePlayer = this.gameObject.AddComponent<CardPlayer>();
            baseRes.InitializeCards();
            resiliencePlayer.funds = baseRes.funds;
            resiliencePlayer.Deck = baseRes.Deck;
            // WORK - needs real type!
            resiliencePlayer.playerType = 0;
            resiliencePlayer.Deck = baseRes.Deck;
            resiliencePlayer.CardCountList = baseRes.CardCountList;
            resiliencePlayer.cardReader = baseRes.cardReader;
            resiliencePlayer.cardDropZone = baseRes.cardDropZone;
            baseRes.cardDropZone.transform.parent = resiliencePlayer.transform;
            resiliencePlayer.handDropZone = baseRes.handDropZone;
            baseRes.handDropZone.transform.parent = resiliencePlayer.transform;
            resiliencePlayer.cardPrefab = baseRes.cardPrefab;
            resiliencePlayer.HandList = baseRes.HandList;
            resiliencePlayer.ActiveCardList = baseRes.ActiveCardList;
            resiliencePlayer.activeCardIDs = baseRes.activeCardIDs;
            resiliencePlayer.manager = baseRes.manager;
            resiliencePlayer.facilitiesActedUpon = baseRes.facilitiesActedUpon;
            resiliencePlayer.targetIDList = baseRes.targetIDList;
            baseRes.gameObject.SetActive(false);
            GameObject centralMap = GameObject.Find("Central Map");
            this.gameObject.transform.SetParent(centralMap.transform);
            GameObject obj = GameObject.Find("RGTitle");
            obj.GetComponent<TextMeshProUGUI>().text = "R " + resiliencePlayer.Deck.Count;
            Debug.Log(resiliencePlayer.Deck.Count);
            resiliencePlayer.cardDropZone.SetActive(true);
            resiliencePlayer.handDropZone.SetActive(true);
        }
        
        
}

    // Update is called once per frame
    void Update()
    {
        if (isInit)
        {
            if (gameStarted)
            {
                if (isServer)
                {
                    if (!endGame && turnTotal >= 4)
                    {
                        EndGame(0, true);
                    }
                }
                
            }
            else
            {
                if (isServer)
                {
                    StartTurn();
                }
            }
            NotifyObservers();

        } else
        {
            mRGNetworkPlayerList = RGNetworkPlayerList.instance;
            if (mRGNetworkPlayerList == null)
            {
                Debug.Log("player list is a null var!");
            }
            else
            {
                RegisterObserver(mRGNetworkPlayerList);
                isServer = mRGNetworkPlayerList.isServer;
                CardPlayer baseRes = GameObject.FindObjectOfType<CardPlayer>();
                CardPlayer baseMal = GameObject.FindObjectOfType<CardPlayer>();
                int team = 0;
              
                if (baseRes != null && baseMal != null)
                {
                    turnTotal = 0;
                    setupActors();
                    float funds = 0;
                    myTurn = false;

                    if (isServer)
                    {
                        funds = maliciousActor.funds;
                        
                    }
                    else
                    {
                        funds = resiliencePlayer.funds;
                        team = (int)resiliencePlayer.playerType+1;
                        
                    }

                    GameObject obj = GameObject.Find("ExampleGameUI");
                    gameView = obj.GetComponent<RGGameExampleUI>();
              
                    if (isServer)
                    {
                        gameView.SetStartTeamInfo(maliciousActor, team, funds);
                    } else
                    {
                        gameView.SetStartTeamInfo(resiliencePlayer, team, funds);
                    }
                   
                    isInit = true;
                    
                }
            }
        }

    }

    // Will want to move to a game manager later
    public void EnableAllOutline(bool toggled)
    {
        //FacilityOutline[] allOutlines = GameObject.FindObjectsOfType<FacilityOutline>();
        //for (int i = 0; i < allOutlines.Length; i++)
        //{
        //    allOutlines[i].outline.SetActive(toggled);
        //}
    }


    public void EnableCriticalOutline(bool toggled)
    {
        //criticalEnabled = toggled;
        //FacilityOutline[] criticalOutlines = GameObject.FindObjectsOfType<FacilityOutline>();
        //for (int i = 0; i < criticalOutlines.Length; i++)
        //{
        //    // Comms
        //    if (criticalOutlines[i].gameObject.GetComponent<Communications>() != null)
        //    {
        //        criticalOutlines[i].outline.GetComponent<RawImage>().color = new Color(1.0f, 0.8431372549f, 0.0f, 1.0f);
        //        criticalOutlines[i].outline.SetActive(toggled);
        //    }

        //    // Water
        //    else if (criticalOutlines[i].gameObject.GetComponent<Water>() != null)
        //    {
        //        criticalOutlines[i].outline.GetComponent<RawImage>().color = new Color(1.0f, 0.8431372549f, 0.0f, 1.0f);

        //        criticalOutlines[i].outline.SetActive(toggled);

        //    }

        //    // Power
        //    else if (criticalOutlines[i].gameObject.GetComponent<ElectricityDistribution>() != null)
        //    {
        //        criticalOutlines[i].outline.GetComponent<RawImage>().color = new Color(1.0f, 0.8431372549f, 0.0f, 1.0f);

        //        criticalOutlines[i].outline.SetActive(toggled);

        //    }
        //    else if (criticalOutlines[i].gameObject.GetComponent<ElectricityGeneration>() != null)
        //    {
        //        criticalOutlines[i].outline.GetComponent<RawImage>().color = new Color(1.0f, 0.8431372549f, 0.0f, 1.0f);

        //        criticalOutlines[i].outline.SetActive(toggled);

        //    }

        //    // IT

        //    // Transport
        //    else if (criticalOutlines[i].gameObject.GetComponent<Transportation>() != null)
        //    {
        //        criticalOutlines[i].outline.GetComponent<RawImage>().color = new Color(1.0f, 0.8431372549f, 0.0f, 1.0f);

        //        criticalOutlines[i].outline.SetActive(toggled);

        //    }
        //    else
        //    {
        //        criticalOutlines[i].outline.SetActive(false);

        //    }
        //}
    }

    public void SwapPlayer()
    {
        //if ((continueButton.activeSelf == false) && (yarnSpinner.activeSelf == true))
        //{
        //    return;
        //}
        //else
        //{
        //    maliciousPlayerEndMenu.SetActive(false);
        //    resilientPlayerEndMenu.SetActive(false);
        //    playerActive = !playerActive;

        //    DisableAllOutline();
        //    resiliencePlayer.facilitiesActedUpon.Clear();
           
        //    maliciousActor.facilitiesActedUpon.Clear();
        //    maliciousActor.targetIDList.Clear();
        //    turnCount += 0.5f;
        //    if (playerActive)
        //    {
        //        activePlayerText.text = resiliencePlayer.playerType + " Player";
        //        fundText.text = "Funds: " + resiliencePlayer.funds;

        //        activePlayerColor = new Color(0.0f, 0.4209991f, 1.0f, 1.0f);
        //        activePlayerText.color = activePlayerColor;
        //        yarnSpinner.SetActive(true);
        //        facilityEvents.SpawnEvent();
        //        ChangePlayers();
        //        foreach (GameObject card in maliciousActor.HandList)
        //        {
        //            card.SetActive(false);
        //        }
        //        foreach (GameObject card in maliciousActor.ActiveCardList)
        //        {
        //            card.SetActive(false);
        //        }
        //        foreach (GameObject obj in allPlayers)
        //        {
        //            obj.GetComponent<CardPlayer>().facilitiesActedUpon.Clear();
        //        }
        //        foreach (GameObject obj in allPlayers)
        //        {
        //            obj.GetComponent<CardPlayer>().targetIDList.Clear();
        //        }
        //        maliciousActor.cardDropZone.SetActive(false);
        //        maliciousActor.handDropZone.SetActive(false);

        //    }
        //    else
        //    {
        //        fundText.text = "Funds: " + maliciousActor.funds;
        //        activePlayerText.text = "Malicious Player";
        //        activePlayerColor = new Color(1.0f, 0.0f, 0.0f, 1.0f);
        //        activePlayerText.color = activePlayerColor;
        //        yarnSpinner.SetActive(false);
        //        MalActorObject.SetActive(true);
        //        foreach (GameObject fac in allFacilities)
        //        {
        //            Color tempColor = fac.GetComponent<SVGImage>().color;
        //            tempColor.a = 1.0f;
        //            fac.GetComponent<SVGImage>().color = tempColor;
        //        }
        //        Debug.Log(maliciousActor.handSize);
        //        if (maliciousActor.handSize < 5)
        //        {
        //            maliciousActor.DrawCard(true, 0);
        //        }
        //        resiliencePlayer.cardDropZone.SetActive(false);
        //        resiliencePlayer.handDropZone.SetActive(false);
        //        foreach (GameObject card in resiliencePlayer.HandList)
        //        {
        //            card.SetActive(false);
        //        }
        //        foreach (GameObject card in resiliencePlayer.ActiveCardList)
        //        {
        //            card.SetActive(false);
        //        }
               
        //        foreach (GameObject card in maliciousActor.HandList)
        //        {
        //            card.SetActive(true);
        //        }
        //        foreach (GameObject card in maliciousActor.ActiveCardList)
        //        {
        //            card.SetActive(true);
        //        }
        //        maliciousActor.targetIDList.Clear();
        //        maliciousActor.facilitiesActedUpon.Clear();
        //        maliciousActor.cardDropZone.SetActive(true);
        //        maliciousActor.handDropZone.SetActive(true);
        //    }
        //}

    }
    public void DisableAllOutline()
    {
        //FacilityOutline[] allOutlines = GameObject.FindObjectsOfType<FacilityOutline>();
        //for (int i = 0; i < allOutlines.Length; i++)
        //{
        //    allOutlines[i].outline.SetActive(false);
        //}
    }

    public void EnableSwapPlayerMenu()
    {
        //if ((continueButton.activeSelf == false) && (yarnSpinner.activeSelf == true))
        //{
        //    return;
        //}
        //else
        //{
        //    if (playerActive)
        //    {
        //        resilientPlayerEndMenu.SetActive(true);
        //    }
        //    else
        //    {
        //        maliciousPlayerEndMenu.SetActive(true);
        //    }
        //}

    }

    public void StartGame()
    {
        gameCanvas.SetActive(true);
        //PlaceIcons  placeIcons = GameObject.Find("FakeMapTest").GetComponent<PlaceIcons>();
        //placeIcons.spawnAllFacilities(true, hospitalToggle.isOn, fireDeptToggle.isOn, elecGenToggle.isOn, waterToggle.isOn, commToggle.isOn, cityHallToggle.isOn, commoditiesToggle.isOn, elecDistToggle.isOn, fuelToggle.isOn, true);

        startScreen.SetActive(false); // DUsable the start menu where you determine how many of each facility you would like


        gameStarted = true;

        activePlayerNumber = 0;
        playerActive = false;
        turnCount = 0;
       
        if(!isServer)
        {
            // this is a resilience player
            if(resiliencePlayer.facilitiesActedUpon != null)
            {
                resiliencePlayer.facilitiesActedUpon.Clear();
            }
            playerActive = true;

        }
        else 
        {
            maliciousActor.facilitiesActedUpon.Clear();
            activePlayerColor = new Color(1.0f, 0.0f, 0.0f, 1.0f);
            activePlayerText.color = activePlayerColor;
        }

        if (playerActive)
        {

            activePlayerText.text = resiliencePlayer.playerType + " Player";
            Debug.Log("set active player to be reslilient");
            fundText.text = "Funds: " + resiliencePlayer.funds;
            activePlayerColor = new Color(0.0f, 0.4209991f, 1.0f, 1.0f);
            activePlayerText.color = activePlayerColor;
            //yarnSpinner.SetActive(true);
            Debug.Log("Starting player: " + resiliencePlayer.name);
            Debug.Log("set active player to be resilient with number of cards1 " + resiliencePlayer.HandList.Count);
            foreach (GameObject card in maliciousActor.HandList)
            {
                card.SetActive(true);
            }
            foreach (GameObject card in maliciousActor.ActiveCardList)
            {
                card.SetActive(true);
            }
            
            //foreach (GameObject fac in allFacilities)
            //{
            //    //if (fac.GetComponent<FacilityV3>().type == allPlayers[activePlayerNumber].GetComponent<Player>().type)
            //    if (fac.GetComponent<FacilityV3>().type == resiliencePlayer.type)
            //    {
            //        Color tempColor = fac.GetComponent<SVGImage>().color;
            //        tempColor.a = 1.0f;
            //        fac.GetComponent<SVGImage>().color = tempColor;
            //    }
            //    //else if (fac.GetComponent<FacilityV3>().type == FacilityV3.Type.ElectricityGeneration)
            //    //{
            //    //    Color tempColor = fac.GetComponent<SVGImage>().color;
            //    //    tempColor.a = 1.0f;
            //    //    fac.GetComponent<SVGImage>().color = tempColor;
            //    //}
            //    //else if (fac.GetComponent<FacilityV3>().type == FacilityV3.Type.ElectricityDistribution)
            //    //{
            //    //    Color tempColor = fac.GetComponent<SVGImage>().color;
            //    //    tempColor.a = 1.0f;
            //    //    fac.GetComponent<SVGImage>().color = tempColor;
            //    //}
            //    //else if (fac.GetComponent<FacilityV3>().type == FacilityV3.Type.Water)
            //    //{
            //    //    Color tempColor = fac.GetComponent<SVGImage>().color;
            //    //    tempColor.a = 1.0f;
            //    //    fac.GetComponent<SVGImage>().color = tempColor;
            //    //}
            //    //else if (fac.GetComponent<FacilityV3>().type == FacilityV3.Type.Transportation)
            //    //{
            //    //    Color tempColor = fac.GetComponent<SVGImage>().color;
            //    //    tempColor.a = 1.0f;
            //    //    fac.GetComponent<SVGImage>().color = tempColor;
            //    //}
            //    //else if (fac.GetComponent<FacilityV3>().type == FacilityV3.Type.Communications)
            //    //{
            //    //    Color tempColor = fac.GetComponent<SVGImage>().color;
            //    //    tempColor.a = 1.0f;
            //    //    fac.GetComponent<SVGImage>().color = tempColor;
            //    //}
            //    else
            //    {
            //        Color tempColor = fac.GetComponent<SVGImage>().color;
            //        tempColor.a = 0.5f;
            //        fac.GetComponent<SVGImage>().color = tempColor;
            //    }
            //}
            Debug.Log("player active");
        }
        else
        {
            if(isServer )
            {
                fundText.text = "Funds: " + maliciousActor.funds;
                activePlayerText.text = "Malicious Player";
                Debug.Log("set active player to be malicious");
                activePlayerColor = new Color(1.0f, 0.0f, 0.0f, 1.0f);
                activePlayerText.color = activePlayerColor;
                //yarnSpinner.SetActive(false);

                //foreach (GameObject players in allPlayers)
                //{
                //    foreach (GameObject card in players.GetComponent<Player>().HandList)
                //    {
                //        card.SetActive(false);
                //    }
                //    foreach (GameObject card in players.GetComponent<Player>().ActiveCardList)
                //    {
                //        card.SetActive(false);
                //    }
                //}
                //if(resPlayer != null)
                //{
                //    foreach (GameObject card in resPlayer.GetComponent<Player>().HandList)
                //    {
                //        card.SetActive(false);
                //    }
                //    foreach (GameObject card in resPlayer.GetComponent<Player>().ActiveCardList)
                //    {
                //        card.SetActive(false);
                //    }
                //}

                foreach (GameObject card in maliciousActor.HandList)
                {
                    card.SetActive(true);
                }
                foreach (GameObject card in maliciousActor.ActiveCardList)
                {
                    card.SetActive(true);
                }
            } else
            {
                fundText.text = "Funds: " + resiliencePlayer.funds;
                activePlayerText.text = "Resilient Player";
                Debug.Log("set active player to be resilient with number of cards1 " + resiliencePlayer.HandList.Count);
                activePlayerColor = Color.blue;
                activePlayerText.color = activePlayerColor;

                foreach (GameObject card in resiliencePlayer.HandList)
                {
                    card.SetActive(true);
                }
                foreach (GameObject card in resiliencePlayer.ActiveCardList)
                {
                    card.SetActive(true);
                }
            }
           
        }
    }

    public void ChangePlayers()
    {
        if (playerActive)
        {
            activePlayerNumber++;
            if (activePlayerNumber >= allPlayers.Length)
            {
                activePlayerNumber = 0;
            }
            activePlayerText.text = allPlayers[activePlayerNumber].GetComponent<CardPlayer>().playerType + " Player";
            fundText.text = "Funds: " + allPlayers[activePlayerNumber].GetComponent<CardPlayer>().funds;
            allPlayers[activePlayerNumber].SetActive(true);
            foreach (GameObject players in allPlayers)
            {
                if (players != allPlayers[activePlayerNumber])
                {
                    players.GetComponent<CardPlayer>().cardDropZone.SetActive(false);
                    players.GetComponent<CardPlayer>().handDropZone.SetActive(false);
                    foreach (GameObject card in players.GetComponent<CardPlayer>().HandList)
                    {
                        card.SetActive(false);

                    }
                    foreach (GameObject card in players.GetComponent<CardPlayer>().ActiveCardList)
                    {
                        card.SetActive(false);
                    }
                }
                else
                {
                    players.GetComponent<CardPlayer> ().cardDropZone.SetActive(true);
                    players.GetComponent<CardPlayer>().handDropZone.SetActive(true);
                    if (allPlayers[activePlayerNumber].GetComponent<CardPlayer>().handSize < 5)
                    {
                        allPlayers[activePlayerNumber].GetComponent<CardPlayer>().DrawCard(true, 0);
                    }
                    foreach (GameObject card in players.GetComponent<CardPlayer>().HandList)
                    {
                        card.SetActive(true);

                    }
                    foreach (GameObject card in players.GetComponent<CardPlayer>().ActiveCardList)
                    {
                        card.SetActive(true);
                    }
                }
            }
            //foreach (GameObject fac in allFacilities)
            //{
            //    if (fac.GetComponent<FacilityV3>().type == allPlayers[activePlayerNumber].GetComponent<Player>().type)
            //    {
            //        Color tempColor = fac.GetComponent<SVGImage>().color;
            //        tempColor.a = 1.0f;
            //        fac.GetComponent<SVGImage>().color = tempColor;
            //    }
                
            //    else
            //    {
            //        Color tempColor = fac.GetComponent<SVGImage>().color;
            //        tempColor.a = 0.5f;
            //        fac.GetComponent<SVGImage>().color = tempColor;
            //    }
            //}
            foreach (GameObject card in maliciousActor.HandList)
            {
                card.SetActive(false);
            }
            foreach (GameObject card in maliciousActor.ActiveCardList)
            {
                card.SetActive(false);
            }
            DisableAllOutline();
        }
        else
        {

            //foreach (GameObject fac in allFacilities)
            //{
            //    Color tempColor = fac.GetComponent<SVGImage>().color;
            //    tempColor.a = 1.0f;
            //    fac.GetComponent<SVGImage>().color = tempColor;
            //}
        }
    }

    public void SpawnPlayers(int playerCount)
    {
        GameObject basePlayer = GameObject.Find("Base Player");
        allPlayers = new GameObject[playerCount];
        for (int i = 0; i < playerCount; i++)
        {
            GameObject newPlayer = Instantiate(basePlayer);
            //switch (i)
            //{
            //    case 0:
            //        newPlayer.GetComponent<CardPlayer>().playerType = FacilityV3.Type.Fuel;
            //        break;

            //    case 1:
            //        newPlayer.GetComponent<CardPlayer>().type = FacilityV3.Type.Commodities;
            //        break;

            //    case 2:
            //        newPlayer.GetComponent<CardPlayer>().type = FacilityV3.Type.Health;
            //        break;

            //    case 3:
            //        newPlayer.GetComponent<CardPlayer>().type = FacilityV3.Type.Security;
            //        break;

            //    case 4:
            //        newPlayer.GetComponent<CardPlayer>().type = FacilityV3.Type.FireDept;
            //        break;

            //    case 5:
            //        newPlayer.GetComponent<CardPlayer>().type = FacilityV3.Type.City;
            //        break;

            //    case 6:
            //        newPlayer.GetComponent<CardPlayer>().type = FacilityV3.Type.PublicGoods;
            //        break;

            //    case 7:
            //        newPlayer.GetComponent<CardPlayer>().type = FacilityV3.Type.ElectricityGeneration;
            //        break;
            //    case 8:
            //        newPlayer.GetComponent<CardPlayer>().type = FacilityV3.Type.ElectricityDistribution;
            //        break;
            //    case 9:
            //        newPlayer.GetComponent<CardPlayer>().type = FacilityV3.Type.Water;
            //        break;
            //    case 10:
            //        newPlayer.GetComponent<CardPlayer>().type = FacilityV3.Type.Transportation;
            //        break;
            //}
            newPlayer.transform.SetParent(map.transform, false);
            newPlayer.name = newPlayer.GetComponent<CardPlayer>().playerType + " Player";
            allPlayers[i] = newPlayer;
        }
        basePlayer.SetActive(false);
    }
    public void SpawnMaliciousActor()
    {
        GameObject baseMalPlayer = GameObject.Find("Base Malicious Actor");
        GameObject newPlayer = Instantiate(baseMalPlayer);
        newPlayer.transform.SetParent(map.transform, false);
        newPlayer.name = "Malicious Actor";
        maliciousActor = newPlayer.GetComponent<CardPlayer>();
        MalActorObject = maliciousActor.gameObject;
        baseMalPlayer.SetActive(false);
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

   

    public void AddFunds(int count)
    {
        if (isServer)
        {
            maliciousActor.funds += count;
            fundText.text = "Funds: " + maliciousActor.funds;
        } else
        {
            resiliencePlayer.funds += count;
            fundText.text = "Funds: " + resiliencePlayer.funds;

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

    public void TestEndGame()
    {
       // RGNetworkPlayerList.instance.CmdEndGame(2);
    }

    public void SetPlayerType()
    {
        if (playerDeckChoice == null)
        {
            playerDeckChoice = playerDeckList.GetComponent<Dropdown>();
        }

       switch(playerDeckChoice.value)
        {
            case (int)Card.Type.Energy:
                playerType = Card.Type.Energy;
                break;
            case (int)Card.Type.WaterAndWasteWater:
                playerType = Card.Type.WaterAndWasteWater;
                break;
            default:
                break;
        }
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

    public void EndTurn()
    {
        if (myTurn)
        {
            gameView.HidePlayUI();
            Debug.Log("play ui hidden");
            AddMessage(new Message(CardMessageType.EndTurn));
            myTurn = false;
        }
    }

    public void IncrementTurn()
    {
        turnTotal++;
        gameView.turnText.text = "Turn: " + GetTurn();
        if (isServer)
        {
            Debug.Log("server adding increment turn message");
            AddMessage(new Message(CardMessageType.IncrementTurn));
        }
    }

    public void StartTurn()
    {
        if (!myTurn)
        {
            if (isServer)
            {
                foreach (GameObject card in maliciousActor.GetComponent<CardPlayer>().HandList)
                {
                    card.SetActive(true);
                }
                foreach (GameObject card in maliciousActor.GetComponent<CardPlayer>().ActiveCardList)
                {
                    card.SetActive(true);
                }
            }
            else
            {
                foreach (GameObject card in resiliencePlayer.GetComponent<CardPlayer>().HandList)
                {
                    card.SetActive(true);
                }
                foreach (GameObject card in resiliencePlayer.GetComponent<CardPlayer>().ActiveCardList)
                {
                    card.SetActive(true);
                }
            }
            myTurn = true;
            gameView.ShowPlayUI();
            AddFunds(100);
            Debug.Log("play ui shown");
        }

    }

    public int GetTurn()
    {
        return turnTotal;
    }

    public void ShowMyCardsToEverybody()
    {
        Message msg;
        List<int> tempCardIdList = new List<int>(5);
        // malicious player
        if (isServer)
        {
           for (int i=0; i<maliciousActor.HandList.Count; i++ )
            {
                Card cardObj = maliciousActor.HandList[i].GetComponent<Card>();
                tempCardIdList.Add(cardObj.cardID);
            }
        } else
        {
            for (int i = 0; i < resiliencePlayer.HandList.Count; i++)
            {
                Card cardObj = resiliencePlayer.HandList[i].GetComponent<Card>();
                tempCardIdList.Add(cardObj.cardID);
            }           
        }

        msg = new Message(CardMessageType.ShowCards, tempCardIdList);
        Debug.Log("showing cards: " + msg.arguments.Count);
        AddMessage(msg);
    }

    public void ShowOthersCards(List<int> cardsToShow)
    {
        if (allCardsPlayer == null)
        {
            allCardsPlayer = gameObject.AddComponent<CardPlayer>();
            allCardsPlayer.cardReader = GameObject.FindObjectOfType<CardReader>();
            allCardsPlayer.manager = this;
            allCardsPlayer.cardPrefab = resiliencePlayer.cardPrefab;
            allCardsPlayer.Deck = new List<int>(5);
            allCardsPlayer.CardCountList = new List<int>(5);
            allCardsPlayer.HandList = new List<GameObject>(5);
            //allCardsPlayer.Facilities = new List<GameObject>(5);
            for (int i = 0; i < allCardsPlayer.cardReader.CardIDs.Length; i++)
            {
                    allCardsPlayer.Deck.Add(i);
                    allCardsPlayer.CardCountList.Add(allCardsPlayer.cardReader.CardCount[i]);
            }
            Debug.Log("cardread done!");
            allCardsPlayer.handDropZone = gameView.showCardHolder;
            //allCardsPlayer.InitializeFacilities();
            Debug.Log("drawing cards");
            for (int i = 0; i < cardsToShow.Count; i++)
            {
                allCardsPlayer.DrawCard(false, cardsToShow[i]);
            }
            Debug.Log("cards drawn");
        } else
        {
            for (int i = 0; i < allCardsPlayer.HandList.Count; i++)
            {
                allCardsPlayer.HandList[i].SetActive(false);
                
            }
            allCardsPlayer.HandList.Clear();
            for (int i = 0; i < cardsToShow.Count; i++)
            {
                allCardsPlayer.DrawCard(false, cardsToShow[i]);
            }
        }

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
