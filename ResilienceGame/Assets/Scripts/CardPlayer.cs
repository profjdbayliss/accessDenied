using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Enum to track player type
public enum PlayerType
{
    Water,
    Energy,
    Any
};

public class CardPlayer : MonoBehaviour
{
    // Establish necessary fields
    public PlayerType playerType = PlayerType.Energy;
    public GameManager manager;
    public List<Card> cards;
    public List<int> Facilities = new List<int>(10);
    public List<int> Deck = new List<int>(52);
    public List<int> CardCountList;
    public List<int> targetIDList;
    public List<GameObject> HandList;
    public List<GameObject> ActiveCardList;
    public List<int> activeCardIDs;
    public List<int> activeFacilityIDs = new List<int>(8);
    public int handSize;
    public int maxHandSize = 6;
    public GameObject cardPrefab;
    public GameObject cardDropZone;
    public GameObject handDropZone;
    public GameObject playedCardZone;
    public GameObject playerPlayedZone;

    public bool redoCardRead = false;

    public void InitializeCards()
    {
        manager = GameManager.instance;

        for (int i = 0; i <cards.Count; i++)
        {
            if (cards[i].data.type != CardType.Station)
            {
                for (int j=0; j < cards[i].data.numberInDeck; j++)
                {
                    Deck.Add(cards[i].data.cardID);
                }
                
            } else
            {
                for (int j = 0; j < cards[i].data.numberInDeck; j++)
                {
                    Facilities.Add(cards[i].data.cardID);
                }  
            }
            
        }
    }

    public virtual void DrawCards()
    {
        if (HandList.Count < maxHandSize)
        {
            int count = HandList.Count;
            for (int i = 0; i < maxHandSize-count; i++)
            {
                DrawCard(true, 0, ref Deck, handDropZone, true);
            }
        }
    }

    public virtual int DrawFacility(bool isRandom, int worth)
    {
        int id = -1;
        if (Facilities.Count > 0)
        {
            if (isRandom)
            {
                id = DrawCard(true, 0, ref Facilities, playerPlayedZone, false);
                if (id != -1)
                {
                    activeFacilityIDs.Add(id);
                }
            } else
            {
                // need to draw the 2 pt facility according to rules
                // at the beginning!
                for (int i=0; i<Facilities.Count; i++)
                {
                    if (cards[Facilities[i]].data.worth == worth)
                    {
                        id = DrawCard(false, i, ref Facilities, playerPlayedZone, false);
                        if (id != -1)
                        {
                            activeFacilityIDs.Add(id);
                        }
                        break;
                    }
                }
            }
        }

        return id;
    }

    public virtual int DrawCard(bool random, int cardId, ref List<int> deckToDrawFrom,
        GameObject dropZone, bool allowSlippy)
    {
        int rng = -1;
        if (random)
        {
            rng = UnityEngine.Random.Range(0, deckToDrawFrom.Count);
        }
        else
        {
            rng = cardId;
        }

        if (deckToDrawFrom.Count <= 0) // Check to ensure the deck is actually built before trying to draw a card
        {
            Debug.Log("no cards drawn.");
            return rng;
        }

        GameObject tempCardObj = Instantiate(cardPrefab);
        Card tempCard = tempCardObj.GetComponent<Card>();
        tempCard.cardDropZone = cardDropZone;
        Card actualCard = cards[deckToDrawFrom[rng]];
        tempCard.data = actualCard.data;
        CardFront front = actualCard.GetComponent<CardFront>();
        tempCard.front = front;
        //tempCard.handDropZone = actualCard.handDropZone;

        //    // WORK: not sure the below case ever happens
        //    //if (cardReader.CardFronts[Deck[rng]] == null && redoCardRead == false)
        //    //{
        //    //    cardReader.CSVRead();
        //    //    redoCardRead = true;
        //    //}

        RawImage[] tempRaws = tempCardObj.GetComponentsInChildren<RawImage>();
        for (int i = 0; i < tempRaws.Length; i++)
        {
            if (tempRaws[i].name == "Image")
            {
                tempRaws[i].texture = tempCard.front.img;
            }
            else if (tempRaws[i].name == "Background")
            {
                tempRaws[i].color = tempCard.front.titleColor;
            }
        }

        TextMeshProUGUI[] tempTexts = tempCardObj.GetComponentsInChildren<TextMeshProUGUI>(true);
        for (int i = 0; i < tempTexts.Length; i++)
        {
            if (tempTexts[i].name == "Title Text")
            {
                tempTexts[i].text = tempCard.front.title;
            }
            else if (tempTexts[i].name == "Description Text")
            {
                tempTexts[i].text = tempCard.front.description;
            }
        }
        //TextMeshProUGUI[] tempInnerText = tempCardObj.GetComponentsInChildren<TextMeshProUGUI>(true);
        //for (int i = 0; i < tempInnerText.Length; i++)
        //{
        //    //if (tempInnerText[i].name == "Cost Text")
        //    //{
        //    //    tempInnerText[i].text = cardReader.CardCost[Deck[rng]].ToString();
        //    //} 
        //}

        //    //tempCard.duration = cardReader.CardDuration[Deck[rng]];
        //    //tempCard.cost = cardReader.CardCost[Deck[rng]];
        //    //tempCard.teamID = cardReader.CardTeam[Deck[rng]];
        tempCardObj.GetComponent<slippy>().map = tempCardObj;
        if (!allowSlippy)
        {
            slippy tempSlippy = tempCardObj.GetComponent<slippy>();
            tempSlippy.enabled = false;
        }
        tempCard.state = CardState.CardDrawn;
        Vector3 tempPos = tempCardObj.transform.position;
        tempCardObj.transform.position = tempPos;
        tempCardObj.transform.SetParent(dropZone.transform, false);
        Vector3 tempPos2 = dropZone.transform.position;
        handSize++;
        tempCardObj.transform.position = tempPos2;
        tempCardObj.SetActive(true);
        HandList.Add(tempCardObj);

        // remove this card so we don't draw it again
        deckToDrawFrom.RemoveAt(rng);
        return rng;
    }

    // Update is called once per frame
    void Update()
    {
        //if (HandList != null)
        //{
        //    foreach (GameObject card in HandList)
        //    {
        //        //if (card.GetComponent<Card>().state == CardState.CardInPlay)
        //        //{
        //        //    HandList.Remove(card);
        //        //    ActiveCardList.Add(card);
        //        //    activeCardIDs.Add(card.GetComponent<Card>().data.cardID);
        //        //    card.GetComponent<Card>().duration = cardReader.CardDuration[card.GetComponent<Card>().cardID] + manager.turnCount;
        //        //    break;
        //        //}
        //    }
        //}
        //if (ActiveCardList != null)
        //{
        //    foreach (GameObject card in ActiveCardList)
        //    {
        //        //if (manager.turnCount >= card.GetComponent<Card>().duration)
        //        //{
        //        //    ActiveCardList.Remove(card);
        //        //    activeCardIDs.Remove(card.GetComponent<Card>().cardID);
        //        //    card.SetActive(false);
        //        //    break;
        //        //}
        //    }
        //}
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
