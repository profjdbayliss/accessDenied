using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardPlayer : MonoBehaviour
{
    // Establish necessary fields
    public Card.Type playerType = Card.Type.Energy;
    public float funds = 100.0f;
    public GameManager manager;
    public CardReader cardReader;
    public List<int> Deck;
    public List<int> CardCountList;
    public List<int> targetIDList;
    public List<GameObject> HandList;
    public List<GameObject> ActiveCardList;
    public List<int> activeCardIDs;
    public int handSize;
    public int maxHandSize = 6;
    public GameObject cardPrefab;
    public GameObject cardDropZone;
    public GameObject handDropZone;
    public List<GameObject> facilitiesActedUpon;
    public bool redoCardRead = false;

    public void InitializeCards()
    {
        // NOTE: set funds in scene var
        cardReader = GameObject.FindObjectOfType<CardReader>();
        manager = GameObject.FindObjectOfType<GameManager>();
        int count = 0;
        for (int i = 0; i < cardReader.CardIDs.Length; i++)
        {
            if (cardReader.CardTeam[i] == (int)playerType) 
            {
                Deck.Add(i);
                CardCountList.Add(cardReader.CardCount[i]);
                count++;
            }
        }
        
        if (HandList.Count < maxHandSize)
        {
            for (int i = 0; i < maxHandSize; i++)
            {
                DrawCard(true, 0);
            }
        }
    }

    public virtual void DrawCard(bool random, int cardId)
    {
        int rng;
        if (random)
        {
            rng = UnityEngine.Random.Range(0, Deck.Count);
        } else
        {
            rng = cardId;
        }
        
        if (CardCountList.Count <= 0) // Check to ensure the deck is actually built before trying to draw a card
        {
            Debug.Log("no cards drawn.");
            return;
        }
        if (CardCountList[rng] > 0)
        {
            CardCountList[rng]--;
            GameObject tempCardObj = Instantiate(cardPrefab);
            Card tempCard = tempCardObj.GetComponent<Card>();
            tempCard.cardDropZone = cardDropZone;
            tempCard.cardID = Deck[rng];

            // WORK: not sure the below case ever happens
            if (cardReader.CardFronts[Deck[rng]] == null && redoCardRead == false)
            {
                cardReader.CSVRead();
                redoCardRead = true;
            }

            tempCard.front = cardReader.CardFronts[Deck[rng]];

            RawImage[] tempRaws = tempCardObj.GetComponentsInChildren<RawImage>();
            for (int i = 0; i < tempRaws.Length; i++)
            {
                if (tempRaws[i].name == "Image")
                {
                    tempRaws[i].texture = tempCard.front.img;
                }
                else if (tempRaws[i].name == "Background")
                {
                    //tempRaws[i].color = new Color(0.8067818f, 0.8568867f, 0.9245283f, 1.0f);
                }
            }

            TextMeshProUGUI[] tempTexts = tempCardObj.GetComponentsInChildren<TextMeshProUGUI>(true);
            for (int i = 0; i < tempTexts.Length; i++)
            {
                if (tempTexts[i].name == "Title Text")
                {
                    tempTexts[i].text = Encoding.ASCII.GetString(tempCard.front.title);
                }
                else if (tempTexts[i].name == "Description Text")
                {
                    tempTexts[i].text = Encoding.ASCII.GetString(tempCard.front.description);
                }
            }
            TextMeshProUGUI[] tempInnerText = tempCardObj.GetComponentsInChildren<TextMeshProUGUI>(true);
            for (int i = 0; i < tempInnerText.Length; i++)
            {  
                if (tempInnerText[i].name == "Cost Text")
                {
                    tempInnerText[i].text = cardReader.CardCost[Deck[rng]].ToString();
                } 
            }

            tempCard.percentSuccess = cardReader.CardPercentChance[Deck[rng]];

            tempCard.duration = cardReader.CardDuration[Deck[rng]];
            tempCard.cost = cardReader.CardCost[Deck[rng]];
            tempCard.teamID = cardReader.CardTeam[Deck[rng]];
            tempCardObj.GetComponent<slippy>().map = tempCardObj;
            tempCard.state = Card.CardState.CardDrawn;
            Vector3 tempPos = tempCardObj.transform.position;
            tempCardObj.transform.position = tempPos;
            tempCardObj.transform.SetParent(handDropZone.transform, false);
            Vector3 tempPos2 = handDropZone.transform.position;
            handSize++;
            tempCardObj.transform.position = tempPos2;
            HandList.Add(tempCardObj);
        }
        else
        {
            // WORK: does this condition ever happen? Is there a card with the id of 0???
            Debug.Log("random number was less than 0");
            DrawCard(true, cardId);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (HandList != null)
        {
            foreach (GameObject card in HandList)
            {
                if (card.GetComponent<Card>().state == Card.CardState.CardInPlay)
                {
                    HandList.Remove(card);
                    ActiveCardList.Add(card);
                    activeCardIDs.Add(card.GetComponent<Card>().cardID);
                    card.GetComponent<Card>().duration = cardReader.CardDuration[card.GetComponent<Card>().cardID] + manager.turnCount;
                    break;
                }
            }
        }
        if (ActiveCardList != null)
        {
            foreach (GameObject card in ActiveCardList)
            {
                if (manager.turnCount >= card.GetComponent<Card>().duration)
                {
                    ActiveCardList.Remove(card);
                    activeCardIDs.Remove(card.GetComponent<Card>().cardID);
                    card.SetActive(false);
                    break;
                }
            }
        }
    }

    // there are no facilities in this game
    public bool SelectFacility(int cardID)
    {
        return false;
    }

    public virtual void PlayCard(int cardID, int[] targetID, int targetCount = 3)
    {
       // WORK
    }

    public void DiscardCard(int cardID)
    {
       // WORK

    }
}
