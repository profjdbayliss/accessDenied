using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.UI;
using Image = UnityEngine.UI.Image;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

// Enum to track player type
public enum PlayerType
{
    Water,
    Energy,
    Any
};

public enum AddOrRem
{
    Add,
    Remove
};

public struct Updates
{
    public AddOrRem WhatToDo;
    public int UniqueFacilityID;
    public int CardID;
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
    public GameObject cardStackingCanvas;
    public readonly float ORIGINAL_SCALE = 0.2f;
    public bool redoCardRead = false;

    Vector2 discardDropMin;
    Vector2 discardDropMax;
    Vector2 playedDropMin;
    Vector2 playedDropMax;
    Vector2 opponentDropMin;
    Vector2 opponentDropMax;
    int mUniqueIDCount = 0;

    List<Updates> mUpdatesThisPhase = new List<Updates>(6);

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
                DrawCard(true, 0, -1, ref DeckIDs, handDropZone, true, ref HandList, ref HandListIds);
            }
        }
    }

    public virtual Card DrawFacility(bool isRandom, int worth)
    {
        Card card = null;
        if (FacilityIDs.Count > 0)
        {
            if (isRandom)
            {
                card = DrawCard(true, 0, -1, ref FacilityIDs, playerDropZone, false,
                    ref ActiveFacilities, ref ActiveFacilityIDs);
            } else
            {
                // need to draw the 2 pt facility according to rules
                // at the beginning!
                for (int i = 0; i < FacilityIDs.Count; i++)
                {
                    if (cards[FacilityIDs[i]].data.worth == worth)
                    {
                        card = DrawCard(false, FacilityIDs[i], -1, ref FacilityIDs, playerDropZone, false,
                            ref ActiveFacilities, ref ActiveFacilityIDs);
                        break;
                    }
                }
            }
        }

        Debug.Log("facility was drawn and its unique id is: " + card.UniqueID);
        // always turn slippy off for facilities as we can't move them
        slippy theSlippy = card.GetComponent<slippy>();
        if (theSlippy != null)
        {
            theSlippy.enabled = false;
        }

        return card;
    }

    public virtual Card DrawCard(bool random, int cardId, int uniqueId, ref List<int> deckToDrawFrom,
        GameObject dropZone, bool allowSlippy,
        ref List<GameObject> activeDeckObjs, ref List<int> activeDeckIDs)
    {
        int rng = -1;
        Card actualCard;
        
        if (random)
        {
            rng = UnityEngine.Random.Range(0, deckToDrawFrom.Count);
            actualCard = cards[deckToDrawFrom[rng]];
        }
        else
        {
            rng = -1;
            // this assumes our id is in the deck somewhere
            // we just don't know where
            for (int i=0; i<deckToDrawFrom.Count; i++)
            {
                actualCard = cards[deckToDrawFrom[i]];
                if (actualCard.data.cardID == cardId)
                {
                    Debug.Log("found card id " + cardId);
                    // we found our card yay!
                    rng = i;
                    break;
                }
            }
            if (rng == -1)
            {
                Debug.Log("Error: handed the card deck a card id that isn't in the deck! " + cardId);
                rng = 0;
            }
            actualCard = cards[deckToDrawFrom[rng]];
        }

        if (deckToDrawFrom.Count <= 0) // Check to ensure the deck is actually built before trying to draw a card
        {
            Debug.Log("no cards drawn.");
            return null;
        }

        GameObject tempCardObj = Instantiate(cardPrefab);
        Card tempCard = tempCardObj.GetComponent<Card>();
        tempCard.cardZone = handDropZone;
        tempCard.data = actualCard.data;
        if (uniqueId != -1)
        {
            tempCard.UniqueID = uniqueId;
            Debug.Log("setting unique id for facility " + uniqueId);
        } else
        {
            tempCard.UniqueID = mUniqueIDCount;
            mUniqueIDCount++;
        }
        
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

        Image[] tempImage = tempCardObj.GetComponentsInChildren<Image>();
        for (int i = 0; i < tempImage.Length; i++)
        {
          if (tempImage[i].name.Equals("LeftCardSlot"))
            {
                if (tempCard.front.worthCircle)
                {
                    // enable circle
                    tempImage[i].enabled = true;
                }
                else
                {
                    tempImage[i].enabled = false;
                }
            }
            else if (tempImage[i].name.Equals("RightCardSlot"))
            {
                if (tempCard.front.costCircle)
                {
                    // enable circle
                    tempImage[i].enabled = true;
                }
                else
                {
                    tempImage[i].enabled = false;
                }
            }
        }

        TextMeshProUGUI[] tempTexts = tempCardObj.GetComponentsInChildren<TextMeshProUGUI>(true);
        for (int i = 0; i < tempTexts.Length; i++)
        {
            if (tempTexts[i].name.Equals("Title Text"))
            {
                tempTexts[i].text = tempCard.front.title;
            }
            else if (tempTexts[i].name.Equals("Description Text"))
            {
                tempTexts[i].text = tempCard.front.description;
            } else if (tempTexts[i].name.Equals("LeftCardNumber") )
            {
                if (tempCard.front.worthCircle)
                {
                    // set the text number for worth
                    tempTexts[i].enabled = true;
                    tempTexts[i].text = tempCard.data.worth + "";
                } else
                {
                    // turn off the text box
                    tempTexts[i].enabled = false;
                }
                
            } else if (tempTexts[i].name.Equals("RightCardNumber"))
            {
                if (tempCard.front.costCircle)
                {
                    // set the text number for cost
                    tempTexts[i].enabled = true;
                    tempTexts[i].text = tempCard.data.cost + "";
                }
                else
                {
                    // turn off the text box
                    tempTexts[i].enabled = false;
                }
            }
        }

        switch (tempCard.data.cardType)
        {
            case CardType.Defense:
                tempCard.ActionList.Add(new ActionAddDefenseWorthToStation());
                break;
            case CardType.Mitigation:
                tempCard.ActionList.Add(new ActionMitigateCard());
                break;
            default:
                break;
        }

        foreach(string mitigation in cards[deckToDrawFrom[rng]].MitigatesWhatCards)
        {
            tempCard.MitigatesWhatCards.Add(mitigation);
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
        tempCardObj.GetComponent<slippy>().DraggableObject = tempCardObj;
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
        return tempCard;
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
                    //Debug.Log("drop info in card is: " + discardDropMin + " " + discardDropMax);

                    // DO a AABB collision test to see if the card is on the player's drop
                    if (cardPosition.y < playedDropMax.y &&
                       cardPosition.y > playedDropMin.y &&
                       cardPosition.x < playedDropMax.x &&
                       cardPosition.x > playedDropMin.x)
                    {
                        switch (phase)
                        {
                            case GamePhase.Defense:
                                if (card.data.cardType==CardType.Defense && CheckHighlightedStations())
                                {
                                    GameObject selected = GetHighlightedStation();
                                    Card selectedCard = selected.GetComponent<Card>();
                                    StackCards(selected, gameObjectCard, playerDropZone, GamePhase.Defense);
                                    card.state = CardState.CardInPlay;
                                    activeCardIDs.Add(card.UniqueID);
                                    ActiveCardList.Add(gameObjectCard);
                                    selectedCard.ModifyingCards.Add(card.UniqueID);
                                    mUpdatesThisPhase.Add(new Updates
                                    {
                                        WhatToDo=AddOrRem.Add,
                                        UniqueFacilityID=selectedCard.UniqueID,
                                        CardID=card.data.cardID
                                    });

                                    // we should play the card's effects
                                    card.Play(this, selectedCard);
                                    playCount = 1; 
                                }
                                else
                                {
                                    card.state = CardState.CardDrawn;
                                    manager.DisplayGameStatus("Please select a single facility you own and play a defense card type.");
                                }
                                break;
                            case GamePhase.Mitigate:
                                if (card.data.cardType == CardType.Mitigation && CheckHighlightedStations())
                                {
                                    // WORK: need to make mitigation card cancel an existing vul on station
                                    // maybe should highlight what we want to cancel or does it do it automatically?
                                    GameObject selected = GetHighlightedStation();
                                    Card selectedCard = selected.GetComponent<Card>();
                                    activeCardIDs.Add(card.UniqueID);
                                    ActiveCardList.Add(gameObjectCard);
                                    // we should play the card's effects
                                    card.Play(this, selectedCard);

                                    if (card.state == CardState.CardDiscarded)
                                    {
                                        //StackCards(selected, gameObjectCard, playerDropZone, GamePhase.Mitigate);
                                        //card.state = CardState.CardInPlay;
                                        
                                        


                                        playCount = 1;
                                    }
                                    else
                                    {
                                        // remove what we just added
                                        activeCardIDs.Remove(card.UniqueID);
                                        ActiveCardList.Remove(gameObjectCard);
                                        card.state = CardState.CardDrawn;
                                        manager.DisplayGameStatus("Please select a card that can mitigate a vulnerability card on a chosen facility.");
                                    }
                                }
                                else
                                {
                                    card.state = CardState.CardDrawn;
                                    manager.DisplayGameStatus("Please select a single opponent facility and play a vulnerability card.");
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
                        switch (phase)
                        {
                            case GamePhase.Vulnerability:
                                if (card.data.cardType == CardType.Vulnerability && manager.CheckOpponentHighlightedStations())
                                {
                                    GameObject selected = manager.GetOpponentHighlightedStation();
                                    Card selectedCard = selected.GetComponent<Card>();
                                    StackCards(selected, gameObjectCard, opponentDropZone, GamePhase.Vulnerability);
                                    card.state = CardState.CardInPlay;
                                    activeCardIDs.Add(card.UniqueID);
                                    ActiveCardList.Add(gameObjectCard);
                                    selectedCard.AttackingCards.Add(card.UniqueID);
                                    mUpdatesThisPhase.Add(new Updates
                                    {
                                        WhatToDo = AddOrRem.Add,
                                        UniqueFacilityID = selectedCard.UniqueID,
                                        CardID = card.data.cardID
                                    });

                                    // we don't play vuln effects until the attack phase
                                    playCount = 1;
                                }
                                else
                                {
                                    card.state = CardState.CardDrawn;
                                    manager.DisplayGameStatus("Please select a single opponent facility and play a vulnerability card.");
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

    public void ChangeScaleAndPosition(Vector2 scale, GameObject objToScale)
    {
        Transform parent = objToScale.transform.parent;
        slippy parentSlippy = objToScale.GetComponentInParent<slippy>();
        slippy areaSlippy = objToScale.GetComponent<slippy>();

        if (parent != null && parentSlippy != null)
        {
            objToScale.transform.SetParent(null, true);

            parentSlippy.originalScale = scale;
            parentSlippy.originalPosition = new Vector3();
            parentSlippy.ResetScale();

            if (areaSlippy != null)
            {
                areaSlippy.originalScale = scale;
                areaSlippy.originalPosition = new Vector3();
                areaSlippy.ResetScale();
            }

            objToScale.transform.SetPositionAndRotation(new Vector3(), objToScale.transform.rotation);            
        } else if (parent != null)
        {
            objToScale.transform.SetParent(null, true);

            if (areaSlippy != null)
            {
                areaSlippy.originalScale = scale;
                areaSlippy.originalPosition = new Vector3();
                areaSlippy.ResetScale();
            }

            objToScale.transform.localScale = scale;
            objToScale.transform.SetPositionAndRotation(new Vector3(), objToScale.transform.rotation);
        }
        else
        {
            if (areaSlippy != null)
            {
                areaSlippy.originalScale = scale;
                areaSlippy.originalPosition = new Vector3();
                areaSlippy.ResetScale();
            }

            // if there's no parent then our scale is THE scale
            objToScale.transform.localScale = new Vector3(scale.x, scale.y, 1.0f);
            objToScale.transform.SetPositionAndRotation(new Vector3(), objToScale.transform.rotation);
            
         
        }
    }

    public void StackCards(GameObject stationObject, GameObject addedObject, GameObject dropZone, GamePhase phase)
    {
        Card stationCard = stationObject.GetComponent<Card>();

        // unhighlight the outline if it's turned on
        stationCard.OutlineImage.SetActive(false);
        GameObject tempCanvas;

        if (stationCard.HasCanvas)
        {
            // at least one card is already played on this one!    
            tempCanvas = stationCard.CanvasHolder;
            
            ChangeScaleAndPosition(new Vector2(1.0f, 1.0f), addedObject);
            addedObject.transform.SetParent(tempCanvas.transform, false);

            // set local offset for actual stacking
            stationCard.stackNumber += 1;
            if (phase == GamePhase.Defense)
            {
                // added cards go at the back
                addedObject.transform.SetAsFirstSibling();
            }
            else if (phase == GamePhase.Vulnerability)
            {
                // added cards go at the front if they're vulnerabilities
                addedObject.transform.SetAsLastSibling();
            }
            else if (phase == GamePhase.Mitigate)
            {
                // added cards go at the front if they're vulnerabilities
                addedObject.transform.SetAsLastSibling();
            }

            addedObject.GetComponent<slippy>().enabled = false;
            addedObject.GetComponent<HoverScale>().previousScale = Vector2.one;
            addedObject.GetComponent<HoverScale>().SlippyOff = true;

        }
        else
        {
            // add a canvas component and change around the parents
            tempCanvas = Instantiate(cardStackingCanvas);
            // set defaults for canvas
            Transform parent = tempCanvas.transform.parent;
            if (parent != null)
            {
                tempCanvas.transform.SetParent(null, false);
            }
            tempCanvas.transform.localPosition = new Vector3(0.0f, 0.0f, 0.0f);
            tempCanvas.transform.localScale = new Vector3(ORIGINAL_SCALE, ORIGINAL_SCALE, 1.0f);
            
            // turn slippy off - needs to be here???
            if (addedObject.GetComponentInParent<slippy>() != null)
            {
                addedObject.GetComponentInParent<slippy>().enabled = false;
            } 
             if (stationObject.GetComponentInParent<slippy>() != null)
            {
                stationObject.GetComponentInParent<slippy>().enabled = false;
            }

            // now reset scale on all the cards under the canvas!
            // this is only necessary since they likely already have their own scale and we
            // want the canvas to now scale them
            ChangeScaleAndPosition(new Vector2(1.0f, 1.0f), stationObject);
            ChangeScaleAndPosition(new Vector2(1.0f, 1.0f), addedObject);

            // now add them to canvas
            addedObject.transform.SetParent(tempCanvas.transform, false);
            addedObject.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
            stationObject.transform.SetParent(tempCanvas.transform, false);
            
            if (phase == GamePhase.Defense)
            {
                // added cards go at the back
                addedObject.transform.SetAsFirstSibling();
            }
            else if (phase == GamePhase.Vulnerability)
            {
                // added cards go at the front if they're vulnerabilities
                addedObject.transform.SetAsLastSibling();
            } else if (phase == GamePhase.Mitigate)
            {
                // added cards go at the front if they're vulnerabilities
                addedObject.transform.SetAsLastSibling();
            }

            // make sure the station knows if has a canvas with children
            stationCard.HasCanvas = true;
            stationCard.CanvasHolder = tempCanvas;
            stationCard.stackNumber += 1;

            // reset some hoverscale info
            addedObject.GetComponent<HoverScale>().previousScale = Vector2.one;
            addedObject.GetComponent<HoverScale>().SlippyOff = true;
            stationObject.GetComponent<HoverScale>().SlippyOff = true;
            stationObject.GetComponent<HoverScale>().previousScale = Vector2.one;

            // add the canvas to the played card holder
            tempCanvas.transform.SetParent(dropZone.transform, false);
            tempCanvas.SetActive(true);

            addedObject.GetComponent<slippy>().enabled = false;
        }

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
                    
                    // DO a AABB collision test to see if the card is on the card drop
                    if (cardPosition.y < discardDropMax.y &&
                       cardPosition.y > discardDropMin.y &&
                       cardPosition.x < discardDropMax.x &&
                       cardPosition.x > discardDropMin.x)
                    {
                        //Debug.Log("card dropped in discard zone");
                        if (GameManager.instance.isDiscardAllowed)
                        {
                            // remove from player lists
                            int index = HandList.FindIndex(x => x.Equals(gameObjectCard));
                            if (index >= 0)
                            {
                                discardIndex = index;
                                //Debug.Log("discard is allowed and will be done now");
                                DiscardCards.Add(gameObjectCard);
                                DiscardIds.Add(card.data.cardID);

                                // change parent and rescale
                                card.state = CardState.CardDiscarded;
                                gameObjectCard.GetComponentInParent<slippy>().enabled = false;
                                gameObjectCard.GetComponentInParent<slippy>().ResetScale();
                                gameObjectCard.transform.SetParent(discardDropZone.transform, false);
                                gameObjectCard.transform.localPosition = new Vector3();

                                // for the future might want to stack cards in the discard zone
                                // WORK
                                gameObjectCard.SetActive(false);
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
                        //Debug.Log("card not dropped in card drop zone");
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

    public void DiscardSingleActiveCard(int uniqueFacilityID, int cardID, bool addUpdate)
    {
        int discardCount = 0;
        int discardIndex = 0;

        if (ActiveCardList.Count > 0)
        {
            foreach (GameObject gameObjectCard in ActiveCardList)
            {
                Card card = gameObjectCard.GetComponent<Card>();
                if (card.data.cardID == cardID)
                {

                    // remove from player lists
                    int index = ActiveCardList.FindIndex(x => x.Equals(gameObjectCard));
                    if (index >= 0)
                    {
                        discardIndex = index;
                        //Debug.Log("discard is allowed and will be done now");
                        DiscardCards.Add(gameObjectCard);
                        DiscardIds.Add(card.data.cardID);

                        // change parent and rescale
                        card.state = CardState.CardDiscarded;
                        gameObjectCard.GetComponentInParent<slippy>().enabled = false;
                        gameObjectCard.GetComponentInParent<slippy>().ResetScale();
                        gameObjectCard.transform.SetParent(discardDropZone.transform, false);
                        gameObjectCard.transform.localPosition = new Vector3();

                        // for the future might want to stack cards in the discard zone
                        // WORK
                        gameObjectCard.SetActive(false);
                        card.cardZone = discardDropZone;
                        discardCount++;

                        if (addUpdate)
                        {
                            mUpdatesThisPhase.Add(new Updates
                            {
                                WhatToDo = AddOrRem.Remove,
                                UniqueFacilityID = uniqueFacilityID,
                                CardID = card.data.cardID
                            });
                        }
                    }
                    break;
                }
            }

        }

        if (discardCount > 0)
        {
            // remove the discarded card
            activeCardIDs.RemoveAt(discardIndex);
            ActiveCardList.RemoveAt(discardIndex);
        }
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

    public void AddUpdate(Updates update, GameObject cardGameObject, GameObject dropZone, GamePhase phase)
    {
        GameObject facility;
        Card facilityCard = null;
        int index = -1;

        // find unique facility in facilities list
        for (int i = 0; i < ActiveFacilities.Count; i++)
        {
            facility = ActiveFacilities[i];
            facilityCard = facility.GetComponent<Card>();
            if (facilityCard.UniqueID == update.UniqueFacilityID)
            {
                index = i;
                
                break;
            }
        }

        // if we found the right facility
        if (index != -1)
        {
            //// create card to be displayed
            //Card card = DrawCard(false, update.CardID, -1, ref DeckIDs, opponentDropZone, true, ref ActiveCardList, ref activeCardIDs);
            //GameObject cardGameObject = ActiveCardList[ActiveCardList.Count - 1];
            Card card = cardGameObject.GetComponent<Card>();
            if (phase == GamePhase.Vulnerability)
            {
                Debug.Log("adding attack with card id : " + card.data.cardID);
                facilityCard.AttackingCards.Add(card.data.cardID);
            }
            cardGameObject.SetActive(false);

            // add card to its displayed cards
            StackCards(ActiveFacilities[index], cardGameObject, dropZone, phase);
            card.state = CardState.CardInPlay;
            cardGameObject.SetActive(true);
        }
        else
        {
            Debug.Log("a facility returned -1 for an opponent play - there's a bug somewhere.");
        }
    }

    public void AddUpdates(ref List<Updates> updates, GamePhase phase)
    {
        foreach (Updates update in updates)
        {
            GameObject facility;
            Card selectedCard = null;
            int index = -1;

            // find unique facility in facilities list
            for (int i = 0; i < ActiveFacilities.Count; i++)
            {
                facility = ActiveFacilities[i];
                selectedCard = facility.GetComponent<Card>();
                if (selectedCard.UniqueID == update.UniqueFacilityID)
                {
                    index = i;
                    break;
                }
            }

            // if we found the right facility
            if (index != -1 && update.WhatToDo == AddOrRem.Add)
            {
                // create card to be displayed
                Card card = DrawCard(false, update.CardID, -1, ref DeckIDs, opponentDropZone, true, ref ActiveCardList, ref activeCardIDs);
                GameObject cardGameObject = ActiveCardList[ActiveCardList.Count - 1];
                cardGameObject.SetActive(false);

                // add card to its displayed cards
                StackCards(ActiveFacilities[index], cardGameObject, opponentDropZone, GamePhase.Defense);
                card.state = CardState.CardInPlay;
                Debug.Log("opponent player updates added " + card.data.cardID + " to the active list of size " + ActiveCardList.Count);
                card.Play(this, selectedCard);
                cardGameObject.SetActive(true);
            }
            else
            if ( update.WhatToDo == AddOrRem.Remove)
            {
                Debug.Log("asked to remove card id " + update.CardID + " from opponent facility " + update.UniqueFacilityID);
                manager.DiscardOpponentActiveCard(update.UniqueFacilityID, update.CardID, false);
            }
            else
            {
                Debug.Log("a facility returned -1 for an opponent play - there's a bug somewhere.");
            }
        }
        
    }


    // and update message consists of:
    // count of updates - 1 per card
    // what game phase this is happening for
    // the list of updates in the order of: add/remove, unique facility id, card id
    public void GetUpdatesInMessageFormat(ref List<int> playsForMessage, GamePhase phase)
    {
        playsForMessage.Add(mUpdatesThisPhase.Count);
        playsForMessage.Add((int)phase);

        foreach(Updates update in mUpdatesThisPhase)
        {
            playsForMessage.Add((int)update.WhatToDo);
            playsForMessage.Add(update.UniqueFacilityID);
            playsForMessage.Add(update.CardID);
            Debug.Log("adding update to send to opponent: " + update.UniqueFacilityID + " and card id " + update.CardID);
        }

        // we've given the updates away, so let's make sure to 
        // clear the list
        mUpdatesThisPhase.Clear();
    }
}
