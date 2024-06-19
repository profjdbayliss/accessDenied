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
    public List<int> FacilityIDs = new List<int>(10);
    public List<int> DeckIDs = new List<int>(52);
    public List<int> CardCountList;
    public List<int> targetIDList;
    public List<GameObject> HandList;
    public List<int> HandListIds = new List<int>(6);
    public List<GameObject> DiscardCards = new List<GameObject>(6);
    public List<int> DiscardIds = new List<int>(6);
    public List<GameObject> ActiveCardList;
    public List<int> activeCardIDs;
    public List<int> ActiveFacilityIDs = new List<int>(8);
    public List<GameObject> ActiveFacilities = new List<GameObject>(8);
    public int handSize;
    public int maxHandSize = 6;
    public GameObject cardPrefab;
    public GameObject discardDropZone;
    public GameObject handDropZone;
    public GameObject opponentDropZone;
    public GameObject playerDropZone;

    public bool redoCardRead = false;
    Vector2 discardDropMin;
    Vector2 discardDropMax;
    Vector2 playedDropMin;
    Vector2 playedDropMax;
    Vector2 opponentDropMin;
    Vector2 opponentDropMax;


    public void Start()
    {
        // discard rectangle information for AABB collisions
        RectTransform discardRectTransform = discardDropZone.GetComponent<RectTransform>();
        discardDropMin.x = discardRectTransform.position.x - (discardRectTransform.rect.width / 2);
        discardDropMin.y = discardRectTransform.position.y - (discardRectTransform.rect.height / 2);
        discardDropMax.x = discardRectTransform.position.x + (discardRectTransform.rect.width / 2);
        discardDropMax.y = discardRectTransform.position.y + (discardRectTransform.rect.height / 2);

        // played area rectangle information for AABB collisions
        RectTransform playedRectTransform = playerDropZone.GetComponent<RectTransform>();
        playedDropMin.x = playedRectTransform.position.x - (playedRectTransform.rect.width / 2);
        playedDropMin.y = playedRectTransform.position.y - (playedRectTransform.rect.height / 2);
        playedDropMax.x = playedRectTransform.position.x + (playedRectTransform.rect.width / 2);
        playedDropMax.y = playedRectTransform.position.y + (playedRectTransform.rect.height / 2);

        // playing on opponent area rectangle information
        RectTransform opponentRectTransform = opponentDropZone.GetComponent<RectTransform>();
        opponentDropMin.x = opponentRectTransform.position.x - (opponentRectTransform.rect.width / 2);
        opponentDropMin.y = opponentRectTransform.position.y - (opponentRectTransform.rect.height / 2);
        opponentDropMax.x = opponentRectTransform.position.x + (opponentRectTransform.rect.width / 2);
        opponentDropMax.y = opponentRectTransform.position.y + (opponentRectTransform.rect.height / 2);

    }

    public void InitializeCards()
    {
        manager = GameManager.instance;

        for (int i = 0; i < cards.Count; i++)
        {
            if (cards[i].data.cardType != CardType.Station)
            {
                for (int j = 0; j < cards[i].data.numberInDeck; j++)
                {
                    DeckIDs.Add(cards[i].data.cardID);
                }

            }
            else
            {
                for (int j = 0; j < cards[i].data.numberInDeck; j++)
                {
                    FacilityIDs.Add(cards[i].data.cardID);
                }
            }

        }
    }

    public virtual void DrawCards()
    {
        if (HandList.Count < maxHandSize)
        {
            int count = HandList.Count;
            for (int i = 0; i < maxHandSize - count; i++)
            {
                DrawCard(true, 0, ref DeckIDs, handDropZone, true, ref HandList, ref HandListIds);
            }
        }
    }

    public virtual int DrawFacility(bool isRandom, int worth)
    {
        int id = -1;
        if (FacilityIDs.Count > 0)
        {
            if (isRandom)
            {
                id = DrawCard(true, 0, ref FacilityIDs, playerDropZone, false,
                    ref ActiveFacilities, ref ActiveFacilityIDs);
            } else
            {
                // need to draw the 2 pt facility according to rules
                // at the beginning!
                for (int i = 0; i < FacilityIDs.Count; i++)
                {
                    if (cards[FacilityIDs[i]].data.worth == worth)
                    {
                        id = DrawCard(false, i, ref FacilityIDs, playerDropZone, false,
                            ref ActiveFacilities, ref ActiveFacilityIDs);
                        break;
                    }
                }
            }
        }

        return id;
    }

    public virtual int DrawCard(bool random, int cardId, ref List<int> deckToDrawFrom,
        GameObject dropZone, bool allowSlippy,
        ref List<GameObject> activeDeckObjs, ref List<int> activeDeckIDs)
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
        tempCard.cardZone = handDropZone;
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
        activeDeckObjs.Add(tempCardObj);
        activeDeckIDs.Add(tempCard.data.cardID);

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
        //        if (card.GetComponent<Card>().state == CardState.CardInPlay)
        //        {
        //            int index = HandList.FindIndex(x => x.Equals(card));
        //            if (index >= 0)
        //            {
        //                HandList.Remove(card);
        //                HandListIds.RemoveAt(index);
        //                ActiveCardList.Add(card);
        //                activeCardIDs.Add(card.GetComponent<Card>().data.cardID);
        //                //card.GetComponent<Card>().duration = cardReader.CardDuration[card.GetComponent<Card>().cardID] + manager.turnCount;
        //            }
        //            break;
        //        }
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

    public virtual int HandlePlayCard(GamePhase phase)
    {
        int playCount = 0;
        int playIndex = 0;

        if (HandList != null)
        {
            foreach (GameObject gameObjectCard in HandList)
            {
                Card card = gameObjectCard.GetComponent<Card>();
                if (card.state == CardState.CardDrawnDropped)
                {
                    // card has been dropped somewhere - where?
                    Vector2 cardPosition = card.getDroppedPosition();
                    //Debug.Log("card transform inside of player: " + cardPosition.x + ":" + cardPosition.y);
                    Debug.Log("drop info in card is: " + discardDropMin + " " + discardDropMax);

                    // DO a AABB collision test to see if the card is on the player's drop
                    if (cardPosition.y < playedDropMax.y &&
                       cardPosition.y > playedDropMin.y &&
                       cardPosition.x < playedDropMax.x &&
                       cardPosition.x > playedDropMin.x)
                    {
                        switch (phase)
                        {
                            case GamePhase.Defense:
                                Debug.Log("card dropped in played zone");
                                if (CheckHighlightedStations())
                                {
                                    Debug.Log("this card should be played for defense now.");
                                    GameObject selected = GetHighlightedStation();
                                    Transform parent = gameObjectCard.transform.parent;
                                    gameObjectCard.transform.SetParent(null, true);

                                    //RectTransform rect = gameObjectCard.GetComponent<RectTransform>();
                                    //gameObjectCard.transform.localScale = new Vector2(1.0f, 1.0f);
                                    //rect.position = selected.transform.position + new Vector3(0, 1, 0);
                                    gameObjectCard.GetComponentInParent<slippy>().originalScale = new Vector2(1.0f, 1.0f);
                                    gameObjectCard.GetComponentInParent<slippy>().ResetScale();
                                    gameObjectCard.GetComponentInParent<HoverScale>().previousScale = new Vector2(1.0f, 1.0f);
                                    gameObjectCard.transform.SetPositionAndRotation(selected.transform.position + 
                                        new Vector3(0, 20, 0),
                                        gameObject.transform.rotation);
                                    //gameObjectCard.transform.SetParent(parent, true);
                                    gameObjectCard.transform.SetParent(selected.transform, true);
                                    //gameObjectCard.transform.SetParent(selected.transform, true);
                                    card.state = CardState.CardInPlay;
                                    gameObjectCard.GetComponentInParent<slippy>().enabled = false;
                                    gameObjectCard.GetComponent<HoverScale>().Drop();
                                    activeCardIDs.Add(card.data.cardID);
                                    ActiveCardList.Add(gameObjectCard);
                                    playCount = 1; 
                                }
                                else
                                {
                                    card.state = CardState.CardDrawn;
                                    manager.DisplayGameStatus("Please select a single facility to receive this card's effect.");
                                }
                                break;
                            default:
                                // we're not in the right phase, so
                                // reset the dropped state
                                card.state = CardState.CardDrawn;
                                break;
                        }

                    }
                    else
                    if (cardPosition.y < opponentDropMax.y &&
                       cardPosition.y > opponentDropMin.y &&
                       cardPosition.x < opponentDropMax.x &&
                       cardPosition.x > opponentDropMin.x)
                    {
                        Debug.Log("card dropped in opponent zone");
                        card.state = CardState.CardDrawn;
                    }
                    else
                    {
                        Debug.Log("card not dropped in card drop zone");
                        // If it fails, parent it back to the hand location and then set its state to be in hand and make it grabbable again
                        gameObjectCard.transform.SetParent(handDropZone.transform, false);
                        card.state = CardState.CardDrawn;
                        gameObjectCard.GetComponentInParent<slippy>().enabled = true;
                        gameObjectCard.GetComponent<HoverScale>().Drop();
                    }
                }

                // index of where this card is in handlist
                if (playCount > 0)
                {
                    break;
                } else
                {
                    playIndex++;
                }             
            }
        }

        if (playCount > 0)
        {
            // remove the discarded card
            HandList.RemoveAt(playIndex);
            HandListIds.RemoveAt(playIndex);

        }

        return playCount;
    }

    public void ClearDropState()
    {
        if (HandList != null)
        {
            foreach (GameObject cardGameObject in HandList)
            {
                Card card = cardGameObject.GetComponent<Card>();
                if (card.state == CardState.CardDrawnDropped)
                {
                    card.state = CardState.CardDrawn;
                }
            }
        }
    }

    public bool CheckHighlightedStations()
    {
        bool singleHighlighted = false;
        int countHighlighted = 0;

        foreach (GameObject gameObject in ActiveFacilities)
        {
            Card card = gameObject.GetComponent<Card>();
            if (card.OutlineImage.activeSelf)
            {
                countHighlighted++;
            }
        }

        if (countHighlighted == 1)
            singleHighlighted = true;

        return singleHighlighted;
    }

    public int HandleDiscards()
    {
        int discardCount = 0;
        int discardIndex = 0;

        if (HandList != null)
        {
            foreach (GameObject gameObjectCard in HandList)
            {
                Card card = gameObjectCard.GetComponent<Card>();
                if (card.state == CardState.CardDrawnDropped)
                {
                    Vector2 cardPosition = card.getDroppedPosition();
                    Debug.Log("card transform inside of player: " + cardPosition.x + ":" + cardPosition.y);
                    Debug.Log("discard drop info in card is: " + discardDropMin + " " + discardDropMax);

                    // DO a AABB collision test to see if the card is on the card drop
                    if (cardPosition.y < discardDropMax.y &&
                       cardPosition.y > discardDropMin.y &&
                       cardPosition.x < discardDropMax.x &&
                       cardPosition.x > discardDropMin.x)
                    {
                        Debug.Log("card dropped in discard zone");
                        if (GameManager.instance.isDiscardAllowed)
                        {
                            // remove from player lists
                            int index = HandList.FindIndex(x => x.Equals(gameObjectCard));
                            if (index >= 0)
                            {
                                discardIndex = index;
                                Debug.Log("discard is allowed and will be done now");
                                DiscardCards.Add(gameObjectCard);
                                DiscardIds.Add(card.data.cardID);

                                // change parent and rescale
                                card.state = CardState.CardDiscarded;
                                gameObjectCard.GetComponentInParent<slippy>().enabled = false;
                                gameObjectCard.GetComponentInParent<slippy>().ResetScale();
                                gameObjectCard.transform.SetParent(discardDropZone.transform, false);
                                card.cardZone = discardDropZone;
                                discardCount++;

                                // only allow one discard per cycle
                                // don't think it's possible for more than one anyway
                                break;
                            } else
                            {
                                Debug.Log("problem with discard: card index not found!");
                                gameObjectCard.transform.SetParent(handDropZone.transform, false);
                                card.state = CardState.CardDrawn;
                                gameObjectCard.GetComponentInParent<slippy>().enabled = true;
                                gameObjectCard.GetComponent<HoverScale>().Drop();
                            }

                        } else
                        {
                            // we're not allowing this card to be discarded
                            gameObjectCard.transform.SetParent(handDropZone.transform, false);
                            card.state = CardState.CardDrawn;
                            gameObjectCard.GetComponentInParent<slippy>().enabled = true;
                            gameObjectCard.GetComponent<HoverScale>().Drop();
                        }
                    }
                    else
                    {
                        Debug.Log("card not dropped in card drop zone");
                        // If it fails, parent it back to the hand location and then set its state to be in hand and make it grabbable again
                        gameObjectCard.transform.SetParent(handDropZone.transform, false);
                        card.state = CardState.CardDrawn;
                        gameObjectCard.GetComponentInParent<slippy>().enabled = true;
                        gameObjectCard.GetComponent<HoverScale>().Drop();
                        //Debug.Log("card reset position done");
                    }
                }


            }
        }

        if (discardCount > 0)
        {
            // remove the discarded card
            HandList.RemoveAt(discardIndex);
            HandListIds.RemoveAt(discardIndex);

        }

        return discardCount;
    }

    public GameObject GetHighlightedStation() {
        GameObject station = null;

        foreach (GameObject gameObject in ActiveFacilities)
        {
            Card card = gameObject.GetComponent<Card>();
            if (card.OutlineImage.activeSelf)
            {
                station = gameObject;
                break;
            }
        }

        return station;
    }

    public bool CheckForCardsOfType(CardType cardType, List<GameObject> listToCheck)
    {
        bool hasCardType = false;

        foreach(GameObject gameObject in listToCheck)
        {
            Card card = gameObject.GetComponent<Card>();
            if (card.data.cardType == cardType)
            {
                hasCardType = true;
                break;
            }
        }

        return hasCardType;
    }
}
