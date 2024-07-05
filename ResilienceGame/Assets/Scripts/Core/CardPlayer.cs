using System.Collections.Generic;
using TMPro;
using UnityEngine;
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
    public static Dictionary<int, Card> cards = new Dictionary<int,Card>();
    public List<int> FacilityIDs = new List<int>(10);
    public List<int> DeckIDs = new List<int>(52);
    public Dictionary<int, GameObject> HandCards = new Dictionary<int, GameObject>();
    public Dictionary<int,GameObject> Discards = new Dictionary<int, GameObject>();
    public Dictionary<int, GameObject> ActiveCards = new Dictionary<int, GameObject>();
    public Dictionary<int, GameObject> ActiveFacilities = new Dictionary<int, GameObject>();
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
    public string DeckName="";

    Vector2 discardDropMin;
    Vector2 discardDropMax;
    Vector2 playedDropMin;
    Vector2 playedDropMax;
    Vector2 opponentDropMin;
    Vector2 opponentDropMax;
    // the var is static to make sure the id's don't overlap between
    // multiple card players
    static int sUniqueIDCount = 0;
    int mTotalFacilityValue = 0;
    int mValueSpentOnVulnerabilities = 0;
    int mNumberDefensePhase = 0;
    public const int MAX_DEFENSE_CARDS_PER_TURN = 1;

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

    public static void AddCards(List<Card> cardList)
    {
        foreach (Card card in cardList)
        {
            cards.Add(card.data.cardID, card);
        }
    }

    public void InitializeCards()
    {
        manager = GameObject.FindObjectOfType<GameManager>();
        Debug.Log("card count is: " + cards.Count);
        foreach (Card card in cards.Values)
        {
            if (card != null)
            {
                if (card.DeckName.Equals(DeckName) && card.data.cardType != CardType.Station)
                {
                    Debug.Log("adding card " + card.name + " with id " + card.data.cardID + " to deck " + DeckName);
                    for (int j = 0; j < card.data.numberInDeck; j++)
                    {
                        DeckIDs.Add(card.data.cardID);
                    }

                }
                else if (card.DeckName.Equals(DeckName))
                {
                    Debug.Log("adding facility " + card.name + " with id " + card.data.cardID + " to deck " + DeckName);
                    for (int j = 0; j < card.data.numberInDeck; j++)
                    {
                        FacilityIDs.Add(card.data.cardID);
                    }
                }
                else
                {
                    // they don't match and it's ok.
                }

            }
        }
    }

    public virtual void DrawCards()
    {
        if (HandCards.Count < maxHandSize)
        {
            int count = HandCards.Count;
            for (int i = 0; i < maxHandSize - count; i++)
            {
                DrawCard(true, 0, -1, ref DeckIDs, handDropZone, true, ref HandCards);
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
                    ref ActiveFacilities);
                mTotalFacilityValue += card.data.worth;
            } else
            {
                // need to draw the 2 pt facility according to rules
                // at the beginning!
                for (int i = 0; i < FacilityIDs.Count; i++)
                {
                    if (cards[FacilityIDs[i]].data.worth == worth)
                    {
                        card = DrawCard(false, FacilityIDs[i], -1, ref FacilityIDs, playerDropZone, false,
                            ref ActiveFacilities);
                        mTotalFacilityValue += card.data.worth;
                        break;
                    }
                }
            }

            Debug.Log("facility was drawn and its unique id is: " + card.UniqueID + " with total worth climbing to " + mTotalFacilityValue);
            Debug.Log("total active facilities are: " + ActiveFacilities.Count);
            // always turn slippy off for facilities as we can't move them
            slippy theSlippy = card.GetComponent<slippy>();
            if (theSlippy != null)
            {
                theSlippy.enabled = false;
            }
        }

       

        return card;
    }

    public virtual Card DrawCard(bool random, int cardId, int uniqueId, ref List<int> deckToDrawFrom,
        GameObject dropZone, bool allowSlippy,
        ref Dictionary<int, GameObject> activeDeck)
    {
        int rng = -1;
        Card actualCard;
        int indexForCard = -1;

        if (random)
        {
            rng = UnityEngine.Random.Range(0, deckToDrawFrom.Count);
            if (cards.TryGetValue(deckToDrawFrom[rng], out actualCard))
            {
                Debug.Log("found proper card!");
            }
            indexForCard = rng;
        }
        else
        {
            if (!cards.TryGetValue(cardId, out actualCard))
            {
                Debug.Log("Error: handed the card deck a card id that isn't in the deck! " + cardId);
                rng = 0;
                return null;
                
            }
            indexForCard = deckToDrawFrom.FindIndex(x => x == cardId);
            if (indexForCard == -1)
            {
                Debug.Log("didn't find a card of this type to draw : " + cardId);
                return null;
            }
        }

        if (deckToDrawFrom.Count <= 0) // Check to ensure the deck is actually built before trying to draw a card
        {
            Debug.Log("no cards drawn.");
            return null;
        }

        GameObject tempCardObj = Instantiate(cardPrefab);
        Card tempCard = tempCardObj.GetComponent<Card>();
        tempCard.cardZone = dropZone ;
        tempCard.data = actualCard.data;
        if (uniqueId != -1)
        {
            tempCard.UniqueID = uniqueId;
            Debug.Log("setting unique id for facility " + uniqueId);
        } else
        {
            // since there are multiples of each card type potentially
            // in a deck they need a unique id outside of the card's id
            tempCard.UniqueID = sUniqueIDCount;
            sUniqueIDCount++;
        }

        // set the info on the card front
        CardFront front = actualCard.GetComponent<CardFront>();
        tempCard.front = front;

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
            case CardType.Vulnerability:
                tempCard.ActionList.Add(new ActionImpactFacilityWorth());
                break;
            default:
                break;
        }

        foreach(string mitigation in cards[tempCard.data.cardID].MitigatesWhatCards)
        {
            tempCard.MitigatesWhatCards.Add(mitigation);
        }

 
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
        activeDeck.Add(tempCard.UniqueID, tempCardObj);

        // remove this card so we don't draw it again
        deckToDrawFrom.RemoveAt(indexForCard);
        return tempCard;
    }

    // Update is called once per frame
    void Update()
    {
        // nothing to update at the moment
    }

    public void ResetVulnerabilityCost()
    {
        mValueSpentOnVulnerabilities = 0;
    }

    public void ResetDefenseNumber()
    {
        mNumberDefensePhase = 0;
    }

    public void HandleAttackPhase(CardPlayer opponent)
    {
        List<int> indicesToRemove = new List<int>(8);
        int index = 0;

        // for all active facilities
        foreach (GameObject facilityGameObject in ActiveFacilities.Values)
        {
            Card facilityCard = facilityGameObject.GetComponent<Card>();
            Debug.Log("number of attacks on facility: " + facilityCard.AttackingCards.Count);
            // for all attacking cards on those facilities
            foreach(CardIDInfo cardInfo in facilityCard.AttackingCards)
            {
                // run the effects of the card, but only if we roll between 11-20 on a d20 does the attack happen
                // This is the same as 50-99 on a 0-100 random roll
                int randomNumber = UnityEngine.Random.Range(0, 100);
                if (randomNumber >= 50)
                {
                    // get the card
                    GameObject opponentAttackObject = opponent.GetActiveCardObject(cardInfo);

                    // run the attack effects
                    if (opponentAttackObject != null)
                    {
                        
                        Card opponentCard = opponentAttackObject.GetComponent<Card>();
                        Debug.Log("attacking card with value : " + opponentCard.data.worth);
                        opponentCard.Play(this, opponent, facilityCard);
                        mUpdatesThisPhase.Add(new Updates
                        {
                            WhatToDo = AddOrRem.Remove,
                            UniqueFacilityID = facilityCard.UniqueID,
                            CardID = opponentCard.data.cardID
                        });
                    } else
                    {
                        Debug.Log("there's a problem because an opponent attack card wasn't in the opponent's active list.");
                    }
                }
            }

            Debug.Log("facility worth is " + (facilityCard.data.worth + facilityCard.DefenseHealth));

            // now check the total worth of the facility to see if it
            // and do a removal of all cards that were spent in attacks
            if (facilityCard.data.worth+facilityCard.DefenseHealth <= 0)
            {
                Debug.Log("we need to get rid of this facility");
                // the facility needs to be removed along with all remaining
                // attack cards on it
                foreach(CardIDInfo cardInfo in facilityCard.AttackingCards)
                {
                    GameObject cardObject = opponent.GetActiveCardObject(cardInfo);
                    Card cardToDispose = cardObject.GetComponent<Card>();
                    Debug.Log("handling all attack cards on defunct facility : this one's id is " + cardToDispose.UniqueID);
                    cardToDispose.state = CardState.CardNeedsToBeDiscarded;
                    opponent.HandleDiscard(opponent.ActiveCards, opponent.opponentDropZone, facilityCard.UniqueID, true);
                    //manager.DiscardOpponentActiveCard(facilityCard.UniqueID, cardInfo, true);
                }
                facilityCard.AttackingCards.Clear();
                facilityCard.state = CardState.CardNeedsToBeDiscarded;
                mUpdatesThisPhase.Add(new Updates
                {
                    WhatToDo = AddOrRem.Remove,
                    UniqueFacilityID = facilityCard.UniqueID,
                    CardID = facilityCard.data.cardID
                });
                indicesToRemove.Add(facilityCard.UniqueID);
            } 

            index++;

        }

        // now we can discard all the facility cards no longer active
        // start at the back of the list and then remove towards the front
        for (int i = indicesToRemove.Count-1; i>=0; i--)
        {
            Debug.Log("removing facility at key: " + indicesToRemove[i]);
            GameObject facilityObject = ActiveFacilities[indicesToRemove[i]];
            //Discards.Add(indicesToRemove[i], facilityObject);
            Card facilityCard = ActiveFacilities[indicesToRemove[i]].GetComponent<Card>();
            facilityCard.state = CardState.CardNeedsToBeDiscarded;
            HandleDiscard(ActiveFacilities, playerDropZone, facilityCard.UniqueID, false);
            //facilityCard.state = CardState.CardDiscarded;

            //// change parent and rescale
            //facilityObject.GetComponentInParent<slippy>().enabled = false;
            //facilityObject.GetComponentInParent<slippy>().ResetScale();
            //facilityObject.transform.SetParent(discardDropZone.transform, false);
            //facilityObject.transform.localPosition = new Vector3();

            //// for the future might want to stack cards in the discard zone
            //facilityObject.SetActive(false);
            //facilityCard.cardZone = discardDropZone;
            //ActiveFacilities.Remove(indicesToRemove[i]);
            
            // should send the opponent this important info!
            // WORK!!!!!!
        }


    }

    public GameObject GetActiveCardObject(CardIDInfo cardIdInfo)
    {
        GameObject cardObject = null;
        if (ActiveCards.ContainsKey(cardIdInfo.UniqueID))
        {
            cardObject = ActiveCards[cardIdInfo.UniqueID];
        } else if (HandCards.ContainsKey(cardIdInfo.UniqueID))
        {
            Debug.Log("hand cards contained the card with unique id " + cardIdInfo.UniqueID);
        }

        return cardObject;
    }

    public void DiscardAllInactiveCards()
    {
        List<int> inactives = new List<int>(10);

        foreach(GameObject activeCardObject in ActiveCards.Values)
        {
            //GameObject activeCardObject = ActiveCardList[i];
            Card card = activeCardObject.GetComponent<Card>();

            if (card.state == CardState.CardNeedsToBeDiscarded)
            {
                Discards.Add(card.UniqueID, activeCardObject);
                inactives.Add(card.UniqueID);
                card.state = CardState.CardDiscarded;

                // change parent and rescale
                activeCardObject.GetComponentInParent<HoverScale>().previousScale = Vector2.zero;
                activeCardObject.GetComponentInParent<HoverScale>().ResetScale();
                activeCardObject.GetComponentInParent<slippy>().enabled = false;
                activeCardObject.GetComponentInParent<slippy>().ResetScale();
                activeCardObject.GetComponent<HoverScale>().enabled = false;
                activeCardObject.GetComponent<slippy>().ResetScale();
                activeCardObject.GetComponent<slippy>().enabled = false;
                activeCardObject.transform.SetParent(discardDropZone.transform, false);
                activeCardObject.transform.localPosition = new Vector3();
                activeCardObject.transform.localScale = new Vector3(1,1,1);

                // for the future might want to stack cards in the discard zone
                Debug.Log("setting card to discard zone: " + card.UniqueID + " with name " + card.front.title);
                activeCardObject.SetActive(false);
                card.cardZone = discardDropZone;           
            }
        }
        foreach (int key in inactives)
        {
            Debug.Log("key being discarded is " + key);
            if (!ActiveCards.Remove(key))
            {
                Debug.Log("problem with removing key from actives : " + key);
            }
            if (HandCards.Remove(key))
            {
                Debug.Log("card was in the hand rather than active list: " + key);
            }
        }
    }
    public virtual int HandlePlayCard(GamePhase phase, CardPlayer opponentPlayer)
    {
        int playCount = 0;
        int playKey = 0;

        if (HandCards.Count != 0)
        {
            foreach (GameObject gameObjectCard in HandCards.Values)
            {
                Card card = gameObjectCard.GetComponent<Card>();
                if (card.state == CardState.CardDrawnDropped)
                {
                    Debug.Log("card dropped in cardhandle");
                    // card has been dropped somewhere - where?
                    Vector2 cardPosition = card.getDroppedPosition();
                   
                    // DO a AABB collision test to see if the card is on the player's drop
                    if (cardPosition.y < playedDropMax.y &&
                       cardPosition.y > playedDropMin.y &&
                       cardPosition.x < playedDropMax.x &&
                       cardPosition.x > playedDropMin.x)
                    {
                        Debug.Log("collision with played area");
                        switch (phase)
                        {
                            case GamePhase.Defense:
                                if (mNumberDefensePhase<MAX_DEFENSE_CARDS_PER_TURN &&  card.data.cardType==CardType.Defense && CheckHighlightedStations())
                                {
                                    GameObject selected = GetHighlightedStation();
                                    Card selectedCard = selected.GetComponent<Card>();
                                    StackCards(selected, gameObjectCard, playerDropZone, GamePhase.Defense);
                                    card.state = CardState.CardInPlay;
                                    ActiveCards.Add(card.UniqueID, gameObjectCard);
                                    
                                    selectedCard.ModifyingCards.Add(card.UniqueID);
                                    mUpdatesThisPhase.Add(new Updates
                                    {
                                        WhatToDo=AddOrRem.Add,
                                        UniqueFacilityID=selectedCard.UniqueID,
                                        CardID=card.data.cardID
                                    });

                                    // we should play the card's effects
                                    card.Play(this, opponentPlayer, selectedCard);
                                    playCount = 1;
                                    selectedCard.OutlineImage.SetActive(false);
                                    playKey = card.UniqueID;
                                    mNumberDefensePhase++;
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
                                    Debug.Log("trying to mitigate");
                                    GameObject selected = GetHighlightedStation();
                                    Card selectedCard = selected.GetComponent<Card>();

                                    ActiveCards.Add(card.UniqueID, gameObjectCard);
                                    
                                    // we should play the card's effects
                                    card.Play(this, opponentPlayer, selectedCard);

                                    if (card.state == CardState.CardNeedsToBeDiscarded)
                                    {
                                        HandleDiscard(ActiveCards, playerDropZone, selectedCard.UniqueID, false);
                                        opponentPlayer.HandleDiscard(opponentPlayer.ActiveCards, playerDropZone, selectedCard.UniqueID, true);
                                        playCount = 1;
                                        playKey = card.UniqueID;
                                        selectedCard.OutlineImage.SetActive(false);
                                    }
                                    else
                                    {
                                        // remove what we just added
                                        ActiveCards.Remove(card.UniqueID);
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
                                if (card.data.cardType == CardType.Vulnerability && opponentPlayer.CheckHighlightedStations() && 
                                    ((mValueSpentOnVulnerabilities+card.data.cost) <= mTotalFacilityValue))
                                {
                                    GameObject selected = opponentPlayer.GetHighlightedStation();
                                    Card selectedCard = selected.GetComponent<Card>();
                                    StackCards(selected, gameObjectCard, opponentDropZone, GamePhase.Vulnerability);
                                    card.state = CardState.CardInPlay;
                                    ActiveCards.Add(card.UniqueID, gameObjectCard);
                                  
                                    selectedCard.AttackingCards.Add(new CardIDInfo
                                    {
                                        CardID = card.data.cardID,
                                        UniqueID = card.UniqueID
                                    });
                                    mUpdatesThisPhase.Add(new Updates
                                    {
                                        WhatToDo = AddOrRem.Add,
                                        UniqueFacilityID = selectedCard.UniqueID,
                                        CardID = card.data.cardID
                                    });

                                    // we don't play vuln effects until the attack phase
                                    playCount = 1;
                                    playKey = card.UniqueID;
                                    selectedCard.OutlineImage.SetActive(false);
                                    mValueSpentOnVulnerabilities += card.data.cost;
                                    Debug.Log("amount spent on vuln is " + mValueSpentOnVulnerabilities + " with total facility worth of " + mTotalFacilityValue);
                                }
                                else
                                {
                                    card.state = CardState.CardDrawn;
                                    manager.DisplayGameStatus("Please select a single opponent facility and play a vulnerability card less than the total worth of your facility cards.");
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
                }             
            }
        }

        if (playCount > 0)
        {
            // remove the discarded card
            if (!HandCards.Remove(playKey))
            {
                Debug.Log("didn't find a key to remove! " + playKey);
            }
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
        if (HandCards.Count != 0)
        {
            foreach (GameObject cardGameObject in HandCards.Values)
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

        foreach (GameObject gameObject in ActiveFacilities.Values)
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

    public int HandleDiscard(Dictionary<int, GameObject> discardFromArea, GameObject previousParent, int uniqueFacilityID, bool addUpdate)
    {
        int discardCount = 0;
        int discardIndex = 0;

        if (discardFromArea.Count != 0)
        {
            foreach (GameObject gameObjectCard in discardFromArea.Values)
            {
                Card card = gameObjectCard.GetComponent<Card>();
                if (card.state == CardState.CardDrawnDropped || card.state == CardState.CardNeedsToBeDiscarded)
                {
                    Vector2 cardPosition = card.getDroppedPosition();

                    // DO a AABB collision test to see if the card is on the card drop
                    if (card.state == CardState.CardNeedsToBeDiscarded ||
                        (cardPosition.y < discardDropMax.y &&
                       cardPosition.y > discardDropMin.y &&
                       cardPosition.x < discardDropMax.x &&
                       cardPosition.x > discardDropMin.x))
                    {
                        Debug.Log("card dropped in discard zone or needs to be discarded" + card.UniqueID);

                        discardIndex = card.UniqueID;
                        //Debug.Log("discard is allowed and will be done now");
                        Discards.Add(card.UniqueID, gameObjectCard);

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
                            Debug.Log("adding update for opponent to get");
                            mUpdatesThisPhase.Add(new Updates
                            {
                                WhatToDo = AddOrRem.Remove,
                                UniqueFacilityID = uniqueFacilityID,
                                CardID = card.data.cardID
                            });
                        }
                        // only allow one discard per cycle
                        // don't think it's possible for more than one anyway
                        break;

                    }
                    else
                    {
                        //Debug.Log("card not dropped in card drop zone");
                        // If it fails, parent it back to the hand location and then set its state to be in hand and make it grabbable again
                        gameObjectCard.transform.SetParent(previousParent.transform, false);
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
            discardFromArea.Remove(discardIndex);
        }

        return discardCount;
    }

    public GameObject GetHighlightedStation() {
        GameObject station = null;

        foreach (GameObject gameObject in ActiveFacilities.Values)
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

    //public void DiscardSingleActiveCard(int uniqueFacilityID, CardIDInfo cardID, bool addUpdate)
    //{
    //    if (ActiveCards.ContainsKey(cardID.UniqueID))
    //    {
    //        Debug.Log("discard is found and should be set inactive : " + cardID.UniqueID);
    //        GameObject gameObjectCard = ActiveCards[cardID.UniqueID];
    //        Card card = gameObjectCard.GetComponent<Card>();
    //        card.state = CardState.CardDiscarded;
    //        gameObjectCard.GetComponentInParent<slippy>().enabled = false;
    //        gameObjectCard.GetComponentInParent<slippy>().ResetScale();
    //        gameObjectCard.transform.SetParent(discardDropZone.transform, false);
    //        gameObjectCard.transform.localPosition = new Vector3();
    //        gameObjectCard.SetActive(false);
    //        card.cardZone = discardDropZone;

    //        Discards.Add(card.UniqueID, gameObjectCard);
    //        if (addUpdate)
    //        {
    //            mUpdatesThisPhase.Add(new Updates
    //            {
    //                WhatToDo = AddOrRem.Remove,
    //                UniqueFacilityID = uniqueFacilityID,
    //                CardID = card.data.cardID
    //            });
    //        }
    //        ActiveCards.Remove(cardID.UniqueID);
    //    }
    //    else if (ActiveFacilities.ContainsKey(cardID.UniqueID))
    //    {
    //        GameObject gameObjectCard = ActiveCards[cardID.UniqueID];
    //        Card card = gameObjectCard.GetComponent<Card>();
    //        card.state = CardState.CardDiscarded;
    //        gameObjectCard.GetComponentInParent<slippy>().enabled = false;
    //        gameObjectCard.GetComponentInParent<slippy>().ResetScale();
    //        gameObjectCard.transform.SetParent(discardDropZone.transform, false);
    //        gameObjectCard.transform.localPosition = new Vector3();
    //        gameObjectCard.SetActive(false);
    //        card.cardZone = discardDropZone;

    //        Discards.Add(card.UniqueID, gameObjectCard);
    //        if (addUpdate)
    //        {
    //            mUpdatesThisPhase.Add(new Updates
    //            {
    //                WhatToDo = AddOrRem.Remove,
    //                UniqueFacilityID = uniqueFacilityID,
    //                CardID = card.data.cardID
    //            });
    //        }
    //        ActiveFacilities.Remove(cardID.UniqueID);
    //    }
    //}

    public bool CheckForCardsOfType(CardType cardType, Dictionary<int, GameObject> listToCheck)
    {
        bool hasCardType = false;

        foreach(GameObject gameObject in listToCheck.Values)
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

    public void AddUpdate(Updates update, GameObject cardGameObject, GameObject dropZone, GamePhase phase, bool getRidOfFacility)
    {
        GameObject facility=null;
        Card facilityCard = null;

        // find unique facility in facilities list
        if (ActiveFacilities.TryGetValue(update.UniqueFacilityID, out facility)) {
            facilityCard = facility.GetComponent<Card>();
            // if we found the right facility
            if (cardGameObject != null && update.WhatToDo == AddOrRem.Add)
            {
                Debug.Log("card add called with phase " + phase);
                // create card to be displayed
                Card card = cardGameObject.GetComponent<Card>();
                if (phase == GamePhase.Vulnerability)
                {
                    Debug.Log("adding attack with card id : " + card.data.cardID);
                    facilityCard.AttackingCards.Add(new CardIDInfo {
                        CardID = card.data.cardID,
                        UniqueID = card.UniqueID
                    });
                    cardGameObject.SetActive(false);

                    // add card to its displayed cards
                    StackCards(facility, cardGameObject, dropZone, phase);
                    card.state = CardState.CardInPlay;
                    cardGameObject.SetActive(true);
                }

            }
            else if (update.WhatToDo == AddOrRem.Remove)
            {
                if (phase == GamePhase.Mitigate || phase == GamePhase.Attack)
                {
                    if (!getRidOfFacility)
                    {
                        Debug.Log("removing attack  for mitigation with card id " + update.CardID);
                        int cardIndex = facilityCard.AttackingCards.FindIndex(x => x.CardID == update.CardID);

                        if (cardIndex != -1)
                        {
                            CardIDInfo cardInfo = facilityCard.AttackingCards[cardIndex];
                            Debug.Log("facilities attacking cards contained the unique card info " + cardInfo.CardID + " with unique id " + cardInfo.UniqueID);
                            // discard it
                            GameObject cardObject = manager.actualPlayer.GetActiveCardObject(cardInfo);
                            if (cardObject != null)
                            {
                                Card discardCard = cardObject.GetComponent<Card>();
                                discardCard.state = CardState.CardNeedsToBeDiscarded;
                                manager.actualPlayer.HandleDiscard(manager.actualPlayer.HandCards, manager.actualPlayer.playerDropZone,
                                    facilityCard.UniqueID, false);
                                manager.actualPlayer.HandleDiscard(manager.actualPlayer.ActiveCards, manager.actualPlayer.playerDropZone,
                                facilityCard.UniqueID, false);

                            } else
                            {
                                Debug.Log("an attack card couldn't be found in the hand at 1139 in CardPlayer. " + cardInfo);
                            }
                            
                            //manager.actualPlayer.DiscardSingleActiveCard(facilityCard.UniqueID, cardInfo, false);
                            // remove the card info from the facility
                            facilityCard.AttackingCards.RemoveAt(cardIndex);
                        }
                    }
                    else
                    {
                        // now we can discard all the facility cards no longer active
                        // start at the back of the list and then remove towards the front

                        Debug.Log("removing facility ");
                        facilityCard.state = CardState.CardNeedsToBeDiscarded;
                        HandleDiscard(ActiveFacilities, dropZone, facilityCard.UniqueID, false);
                        //Discards.Add(facilityCard.UniqueID, facility);

                        //facilityCard.state = CardState.CardDiscarded;

                        //// change parent and rescale
                        //facility.GetComponentInParent<slippy>().enabled = false;
                        //facility.GetComponentInParent<slippy>().ResetScale();
                        //facility.transform.SetParent(discardDropZone.transform, false);
                        //facility.transform.localPosition = new Vector3();

                        //// for the future might want to stack cards in the discard zone
                        //facility.SetActive(false);
                        //facilityCard.cardZone = discardDropZone;
                        //ActiveFacilities.Remove(facilityCard.UniqueID);
                    }
                }
            }
        }
        else
        {
            Debug.Log("a facility wasn't found for an opponent play - there's a bug somewhere OR the facility just ran out of points and got nixed.");
        }
    }

    public void AddUpdates(ref List<Updates> updates, GamePhase phase, CardPlayer opponent )
    {
        foreach (Updates update in updates)
        {
            GameObject facility;
            Card selectedCard = null;
            int index = -1;
            Debug.Log("number of active facilities are " + ActiveFacilities.Count);

            // find unique facility in facilities list
            if (ActiveFacilities.TryGetValue(update.UniqueFacilityID, out facility))
            {
                selectedCard = facility.GetComponent<Card>();

                // if we found the right facility
                if (update.WhatToDo == AddOrRem.Add)
                {
                    // create card to be displayed
                    Card card = DrawCard(false, update.CardID, -1, ref DeckIDs, opponentDropZone, true, ref ActiveCards);
                    GameObject cardGameObject = ActiveCards[card.UniqueID];
                    cardGameObject.SetActive(false);

                    // add card to its displayed cards
                    StackCards(facility, cardGameObject, opponentDropZone, GamePhase.Defense);
                    card.state = CardState.CardInPlay;
                    Debug.Log("opponent player updates added " + card.data.cardID + " to the active list of size " + ActiveCards.Count);
                    card.Play(this, opponent, selectedCard);
                    cardGameObject.SetActive(true);
                }

            }
            else
            {
                Debug.Log("a facility was not found for an opponent play - there's a bug somewhere.");
            }
        }
        
    }

    public bool HasUpdates()
    {
        return (mUpdatesThisPhase.Count != 0);
    }

    // an update message consists of:
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
            //Debug.Log("adding update to send to opponent: " + update.UniqueFacilityID + " and card id " + update.CardID + " for phase " + phase);
        }

        // we've given the updates away, so let's make sure to 
        // clear the list
        mUpdatesThisPhase.Clear();
    }

}
