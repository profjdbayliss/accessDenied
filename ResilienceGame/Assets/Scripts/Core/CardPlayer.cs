using System.Collections.Generic;
using System.ComponentModel;
using System.Xml;
using TMPro;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.UI;
using static UnityEngine.PlayerLoop.PreUpdate;
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

public struct AttackUpdate
{
    public int UniqueFacilityID;
    public int ChangeInValue;
};

public struct FacilityConnectionInfo
{
    public int UniqueFacilityID;
    public int WhichFacilityZone;
}

public enum DiscardFromWhere
{
    Hand,
    MyPlayZone,
    MyFacility
};

public class CardPlayer : MonoBehaviour
{
    // Establish necessary fields
    public PlayerType playerType = PlayerType.Energy;
    public GameManager manager;
    public static Dictionary<int, ReadInCardData> cards = new Dictionary<int, ReadInCardData>();
    public List<int> FacilityIDs = new List<int>(10);
    public List<int> DeckIDs = new List<int>(52);
    public Dictionary<int, GameObject> HandCards = new Dictionary<int, GameObject>();
    public Dictionary<int, GameObject> Discards = new Dictionary<int, GameObject>();
    public Dictionary<int, GameObject> ActiveCards = new Dictionary<int, GameObject>();
    public Dictionary<int, GameObject> ActiveFacilities = new Dictionary<int, GameObject>();
    public int handSize;
    public int maxHandSize = 5;
    public GameObject cardPrefab;
    public GameObject discardDropZone;
    public GameObject handDropZone;
    public GameObject opponentDropZone;
    public GameObject playerDropZone;
    public GameObject cardStackingCanvas;
    public List<GameObject> AllFacilityLocations;
    int mFacilityNumber = 0;
    public string DeckName = "";

    [Header("Card Positioning")]
    public readonly float ORIGINAL_SCALE = 0.2f;
    protected HandPositioner handPositioner;



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
    int mFinalScore = 0;
    List<Updates> mUpdatesThisPhase = new List<Updates>(5);
    List<AttackUpdate> mAttackUpdates = new List<AttackUpdate>(5);
    int mMaxFacilities = 0;
    int mDrawnFacilityZone = -1;
    List<int> mNewConnectionUniqueIDs = new List<int>();
    bool mNewFacilityConnected = false;
    bool mAllFacilitiesDrawn = false;

    public void Start()
    {

        if (handDropZone)
            handPositioner = handDropZone.GetComponent<HandPositioner>();
        else
        {
            Debug.LogError("Hand drop zone not found");
        }

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

    public static void AddCards(List<ReadInCardData> cardList)
    {
        foreach (ReadInCardData card in cardList)
        {
            cards.Add(card.data.cardID, card);
        }
    }

    public void InitializeCards()
    {
        DeckIDs.Clear();
        FacilityIDs.Clear();
        manager = GameObject.FindObjectOfType<GameManager>();
        Debug.Log("card count is: " + cards.Count);
        foreach (ReadInCardData card in cards.Values)
        {
            if (card != null)
            {
                if (card.DeckName.Equals(DeckName) && card.data.cardType != CardType.Station)
                {
                    Debug.Log("adding card " + card.front.title + " with id " + card.data.cardID + " to deck " + DeckName);
                    for (int j = 0; j < card.data.numberInDeck; j++)
                    {
                        DeckIDs.Add(card.data.cardID);
                    }

                }
                else if (card.DeckName.Equals(DeckName))
                {
                    Debug.Log("adding facility " + card.front.title + " with id " + card.data.cardID + " to deck " + DeckName);
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

        mMaxFacilities = FacilityIDs.Count;

    }

    public virtual void DrawCards()
    {
        if (HandCards.Count < maxHandSize)
        {
            int count = HandCards.Count;
            for (int i = 0; i < maxHandSize - count; i++)
            {
                if (DeckIDs.Count > 0)
                {
                    DrawCard(true, 0, -1, ref DeckIDs, handDropZone, true, ref HandCards);
                } else
                {
                    break;
                }
            }
        }
    }

    public virtual Card DrawFacility(bool isRandom, int facilityID, int uniqueID, int worth)
    {
        Card card = null;
        if (FacilityIDs.Count > 0)
        {
            if (isRandom && uniqueID == -1)
            {

                if (mFacilityNumber < AllFacilityLocations.Count)
                {
                    card = DrawCard(true, 0, -1, ref FacilityIDs, AllFacilityLocations[mFacilityNumber], false,
                   ref ActiveFacilities);
                    mTotalFacilityValue += card.data.data.worth;
                    card.HasCanvas = true;
                    card.CanvasHolder = AllFacilityLocations[mFacilityNumber];
                    card.WhichFacilityZone = mFacilityNumber;
                    mFacilityNumber++;
                }
            } else if (uniqueID != -1)
            {
                if (mFacilityNumber < AllFacilityLocations.Count)
                {
                    card = DrawCard(false, facilityID, uniqueID, ref FacilityIDs, AllFacilityLocations[mFacilityNumber],
           false, ref ActiveFacilities);
                    mTotalFacilityValue += card.data.data.worth;
                    card.HasCanvas = true;
                    card.CanvasHolder = AllFacilityLocations[mFacilityNumber];
                    card.WhichFacilityZone = mFacilityNumber;
                    mFacilityNumber++;
                }
            } else
            {
                // need to draw the 2 pt facility according to rules
                // at the beginning!
                for (int i = 0; i < FacilityIDs.Count; i++)
                {
                    if (cards[FacilityIDs[i]].data.worth == worth)
                    {

                        if (mFacilityNumber < AllFacilityLocations.Count)
                        {
                            card = DrawCard(false, FacilityIDs[i], -1, ref FacilityIDs, AllFacilityLocations[mFacilityNumber], false,
                           ref ActiveFacilities);
                            card.HasCanvas = true;
                            card.CanvasHolder = AllFacilityLocations[mFacilityNumber];
                            card.WhichFacilityZone = mFacilityNumber;
                            mFacilityNumber++;
                            mTotalFacilityValue += card.data.data.worth;
                        }

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

            // whenever we draw a new facility it's not yet connected
            mNewFacilityConnected = false;
        } else
        {
            mAllFacilitiesDrawn = true;
        }



        return card;
    }

    public virtual Card DrawCard(bool random, int cardId, int uniqueId, ref List<int> deckToDrawFrom,
        GameObject dropZone, bool allowSlippy,
        ref Dictionary<int, GameObject> activeDeck)
    {
        int rng = -1;
        ReadInCardData actualCard;
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
                Debug.Log("didn't find a card of this type to draw : " + cardId + " to card deck " + DeckName + " with number " + deckToDrawFrom.Count);
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
        tempCard.cardZone = dropZone;
        tempCard.data = actualCard;
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
        GameObject background = default;
        GameObject attack=default;
        GameObject defend = default;
        RawImage[] tempRaws = tempCardObj.GetComponentsInChildren<RawImage>();
        for (int i = 0; i < tempRaws.Length; i++)
        {
            if (tempRaws[i].name == "Image")
            {
                tempRaws[i].texture = tempCard.data.front.img;
            }
            else 
            if (tempRaws[i].name == "Background")
            {
                tempRaws[i].color = tempCard.data.front.titleColor;
                background = tempRaws[i].gameObject;
                Debug.Log("background hit");
            }
        }

        Image[] tempImage = tempCardObj.GetComponentsInChildren<Image>();
        for (int i = 0; i < tempImage.Length; i++)
        {
            if (tempImage[i].name.Equals("LeftCardSlot"))
            {
                if (tempCard.data.front.worthCircle)
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
                if (tempCard.data.front.costCircle)
                {
                    // enable circle
                    tempImage[i].enabled = true;
                }
                else
                {
                    tempImage[i].enabled = false;
                }
            }
            else
            if (tempImage[i].name == "AttackBackground")
            {
                Debug.Log("attack hit");
                tempImage[i].color = tempCard.data.front.titleColor;
                attack = tempImage[i].gameObject;
            }
            else
                if (tempImage[i].name == "DefenseBackground")
            {
                tempImage[i].color = tempCard.data.front.titleColor;
                defend = tempImage[i].gameObject;
            }
        }

        // make sure the correct background image is enabled for this card
        if (tempCard.data.data.cardType == CardType.Defense)
        {
            attack.SetActive(false);
            defend.SetActive(true);
            background.SetActive(false);
        }
        else
        if (tempCard.data.data.cardType == CardType.Vulnerability)
        {
            attack.SetActive(true);
            defend.SetActive(false);
            background.SetActive(false);
        }
        else
        {
            attack.SetActive(false);
            defend.SetActive(false);
            background.SetActive(true);
        }

        TextMeshProUGUI[] tempTexts = tempCardObj.GetComponentsInChildren<TextMeshProUGUI>(true);
        for (int i = 0; i < tempTexts.Length; i++)
        {
            if (tempTexts[i].name.Equals("Title Text"))
            {
                tempTexts[i].text = tempCard.data.front.title;
            }
            else if (tempTexts[i].name.Equals("Description Text"))
            {
                tempTexts[i].text = tempCard.data.front.description;
            }
            else if (tempTexts[i].name.Equals("LeftCardNumber"))
            {
                if (tempCard.data.front.worthCircle)
                {
                    // set the text number for worth
                    tempTexts[i].enabled = true;
                    tempTexts[i].text = tempCard.data.data.worth + "";
                }
                else
                {
                    // turn off the text box
                    tempTexts[i].enabled = false;
                }

            }
            else if (tempTexts[i].name.Equals("RightCardNumber"))
            {
                if (tempCard.data.front.costCircle)
                {
                    // set the text number for cost
                    tempTexts[i].enabled = true;
                    tempTexts[i].text = tempCard.data.data.cost + "";
                }
                else
                {
                    // turn off the text box
                    tempTexts[i].enabled = false;
                }
            }
        }

        HoverScale tempScale = tempCardObj.GetComponent<HoverScale>();
        if (tempScale != null)
        {
            tempScale.targetObject = tempCardObj;
        }
        else
        {
            Debug.Log("card couldn't be assigned as hover doesn't exist!");
        }
        tempCardObj.GetComponent<slippy>().DraggableObject = tempCardObj;
        if (!allowSlippy)
        {
            slippy tempSlippy = tempCardObj.GetComponent<slippy>();
            tempSlippy.enabled = false;
        }
        tempCard.state = CardState.CardDrawn;
        if (dropZone.Equals(handDropZone))
        {
            handPositioner.HandleNewCard(tempCard);
        }      
        Vector3 tempPos = tempCardObj.transform.position;
        tempCardObj.transform.position = tempPos;
        tempCardObj.transform.SetParent(dropZone.transform, false);
        if (dropZone.Equals(handDropZone))
        {
            tempCardObj.transform.localScale = new Vector3(0.2f, 0.2f, 1.0f);
        }
        Vector3 tempPos2 = dropZone.transform.position;
        handSize++;
        tempCardObj.transform.position = tempPos2;
        tempCardObj.SetActive(true);
        if (!activeDeck.TryAdd(tempCard.UniqueID, tempCardObj))
        {
            Debug.Log("number of cards in draw active deck are: " + activeDeck.Count);
            foreach (GameObject gameObject in activeDeck.Values)
            {
                Card card = gameObject.GetComponent<Card>();
                Debug.Log("active deck value: " + card.UniqueID);
            }
        }


        // remove this card so we don't draw it again
        deckToDrawFrom.RemoveAt(indexForCard);
        Debug.Log("ending draw function");
        return tempCard;
    }

    public void ResetVulnerabilityCost()
    {
        mValueSpentOnVulnerabilities = 0;
    }

    void RemoveExtraLateralMovementCards(CardPlayer opponent)
    {
        // for all active facilities
        foreach (GameObject facilityGameObject in ActiveFacilities.Values)
        {
            Card facilityCard = facilityGameObject.GetComponent<Card>();

            // for all attacking cards on those facilities
            for (int i = 0; i < facilityCard.AttackingCards.Count; i++)
            {
                CardIDInfo cardInfo = facilityCard.AttackingCards[i];
                // get the card
                GameObject opponentAttackObject = opponent.GetActiveCardObject(cardInfo);
                // run the attack effects
                if (opponentAttackObject != null)
                {

                    Card opponentCard = opponentAttackObject.GetComponent<Card>();

                    // set all lateral movement cards in slot 0 to discard
                    if (i == 0 && opponentCard.data.data.cardType == CardType.LateralMovement)
                    {
                        opponentCard.state = CardState.CardNeedsToBeDiscarded;
                        mUpdatesThisPhase.Add(new Updates
                        {
                            WhatToDo = AddOrRem.Remove,
                            UniqueFacilityID = facilityCard.UniqueID,
                            CardID = opponentCard.data.data.cardID
                        });
                    } else if (i == facilityCard.AttackingCards.Count - 1 && opponentCard.data.data.cardType == CardType.LateralMovement)
                    {
                        opponentCard.state = CardState.CardNeedsToBeDiscarded;
                        mUpdatesThisPhase.Add(new Updates
                        {
                            WhatToDo = AddOrRem.Remove,
                            UniqueFacilityID = facilityCard.UniqueID,
                            CardID = opponentCard.data.data.cardID
                        });
                    }
                }
            }
        }
    }

    public void AddAttackUpdateToList(AttackUpdate update)
    {
        mAttackUpdates.Add(update);
    }


    public void HandleAttackPhase(CardPlayer opponent)
    {
        List<int> facilitiesToRemove = new List<int>(8);
        bool lateralMovementUsed = false;

        RemoveExtraLateralMovementCards(opponent);

        // for all active facilities
        foreach (GameObject facilityGameObject in ActiveFacilities.Values)
        {
            Card facilityCard = facilityGameObject.GetComponent<Card>();
            Debug.Log("number of attacks on facility: " + facilityCard.AttackingCards.Count);
            // for all attacking cards on those facilities
            for (int i = 0; i < facilityCard.AttackingCards.Count; i++)
            {
                CardIDInfo cardInfo = facilityCard.AttackingCards[i];
                // get the card
                GameObject opponentAttackObject = opponent.GetActiveCardObject(cardInfo);
                // run the attack effects
                if (opponentAttackObject != null)
                {

                    Card opponentCard = opponentAttackObject.GetComponent<Card>();

                    if (!(opponentCard.data.data.cardType == CardType.LateralMovement) && opponentCard.state != CardState.CardNeedsToBeDiscarded)
                    {
                        // run the effects of the card, but only if we roll between 11-20 on a d20 does the attack happen
                        // This is the same as 50-99 on a 0-100 random roll
                        int randomNumber = UnityEngine.Random.Range(0, 100);
                        if (randomNumber >= 50)
                        {
                            Debug.Log("attacking card with value : " + opponentCard.data.data.worth);
                            opponentCard.Play(this, opponent, facilityCard);

                            mUpdatesThisPhase.Add(new Updates
                            {
                                WhatToDo = AddOrRem.Remove,
                                UniqueFacilityID = facilityCard.UniqueID,
                                CardID = opponentCard.data.data.cardID
                            });

                            if (i + 1 <= facilityCard.AttackingCards.Count - 1)
                            {
                                // check to see if the next card is for lateral movement
                                Debug.Log("checking next card for lateral movement");
                                CardIDInfo cardInfoNext = facilityCard.AttackingCards[i + 1];
                                Debug.Log("card i+1 obtained");
                                // get the card
                                GameObject opponentAttackObject2 = opponent.GetActiveCardObject(cardInfoNext);
                                if (opponentAttackObject2 != null)
                                {
                                    Card possibleLateralMovement = opponentAttackObject2.GetComponent<Card>();
                                    if (possibleLateralMovement.data.data.cardType == CardType.LateralMovement)
                                    {
                                        // we need to play the lateral movement card too
                                        possibleLateralMovement.Play(this, opponent, facilityCard, opponentCard);
                                        possibleLateralMovement.state = CardState.CardNeedsToBeDiscarded;
                                        lateralMovementUsed = true;
                                        mUpdatesThisPhase.Add(new Updates
                                        {
                                            WhatToDo = AddOrRem.Remove,
                                            UniqueFacilityID = facilityCard.UniqueID,
                                            CardID = possibleLateralMovement.data.data.cardID
                                        });
                                    }
                                }
                            }

                        }
                        else
                        {
                            Debug.Log("there's a problem because an opponent attack card wasn't in the opponent's active list.");
                        }
                    }
                }
            }

            Debug.Log("facility worth is " + (facilityCard.data.data.worth + facilityCard.DefenseHealth));

            // now check the total worth of the facility to see if it
            // and do a removal of all cards that were spent in attacks
            if (facilityCard.data.data.worth + facilityCard.DefenseHealth <= 0)
            {
                Debug.Log("we need to get rid of this facility");

                // remove the worth of this facility since it's no longer there
                mTotalFacilityValue -= facilityCard.data.data.worth;
                // get rid of all potential connectors from both sides
                int whichFacilityZone = facilityCard.WhichFacilityZone;
                GameObject facilityZoneHolder = facilityCard.CanvasHolder;
                Connections connections = facilityZoneHolder.GetComponent<Connections>();

                foreach (FacilityConnectionInfo connectionInfo in facilityCard.ConnectionList)
                {
                    int zoneToTurnOff = connectionInfo.WhichFacilityZone;
                    // turn off this facility's possible connection to the other
                    Debug.Log("turning off a specific zone " + zoneToTurnOff);
                    connections.connections[zoneToTurnOff].SetActive(false);
                    Debug.Log("turned off");
                    // turn off the other facility's possible connection to this one
                    // and delete this facility from its active connections list
                    Debug.Log("getting facility with id " + connectionInfo.UniqueFacilityID);

                    GameObject otherFacility;
                    ActiveFacilities.TryGetValue(connectionInfo.UniqueFacilityID, out otherFacility);
                    Debug.Log("got facility");

                    if (otherFacility != null)
                    {
                        Card otherFacilityCard = otherFacility.GetComponent<Card>();
                        GameObject otherFacilityZone = otherFacilityCard.CanvasHolder;
                        Connections otherConnections = otherFacilityZone.GetComponent<Connections>();
                        Debug.Log("other connection zone setting to false");
                        otherConnections.connections[whichFacilityZone].SetActive(false);
                        Debug.Log("done");
                        int indexToRemove = otherFacilityCard.ConnectionList.FindIndex(x => x.WhichFacilityZone == whichFacilityZone);
                        if (indexToRemove >= 0)
                        {
                            otherFacilityCard.ConnectionList.RemoveAt(indexToRemove);
                        } else
                        {
                            Debug.Log("Error finding facility for zone " + whichFacilityZone + " to remove.");
                        }

                    } else
                    {
                        Debug.Log("Error finding facility " + connectionInfo.UniqueFacilityID + " to turn its connection off");
                    }
                }
                // now clear all the connection info
                facilityCard.ConnectionList.Clear();

                // the facility needs to be removed along with all remaining
                // attack cards on it
                foreach (CardIDInfo cardInfo in facilityCard.AttackingCards)
                {
                    GameObject cardObject = opponent.GetActiveCardObject(cardInfo);
                    if (cardObject != null)
                    {
                        Card cardToDispose = cardObject.GetComponent<Card>();
                        Debug.Log("handling all attack cards on defunct facility : this one's id is " + cardToDispose.UniqueID);
                        cardToDispose.state = CardState.CardNeedsToBeDiscarded;

                    } else
                    {
                        Debug.Log("attack card with id " + cardInfo.CardID + " wasn't found in the pile of cards on a defunct facility.");
                    }
                    //opponent.HandleDiscard(opponent.ActiveCards, opponent.opponentDropZone, facilityCard.UniqueID, true);
                }

                // get rid of defense cards on this facility as well
                foreach (GameObject cardObject in ActiveCards.Values)
                {
                    Card card = cardObject.GetComponent<Card>();
                    if (card.WhichFacilityZone == facilityCard.WhichFacilityZone)
                    {
                        Debug.Log("getting rid of defense facility on zone " + card.WhichFacilityZone);
                        card.state = CardState.CardNeedsToBeDiscarded;
                        // note that we don't add an update as the opponent's side will delete all cards with the facility card
                    }
                }

                // let's discard all the cards on the facility in question
                opponent.DiscardAllInactiveCards(DiscardFromWhere.MyPlayZone, true, facilityCard.UniqueID);
                facilityCard.AttackingCards.Clear();
                facilityCard.state = CardState.CardNeedsToBeDiscarded;

                mUpdatesThisPhase.Add(new Updates
                {
                    WhatToDo = AddOrRem.Remove,
                    UniqueFacilityID = facilityCard.UniqueID,
                    CardID = facilityCard.data.data.cardID
                });

            }

        }

        if (lateralMovementUsed)
        {
            Debug.Log("lateral movement was used!");
            // NOTE: because of lateral movement we need to check and make sure all 
            // facilities are still alive again
            // for all active facilities
            foreach (GameObject facilityGameObject in ActiveFacilities.Values)
            {
                Card facilityCard = facilityGameObject.GetComponent<Card>();

                Debug.Log("facility worth second time around is " + (facilityCard.data.data.worth + facilityCard.DefenseHealth));

                // now check the total worth of the facility to see if it
                // and do a removal of all cards that were spent in attacks
                if ((facilityCard.data.data.worth + facilityCard.DefenseHealth <= 0) &&
                    (facilityCard.state != CardState.CardNeedsToBeDiscarded))
                {
                    Debug.Log("we need to get rid of this facility");
                    // remove the worth of this facility since it's no longer there
                    mTotalFacilityValue -= facilityCard.data.data.worth;

                    // get rid of all potential connectors from both sides
                    int whichFacilityZone = facilityCard.WhichFacilityZone;
                    GameObject facilityZoneHolder = facilityCard.CanvasHolder;
                    Connections connections = facilityZoneHolder.GetComponent<Connections>();

                    foreach (FacilityConnectionInfo connectionInfo in facilityCard.ConnectionList)
                    {
                        int zoneToTurnOff = connectionInfo.WhichFacilityZone;
                        // turn off this facility's possible connection to the other
                        connections.connections[zoneToTurnOff].SetActive(false);

                        // turn off the other facility's possible connection to this one
                        // and delete this facility from its active connections list
                        GameObject otherFacility = ActiveFacilities[connectionInfo.UniqueFacilityID];
                        if (otherFacility != null)
                        {
                            Card otherFacilityCard = otherFacility.GetComponent<Card>();
                            GameObject otherFacilityZone = otherFacilityCard.CanvasHolder;
                            Connections otherConnections = otherFacilityZone.GetComponent<Connections>();
                            otherConnections.connections[whichFacilityZone].SetActive(false);
                            int indexToRemove = otherFacilityCard.ConnectionList.FindIndex(x => x.WhichFacilityZone == whichFacilityZone);
                            if (indexToRemove >= 0)
                            {
                                otherFacilityCard.ConnectionList.RemoveAt(indexToRemove);
                            }
                            else
                            {
                                Debug.Log("Error finding facility for zone " + whichFacilityZone + " to remove.");
                            }

                        }
                        else
                        {
                            Debug.Log("Error finding facility " + connectionInfo.UniqueFacilityID + " to turn its connection off");
                        }
                    }

                    Debug.Log("second for loop for getting rid of facilities done");
                    // now clear all the connection info
                    facilityCard.ConnectionList.Clear();

                    // the facility needs to be removed along with all remaining
                    // attack cards on it
                    foreach (CardIDInfo cardInfo in facilityCard.AttackingCards)
                    {
                        GameObject cardObject = opponent.GetActiveCardObject(cardInfo);
                        if (cardObject != null)
                        {
                            Card cardToDispose = cardObject.GetComponent<Card>();
                            Debug.Log("handling all attack cards on defunct facility : this one's id is " + cardToDispose.UniqueID);
                            cardToDispose.state = CardState.CardNeedsToBeDiscarded;

                        }
                        else
                        {
                            Debug.Log("attack card with id " + cardInfo.CardID + " wasn't found in the pile of cards on a defunct facility.");
                        }
                        //opponent.HandleDiscard(opponent.ActiveCards, opponent.opponentDropZone, facilityCard.UniqueID, true);
                    }

                    // get rid of defense cards on this facility as well
                    foreach (GameObject cardObject in ActiveCards.Values)
                    {
                        Card card = cardObject.GetComponent<Card>();
                        if (card.WhichFacilityZone == facilityCard.WhichFacilityZone)
                        {
                            Debug.Log("getting rid of defense facility on zone " + card.WhichFacilityZone);
                            card.state = CardState.CardNeedsToBeDiscarded;
                        }
                    }

                    // let's discard all the cards on the facility in question
                    opponent.DiscardAllInactiveCards(DiscardFromWhere.MyPlayZone, true, facilityCard.UniqueID);
                    facilityCard.AttackingCards.Clear();
                    facilityCard.state = CardState.CardNeedsToBeDiscarded;

                    mUpdatesThisPhase.Add(new Updates
                    {
                        WhatToDo = AddOrRem.Remove,
                        UniqueFacilityID = facilityCard.UniqueID,
                        CardID = facilityCard.data.data.cardID
                    });

                }

            }
        }

        // now discard all facilities annihilated
        DiscardAllInactiveCards(DiscardFromWhere.MyFacility, false, -1);
        DiscardAllInactiveCards(DiscardFromWhere.MyPlayZone, false, -1);

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

    public void DiscardAllInactiveCards(DiscardFromWhere where, bool addUpdate, int uniqueFacilityID)
    {
        List<int> inactives = new List<int>(10);
        Dictionary<int, GameObject> discardFromArea;

        switch (where)
        {
            case DiscardFromWhere.Hand:
                discardFromArea = HandCards;
                break;
            case DiscardFromWhere.MyPlayZone:
                discardFromArea = ActiveCards;
                break;
            case DiscardFromWhere.MyFacility:
                discardFromArea = ActiveFacilities;
                break;
            default:
                discardFromArea = HandCards;
                break;
        }

        foreach (GameObject activeCardObject in discardFromArea.Values)
        {
            //GameObject activeCardObject = ActiveCardList[i];
            Card card = activeCardObject.GetComponent<Card>();

            if (card.state == CardState.CardNeedsToBeDiscarded)
            {
                // it's possible we just tried to put the discard in the pile
                // so don't add it to discards twice
                if (Discards.TryAdd(card.UniqueID, activeCardObject))
                {
                    inactives.Add(card.UniqueID);
                    card.state = CardState.CardDiscarded;
                    if (where == DiscardFromWhere.Hand)
                    {
                        handPositioner.DiscardCard(activeCardObject);
                    }
                    if (addUpdate)
                    {
                        Debug.Log("adding update for opponent to get");
                        mUpdatesThisPhase.Add(new Updates
                        {
                            WhatToDo = AddOrRem.Remove,
                            UniqueFacilityID = uniqueFacilityID,
                            CardID = card.data.data.cardID
                        });
                    }
                }
                else
                {
                    Debug.Log("adding to discard pile failed. Card unique id of " + card.UniqueID + " was already in it.");
                }
                // change parent and rescale
                HoverScale hoverScale = activeCardObject.GetComponentInParent<HoverScale>();
                if (hoverScale != null)
                {
                    hoverScale.previousScale = Vector2.zero;
                    hoverScale.ResetScale();
                }

                slippy slippyObject = activeCardObject.GetComponentInParent<slippy>();
                if (slippyObject != null)
                {
                    slippyObject.enabled = false;
                    slippyObject.ResetScale();
                }

                //hoverScale = activeCardObject.GetComponent<HoverScale>();
                //if (hoverScale != null)
                //{
                //    hoverScale.enabled = false;
                //}

                slippyObject = activeCardObject.GetComponent<slippy>();
                if (slippyObject != null)
                {
                    slippyObject.ResetScale();
                    slippyObject.enabled = false;
                }

                activeCardObject.transform.SetParent(discardDropZone.transform, false);
                activeCardObject.transform.localPosition = new Vector3();
                activeCardObject.transform.localScale = new Vector3(1, 1, 1);

                // for the future might want to stack cards in the discard zone
                Debug.Log("setting card to discard zone: " + card.UniqueID + " with name " + card.data.front.title);
                activeCardObject.SetActive(false);
                card.cardZone = discardDropZone;

            }
        }

        foreach (int key in inactives)
        {
            Debug.Log("key being discarded is " + key);
            if (!discardFromArea.Remove(key))
            {
                Debug.Log("card not removed where it supposedly was from: " + key);
            }
        }
    }

    public int GetAmountSpentOnVulnerabilities()
    {
        return mValueSpentOnVulnerabilities;
    }

    public int GetTotalFacilityValue()
    {
        return mTotalFacilityValue;
    }

    public void ClearAllHighlightedFacilities()
    {
        foreach (GameObject facility in ActiveFacilities.Values)
        {
            Card card = facility.GetComponent<Card>();
            if (card.OutlineActive())
            {
                card.OutlineImage.SetActive(false);
            }
        }
    }

    // if we've recently drawn an unconnected facility then the goal
    // is to highlight that facility automatically so that a second
    // facility is chosen to connect it to. This makes an initial
    // connection easy. All other connections must have both facilities
    // to connect chosen
    public void HandleConnections(bool highlightFirst)
    {
        if (highlightFirst && !mAllFacilitiesDrawn && (ActiveFacilities.Count > 1))
        {
            int currentFacilityToHighlight = mFacilityNumber - 1;
            // find which facility this is since the unique numbers aren't the same
            // as the place the facility is located
            foreach (GameObject facility in ActiveFacilities.Values)
            {
                Card card = facility.GetComponent<Card>();
                if (card.WhichFacilityZone == currentFacilityToHighlight)
                {
                    card.OutlineImage.SetActive(true);
                    mDrawnFacilityZone = card.WhichFacilityZone;
                    Debug.Log("first facillity for connection set!");
                    break;
                }
            }
        }
        else if (ActiveFacilities.Count == 1)
        {
            mNewFacilityConnected = true;
        }
        else
        {
            // if two facilities are selected then they become connected
            int howManySelected = 0;
            Card firstSelected = null;
            Card secondSelected = null;
            foreach (GameObject activeFacilityObject in ActiveFacilities.Values)
            {
                Card card = activeFacilityObject.GetComponent<Card>();
                if (card.OutlineActive())
                {
                    if (howManySelected == 0)
                    {
                        firstSelected = card;
                    } else if (howManySelected == 1)
                    {
                        secondSelected = card;
                    }
                    howManySelected++;
                }

                if (howManySelected == 2)
                {
                    bool connectionAlreadyExists = false;

                    // form a connection between these two facilities
                    // this is a facility to connect to
                    int firstNumber = firstSelected.WhichFacilityZone;
                    int secondNumber = secondSelected.WhichFacilityZone;
                    GameObject firstFacilityHolder = firstSelected.CanvasHolder;
                    // add this connection to the initial facility's list
                    // only if it doesn't already have it
                    if (firstSelected.ConnectionList.FindIndex(
                        x => x.WhichFacilityZone == secondNumber) == -1)
                    {
                        firstSelected.ConnectionList.Add(new FacilityConnectionInfo
                        {
                            WhichFacilityZone = secondNumber,
                            UniqueFacilityID = secondSelected.UniqueID
                        });
                    } else
                    {
                        connectionAlreadyExists = true;
                    }

                    // add the connection to the second facility's list as well
                    // only if it doesn't already have it
                    if (secondSelected.ConnectionList.FindIndex(
                       x => x.WhichFacilityZone == firstNumber) == -1)
                    {
                        secondSelected.ConnectionList.Add(new FacilityConnectionInfo
                        {
                            WhichFacilityZone = firstNumber,
                            UniqueFacilityID = firstSelected.UniqueID
                        });
                    } else
                    {
                        connectionAlreadyExists = true;
                    }

                    // this allows us to keep track of info to send to the opponent
                    if (!connectionAlreadyExists)
                    {
                        mNewConnectionUniqueIDs.Add(firstSelected.UniqueID);
                    }

                    // now turn on the connection image in the interface
                    Connections connections = firstFacilityHolder.GetComponent<Connections>();
                    if (connections != null)
                    {
                        connections.connections[secondNumber].SetActive(true);
                        Debug.Log("setting a connection from " + firstSelected.WhichFacilityZone + " to " + secondNumber);
                    }
                    else
                    {
                        Debug.Log("something wrong as connections are null");
                    }

                    // now unhighlight both facilities that have been connected
                    firstSelected.OutlineImage.SetActive(false);
                    secondSelected.OutlineImage.SetActive(false);

                    if (!mAllFacilitiesDrawn && (mDrawnFacilityZone == firstNumber || mDrawnFacilityZone == secondNumber))
                    {
                        mNewFacilityConnected = true;
                    }

                }
            }
        }
    }

    public bool GetNewFacilityConnected()
    {
        return mNewFacilityConnected;
    }

    public void ReturnCardsToHand()
    {
        if (HandCards.Count != 0)
        {
            foreach (GameObject gameObjectCard in HandCards.Values)
            {
                Card card = gameObjectCard.GetComponent<Card>();
                handPositioner.ReturnCardToHand(card);
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

                    // DO a AABB collision test to see if the card is on the discard drop
                    if (phase == GamePhase.DrawAndDiscard && (cardPosition.y < discardDropMax.y &&
                       cardPosition.y > discardDropMin.y &&
                       cardPosition.x < discardDropMax.x &&
                       cardPosition.x > discardDropMin.x))
                    {
                        Debug.Log("card dropped in discard zone or needs to be discarded" + card.UniqueID);

                        // change parent and rescale
                        card.state = CardState.CardNeedsToBeDiscarded;
                        playCount = 1;
                    }
                    else
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
                                if (card.data.data.cardType==CardType.Defense && CheckHighlightedStations())
                                {
                                    GameObject selected = GetHighlightedStation();
                                    Card selectedCard = selected.GetComponent<Card>();
                                    StackCards(selected, gameObjectCard, playerDropZone, GamePhase.Defense);
                                    card.state = CardState.CardInPlay;
                                    card.WhichFacilityZone = selectedCard.WhichFacilityZone;
                                    ActiveCards.Add(card.UniqueID, gameObjectCard);
                                    handPositioner.DiscardCard(gameObjectCard);
                                    
                                    selectedCard.ModifyingCards.Add(card.UniqueID);
                                    mUpdatesThisPhase.Add(new Updates
                                    {
                                        WhatToDo=AddOrRem.Add,
                                        UniqueFacilityID=selectedCard.UniqueID,
                                        CardID=card.data.data.cardID
                                    });

                                    // we should play the card's effects
                                    card.Play(this, opponentPlayer, selectedCard);
                                    playCount = 1;
                                    selectedCard.OutlineImage.SetActive(false);
                                    playKey = card.UniqueID;
                                }
                                else
                                {
                                    card.state = CardState.CardDrawn;
                                    handPositioner.ReturnCardToHand(card);
                                    manager.DisplayGameStatusPlayer("Please select a single facility you own and play a defense card type.");
                                }
                                break;
                            case GamePhase.Mitigate:
                                if (card.data.data.cardType == CardType.Mitigation && CheckHighlightedStations())
                                {
                                    Debug.Log("trying to mitigate");
                                    GameObject selected = GetHighlightedStation();
                                    Card selectedCard = selected.GetComponent<Card>();

                                    ActiveCards.Add(card.UniqueID, gameObjectCard);
                                    
                                    // we should play the card's effects
                                    card.Play(this, opponentPlayer, selectedCard);

                                    if (card.state == CardState.CardNeedsToBeDiscarded)
                                    {
                                        handPositioner.DiscardCard(gameObjectCard);
                                        //HandleDiscard(ActiveCards, playerDropZone, selectedCard.UniqueID, false);
                                        //opponentPlayer.HandleDiscard(opponentPlayer.ActiveCards, playerDropZone, selectedCard.UniqueID, true);
                                        DiscardAllInactiveCards(DiscardFromWhere.MyPlayZone, false, selectedCard.UniqueID);
                                        opponentPlayer.DiscardAllInactiveCards(DiscardFromWhere.MyPlayZone, true, selectedCard.UniqueID);
                                        playCount = 1;
                                        playKey = card.UniqueID;
                                        selectedCard.OutlineImage.SetActive(false);
                                    }
                                    else
                                    {
                                        // remove what we just added
                                        ActiveCards.Remove(card.UniqueID);
                                        card.state = CardState.CardDrawn;
                                        //handPositioner.ReturnCardToHand(card);
                                        manager.DisplayGameStatusPlayer("Please select a card that can mitigate a vulnerability card on a chosen facility.");
                                    }
                                }
                                else
                                {
                                    card.state = CardState.CardDrawn;
                                    handPositioner.ReturnCardToHand(card);
                                    manager.DisplayGameStatusPlayer("Please select a single opponent facility and play a vulnerability card.");
                                }
                                break;
                            default:
                                // we're not in the right phase, so
                                // reset the dropped state
                                card.state = CardState.CardDrawn;
                                handPositioner.ReturnCardToHand(card);
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
                                bool checkHighlightedStations = opponentPlayer.CheckHighlightedStations();
                                if (((card.data.data.cardType == CardType.Vulnerability) || 
                                    (card.data.data.cardType == CardType.LateralMovement)) && 
                                    checkHighlightedStations &&
                                    ((mValueSpentOnVulnerabilities + card.data.data.cost) <= mTotalFacilityValue))
                                {
                                    GameObject selected = opponentPlayer.GetHighlightedStation();
                                    Card selectedCard = selected.GetComponent<Card>();
                                    if (!DuplicateCardPlayed(selectedCard, card))
                                    {
                                        StackCards(selected, gameObjectCard, opponentDropZone, GamePhase.Vulnerability);
                                        card.state = CardState.CardInPlay;
                                        ActiveCards.Add(card.UniqueID, gameObjectCard);
                                        handPositioner.DiscardCard(gameObjectCard);
                                        selectedCard.AttackingCards.Add(new CardIDInfo
                                        {
                                            CardID = card.data.data.cardID,
                                            UniqueID = card.UniqueID
                                        });
                                        mUpdatesThisPhase.Add(new Updates
                                        {
                                            WhatToDo = AddOrRem.Add,
                                            UniqueFacilityID = selectedCard.UniqueID,
                                            CardID = card.data.data.cardID
                                        });

                                        // we don't play vuln effects until the attack phase
                                        playCount = 1;
                                        playKey = card.UniqueID;
                                        selectedCard.OutlineImage.SetActive(false);
                                        mValueSpentOnVulnerabilities += card.data.data.cost;
                                        Debug.Log("Amount spent on vuln is " + mValueSpentOnVulnerabilities + " with total facility worth of " + mTotalFacilityValue);

                                    }
                                }
                                else if (card.data.data.cardType == CardType.Instant && checkHighlightedStations &&
                                ((mValueSpentOnVulnerabilities + card.data.data.cost) <= mTotalFacilityValue))
                                {
                                    GameObject selected = opponentPlayer.GetHighlightedStation();
                                    Card selectedCard = selected.GetComponent<Card>();

                                    StackCards(selected, gameObjectCard, opponentDropZone, GamePhase.Vulnerability);
                                    card.state = CardState.CardInPlay;
                                    ActiveCards.Add(card.UniqueID, gameObjectCard);
                                    handPositioner.DiscardCard(gameObjectCard);
                                    selectedCard.AttackingCards.Add(new CardIDInfo
                                    {
                                        CardID = card.data.data.cardID,
                                        UniqueID = card.UniqueID
                                    });
                                    mUpdatesThisPhase.Add(new Updates
                                    {
                                        WhatToDo = AddOrRem.Add,
                                        UniqueFacilityID = selectedCard.UniqueID,
                                        CardID = card.data.data.cardID
                                    });

                                    playCount = 1;
                                    playKey = card.UniqueID;
                                    selectedCard.OutlineImage.SetActive(false);
                                    mValueSpentOnVulnerabilities += card.data.data.cost;
                                    Debug.Log("Amount spent on instant is " + mValueSpentOnVulnerabilities + " with total facility worth of " + mTotalFacilityValue);
                                    
                                    // send update immediately to opponent rather than waiting for the end of the phase
                                    // NOTE: it will send all updates rather than just the one
                                    // WORK: might want to change this in the future
                                    manager.SendUpdatesToOpponent(GamePhase.Vulnerability, this);
                                }
                                else
                                {
                                    card.state = CardState.CardDrawn;
                                    handPositioner.ReturnCardToHand(card);
                                    manager.DisplayGameStatusPlayer("Can't play multiples of the same card on a station.");

                                }
                                break;
                            default:
                                // we're not in the right phase, so
                                // reset the dropped state
                                card.state = CardState.CardDrawn;
                                handPositioner.ReturnCardToHand(card);
                                break;
                        }
                    }
                    else
                    {
                        Debug.Log("card not dropped in card drop zone");
                        // If it fails, parent it back to the hand location and then set its state to be in hand and make it grabbable again
                        gameObjectCard.transform.SetParent(handDropZone.transform, false);
                        card.state = CardState.CardDrawn;
                        handPositioner.ReturnCardToHand(card);
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
            if (phase == GamePhase.DrawAndDiscard)
            {
                // we're not discarding a facility or sharing what we're discarding with the opponent
                DiscardAllInactiveCards(DiscardFromWhere.Hand, false, -1);
            } else
            {
                // remove the discarded card
                if (!HandCards.Remove(playKey))
                {
                    Debug.Log("didn't find a key to remove! " + playKey);
                }
            }      
        }

        return playCount;
    }

    public void MitigateInstantAttack(Updates updateInfo, CardPlayer opponent)
    {
        // find halt card in active hand
        GameObject haltGameObj = null;
        foreach(GameObject potentialGameObj in HandCards.Values)
        {
            Card tmp = potentialGameObj.GetComponent<Card>();
            if (tmp.data.data.cardType == CardType.Halt)
            {
                haltGameObj = potentialGameObj;
                break;
            }
        }

        if (haltGameObj != null)
        {
            Debug.Log("found halt card!");
            Card haltCard = haltGameObj.GetComponent<Card>();
            GameObject facilityGameObject = null;
            if (ActiveFacilities.TryGetValue(updateInfo.UniqueFacilityID, out facilityGameObject))
            {
                Debug.Log("found facility!");
                Card facilityCard = facilityGameObject.GetComponent<Card>();
                haltCard.Play(this, opponent, facilityCard);

                // now discard all facilities annihilated
                DiscardAllInactiveCards(DiscardFromWhere.Hand, false, -1);
                opponent.DiscardAllInactiveCards(DiscardFromWhere.MyPlayZone, true, facilityCard.UniqueID);
                //opponent.DiscardAllInactiveCards(DiscardFromWhere.MyFacility, true, facilityCard.UniqueID);
            }
        }
    }

    public void HandleInstantAttack(Card instantCard, Updates updateInfo, CardPlayer opponent)
    {
        List<int> facilitiesToRemove = new List<int>(8);
        GameObject facilityGameObject = null;
        Debug.Log("inside handle instant attack");
        if (ActiveFacilities.TryGetValue(updateInfo.UniqueFacilityID, out facilityGameObject))
        {
            Debug.Log("Instant card: active facility found");
            Card facilityCard = facilityGameObject.GetComponent<Card>();
            instantCard.Play(this, opponent, facilityCard);

            mUpdatesThisPhase.Add(new Updates
            {
                WhatToDo = AddOrRem.Remove,
                UniqueFacilityID = facilityCard.UniqueID,
                CardID = instantCard.data.data.cardID
            });

            Debug.Log("facility worth is " + (facilityCard.data.data.worth + facilityCard.DefenseHealth));

            // now check the total worth of the facility to see if it
            // and do a removal of all cards that were spent in attacks
            if (facilityCard.data.data.worth + facilityCard.DefenseHealth <= 0)
            {
                Debug.Log("we need to get rid of this facility");

                // remove the worth of this facility since it's no longer there
                mTotalFacilityValue -= facilityCard.data.data.worth;
                // get rid of all potential connectors from both sides
                int whichFacilityZone = facilityCard.WhichFacilityZone;
                GameObject facilityZoneHolder = facilityCard.CanvasHolder;
                Connections connections = facilityZoneHolder.GetComponent<Connections>();

                foreach (FacilityConnectionInfo connectionInfo in facilityCard.ConnectionList)
                {
                    int zoneToTurnOff = connectionInfo.WhichFacilityZone;
                    // turn off this facility's possible connection to the other
                    Debug.Log("turning off a specific zone " + zoneToTurnOff);
                    connections.connections[zoneToTurnOff].SetActive(false);
                    Debug.Log("turned off");
                    // turn off the other facility's possible connection to this one
                    // and delete this facility from its active connections list
                    Debug.Log("getting facility with id " + connectionInfo.UniqueFacilityID);

                    GameObject otherFacility;
                    ActiveFacilities.TryGetValue(connectionInfo.UniqueFacilityID, out otherFacility);
                    Debug.Log("got facility");

                    if (otherFacility != null)
                    {
                        Card otherFacilityCard = otherFacility.GetComponent<Card>();
                        GameObject otherFacilityZone = otherFacilityCard.CanvasHolder;
                        Connections otherConnections = otherFacilityZone.GetComponent<Connections>();
                        Debug.Log("other connection zone setting to false");
                        otherConnections.connections[whichFacilityZone].SetActive(false);
                        Debug.Log("done");
                        int indexToRemove = otherFacilityCard.ConnectionList.FindIndex(x => x.WhichFacilityZone == whichFacilityZone);
                        if (indexToRemove >= 0)
                        {
                            otherFacilityCard.ConnectionList.RemoveAt(indexToRemove);
                        }
                        else
                        {
                            Debug.Log("Error finding facility for zone " + whichFacilityZone + " to remove.");
                        }

                    }
                    else
                    {
                        Debug.Log("Error finding facility " + connectionInfo.UniqueFacilityID + " to turn its connection off");
                    }
                }
                // now clear all the connection info
                facilityCard.ConnectionList.Clear();

                // the facility needs to be removed along with all remaining
                // attack cards on it
                foreach (CardIDInfo cardInfo in facilityCard.AttackingCards)
                {
                    GameObject cardObject = opponent.GetActiveCardObject(cardInfo);
                    if (cardObject != null)
                    {
                        Card cardToDispose = cardObject.GetComponent<Card>();
                        Debug.Log("handling all attack cards on defunct facility : this one's id is " + cardToDispose.UniqueID);
                        cardToDispose.state = CardState.CardNeedsToBeDiscarded;

                    }
                    else
                    {
                        Debug.Log("attack card with id " + cardInfo.CardID + " wasn't found in the pile of cards on a defunct facility.");
                    }
                    //opponent.HandleDiscard(opponent.ActiveCards, opponent.opponentDropZone, facilityCard.UniqueID, true);
                }

                // get rid of defense cards on this facility as well
                foreach (GameObject cardObject in ActiveCards.Values)
                {
                    Card card = cardObject.GetComponent<Card>();
                    if (card.WhichFacilityZone == facilityCard.WhichFacilityZone)
                    {
                        Debug.Log("getting rid of defense facility on zone " + card.WhichFacilityZone);
                        card.state = CardState.CardNeedsToBeDiscarded;
                        // note that we don't add an update as the opponent's side will delete all cards with the facility card
                    }
                }

                // let's discard all the cards on the facility in question
                //opponent.DiscardAllInactiveCards(DiscardFromWhere.Hand, true, facilityCard.UniqueID);
                

                facilityCard.AttackingCards.Clear();
                facilityCard.state = CardState.CardNeedsToBeDiscarded;

                mUpdatesThisPhase.Add(new Updates
                {
                    WhatToDo = AddOrRem.Remove,
                    UniqueFacilityID = facilityCard.UniqueID,
                    CardID = facilityCard.data.data.cardID
                });

            }

            // now discard all facilities annihilated
            DiscardAllInactiveCards(DiscardFromWhere.MyPlayZone, false, -1);
            DiscardAllInactiveCards(DiscardFromWhere.MyFacility, false, -1);
            opponent.DiscardAllInactiveCards(DiscardFromWhere.MyPlayZone, true, facilityCard.UniqueID);
        }
        else
        {
            Debug.Log("This facility doesn't exist: " + updateInfo.UniqueFacilityID);
        }
    }

    public bool DuplicateCardPlayed(Card facilityCard, Card cardToPlay)
    {
        bool duplicateCardFound = false;

        foreach(CardIDInfo cardInfo in facilityCard.AttackingCards)
        {
            if (cardInfo.CardID == cardToPlay.data.data.cardID)
            {
                duplicateCardFound = true;
                break;
            }
        }

        return duplicateCardFound;
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
                    handPositioner.ReturnCardToHand(card);
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


    public bool CheckForCardsOfType(CardType cardType, Dictionary<int, GameObject> listToCheck)
    {
        bool hasCardType = false;

        foreach(GameObject gameObject in listToCheck.Values)
        {
            Card card = gameObject.GetComponent<Card>();
            if (card.data.data.cardType == cardType)
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
                    Debug.Log("adding attack with card id : " + card.data.data.cardID);
                    facilityCard.AttackingCards.Add(new CardIDInfo {
                        CardID = card.data.data.cardID,
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
                                //manager.actualPlayer.HandleDiscard(manager.actualPlayer.HandCards, manager.actualPlayer.playerDropZone,
                                //    facilityCard.UniqueID, false);
                                //manager.actualPlayer.HandleDiscard(manager.actualPlayer.ActiveCards, manager.actualPlayer.playerDropZone,
                                //facilityCard.UniqueID, false);
                                manager.actualPlayer.DiscardAllInactiveCards(DiscardFromWhere.Hand, false, facilityCard.UniqueID);
                                manager.actualPlayer.DiscardAllInactiveCards(DiscardFromWhere.MyPlayZone, false, facilityCard.UniqueID);
                            } else
                            {
                                Debug.Log("an attack card couldn't be found in the hand at 1139 in CardPlayer. " + cardInfo);
                            }
                            
                            // remove the card info from the facility
                            facilityCard.AttackingCards.RemoveAt(cardIndex);
                        } 
                    }
                    else
                    {
                        // get rid of all connections on this facility
                        Debug.Log("we need to get rid of this opponent facility");
                        // get rid of all potential connectors from both sides
                        int whichFacilityZone = facilityCard.WhichFacilityZone;
                        GameObject facilityZoneHolder = facilityCard.CanvasHolder;
                        Connections connections = facilityZoneHolder.GetComponent<Connections>();

                        foreach (FacilityConnectionInfo connectionInfo in facilityCard.ConnectionList)
                        {
                            int zoneToTurnOff = connectionInfo.WhichFacilityZone;
                            // turn off this facility's possible connection to the other
                            connections.connections[zoneToTurnOff].SetActive(false);

                            // turn off the other facility's possible connection to this one
                            // and delete this facility from its active connections list
                            GameObject otherFacility = ActiveFacilities[connectionInfo.UniqueFacilityID];
                            if (otherFacility != null)
                            {
                                Card otherFacilityCard = otherFacility.GetComponent<Card>();
                                GameObject otherFacilityZone = otherFacilityCard.CanvasHolder;
                                Connections otherConnections = otherFacilityZone.GetComponent<Connections>();
                                otherConnections.connections[whichFacilityZone].SetActive(false);
                                int indexToRemove = otherFacilityCard.ConnectionList.FindIndex(x => x.WhichFacilityZone == whichFacilityZone);
                                if (indexToRemove >= 0)
                                {
                                    otherFacilityCard.ConnectionList.RemoveAt(indexToRemove);
                                }
                                else
                                {
                                    Debug.Log("Error finding facility for zone " + whichFacilityZone + " to remove.");
                                }

                            }
                            else
                            {
                                Debug.Log("Error finding facility " + connectionInfo.UniqueFacilityID + " to turn its connection off");
                            }
                        }

                        // now clear all the connection info
                        facilityCard.ConnectionList.Clear();

                        // get rid of defense cards on this facility as well
                        foreach (GameObject cardObject in ActiveCards.Values)
                        {
                            Card card = cardObject.GetComponent<Card>();
                            if (card.WhichFacilityZone == facilityCard.WhichFacilityZone)
                            {
                                Debug.Log("getting rid of defense facility on zone " + card.WhichFacilityZone);
                                card.state = CardState.CardNeedsToBeDiscarded;
                            }
                        }

                        // discard all the cards attacking this now dead facility
                        foreach (CardIDInfo cardInfo in facilityCard.AttackingCards)
                        {
                            GameObject cardObject = manager.actualPlayer.GetActiveCardObject(cardInfo);
                            if (cardObject != null)
                            {
                                Card cardToDispose = cardObject.GetComponent<Card>();
                                Debug.Log("handling all attack cards on defunct facility : this one's id is " + cardToDispose.UniqueID);
                                cardToDispose.state = CardState.CardNeedsToBeDiscarded;

                            }
                            else
                            {
                                Debug.Log("attack card with id " + cardInfo.CardID + " wasn't found in the pile of cards on a defunct facility.");
                            }
                            //opponent.HandleDiscard(opponent.ActiveCards, opponent.opponentDropZone, facilityCard.UniqueID, true);
                        }
                        // let's discard all the cards on the facility in question
                        manager.actualPlayer.DiscardAllInactiveCards(DiscardFromWhere.MyPlayZone, true, facilityCard.UniqueID);
                        manager.actualPlayer.DiscardAllInactiveCards(DiscardFromWhere.MyFacility, true, facilityCard.UniqueID);
                        manager.actualPlayer.DiscardAllInactiveCards(DiscardFromWhere.Hand, true, facilityCard.UniqueID);

                        facilityCard.AttackingCards.Clear();
                        facilityCard.state = CardState.CardNeedsToBeDiscarded;

                        // now discard the facility and all its cards     
                        DiscardAllInactiveCards(DiscardFromWhere.MyPlayZone, false, facilityCard.UniqueID);
                        DiscardAllInactiveCards(DiscardFromWhere.MyFacility, false, facilityCard.UniqueID);

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
                    card.WhichFacilityZone = selectedCard.WhichFacilityZone;
                    ActiveCards.TryAdd(card.UniqueID, cardGameObject);

                    Debug.Log("opponent player updates added " + card.data.data.cardID + " to the active list of size " + ActiveCards.Count);
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

    public void AddConnections(ref List<FacilityConnectionInfo> updates, int uniqueFacilityID)
    {
        GameObject facilityObject = ActiveFacilities[uniqueFacilityID];

        if (facilityObject != null)
        {
            Card facilityCard = facilityObject.GetComponent<Card>();
            GameObject facilityHolder = facilityCard.CanvasHolder;
            Connections connections = facilityHolder.GetComponent<Connections>();
            if (connections != null)
            {
                foreach(FacilityConnectionInfo update in updates)
                {
                    // add this connection to the initial facility's list
                    if (facilityCard.ConnectionList.FindIndex(
                       x => x.WhichFacilityZone == update.WhichFacilityZone) == -1)
                    {
                        facilityCard.ConnectionList.Add(update);
                    }
                       

                    // add it to the other facility list as well
                    GameObject otherFacility = ActiveFacilities[update.UniqueFacilityID];
                    if (otherFacility != null)
                    {
                        Card otherCard = otherFacility.GetComponent<Card>();

                        if (otherCard.ConnectionList.FindIndex(
                      x => x.WhichFacilityZone == facilityCard.WhichFacilityZone) == -1)
                        {
                            // add the connection to the second facility's list as well
                            otherCard.ConnectionList.Add(new FacilityConnectionInfo
                            {
                                WhichFacilityZone = facilityCard.WhichFacilityZone,
                                UniqueFacilityID = facilityCard.UniqueID
                            });
                        }
                           
                    } else
                    {
                        Debug.Log("connected facility " + update.UniqueFacilityID + " is null. There's an error in the program.");
                    }
                   
                    // now turn on the connection image in the interface
                    connections.connections[update.WhichFacilityZone].SetActive(true);          
                }
            } else
            {
                Debug.Log("Connections on opponent was null. There's an error somewhere!");
            }
        }
         else   
        {
            Debug.Log("opponent sent facility that is NOT in the active facilities for connection update. Error!");
        }
       
    }

    public int GetFacilityScores()
    {
        int score = 0;
        foreach(GameObject facilityObj in ActiveFacilities.Values)
        {
            Card facilityCard = facilityObj.GetComponent<Card>();
            score += (facilityCard.data.data.worth + facilityCard.DefenseHealth);
        }

        return score;
    }

    public int GetConnectionScores()
    {
        int score = 0;
        foreach (GameObject facilityObj in ActiveFacilities.Values)
        {
            Card facilityCard = facilityObj.GetComponent<Card>();
            if (facilityCard.ConnectionList.Count >= facilityCard.data.data.worth)
            {
                score += 1;
            }   
        }

        return score;
    }

    public bool HasUpdates()
    {
        return (mUpdatesThisPhase.Count != 0);
    }

    // an update message consists of:
    // a. count of updates - 1 per card
    // b. what game phase this is happening for
    // c. the list of updates in the order of: add/remove, unique facility id, card id
    public void GetUpdatesInMessageFormat(ref List<int> playsForMessage, GamePhase phase)
    {
        playsForMessage.Add(mUpdatesThisPhase.Count);
        playsForMessage.Add((int)phase);

        foreach(Updates update in mUpdatesThisPhase)
        {
            playsForMessage.Add((int)update.WhatToDo);
            playsForMessage.Add(update.UniqueFacilityID);
            playsForMessage.Add(update.CardID);
            Debug.Log("adding update to send to opponent: " + update.UniqueFacilityID + " and card id " + update.CardID + " for phase " + phase);
        }

        // we've given the updates away, so let's make sure to 
        // clear the list
        mUpdatesThisPhase.Clear();
    }

    // an attack update message consists of:
    // a. count of updates - 1 per facility
    // b. the list of updates in the order of: unique facility id, change in value
    public void GetAttackUpdatesInMessageFormat(ref List<int> playsForMessage)
    {
        playsForMessage.Add(mAttackUpdates.Count);
       
        foreach (AttackUpdate update in mAttackUpdates)
        {
            playsForMessage.Add(update.UniqueFacilityID);
            playsForMessage.Add(update.ChangeInValue);
            //Debug.Log("adding update to send to opponent: " + update.UniqueFacilityID + " and card id " + update.CardID + " for phase " + phase);
        }

        // we've given the updates away, so let's make sure to 
        // clear the list
        mAttackUpdates.Clear();
    }

    // new connection messages consists of:
    // a. count of new connections to be attached to chosen facility for connections this round
    // b. unique facility id of facility these connections come from
    // c. list of zone and unique id for all new connections
    public void GetNewConnectionsInMessageFormat(ref List<int> playsForMessage)
    {
        int numberOfMessages = mNewConnectionUniqueIDs.Count;

        if (numberOfMessages > 0)
        {
            int key = mNewConnectionUniqueIDs[numberOfMessages - 1];
            GameObject facilityObject = null;
            
            if (ActiveFacilities.TryGetValue(key, out facilityObject))
            {
                Card card = facilityObject.GetComponent<Card>();
                if (card.ConnectionList.Count > 0)
                {
                    playsForMessage.Add(card.ConnectionList.Count);
                    playsForMessage.Add(card.UniqueID);
                    foreach (FacilityConnectionInfo info in card.ConnectionList)
                    {
                        playsForMessage.Add(info.UniqueFacilityID);
                        playsForMessage.Add(info.WhichFacilityZone);
                    }
                }

                // remove the facility id since we've created the message for it
                mNewConnectionUniqueIDs.RemoveAt(numberOfMessages - 1);
            }
        
        }

        if (numberOfMessages > 1)
        {
            Debug.Log("ERROR: shouldn't be able to get more than one connection per frame");
        }
    }

    public void DisplayAttackUpdates(ref List<AttackUpdate> updates)
    {
        int uniqueFacilityID;
        GameObject facilityObj;

        foreach (AttackUpdate update in updates)
        {
            uniqueFacilityID = update.UniqueFacilityID;
            if (ActiveFacilities.TryGetValue(uniqueFacilityID, out facilityObj))
            {
                // the facility exists and we need to get the card and update the value
                Card facilityCard = facilityObj.GetComponent<Card>();
                facilityCard.DefenseHealth += update.ChangeInValue;

                TextMeshProUGUI[] tempTexts = facilityCard.GetComponentsInChildren<TextMeshProUGUI>(true);
                for (int i = 0; i < tempTexts.Length; i++)
                {
                    if (tempTexts[i].name.Equals("Description Text"))
                    {
                        tempTexts[i].color = Color.red;
                        tempTexts[i].text = "<size=600%>" + facilityCard.DefenseHealth;
                    }
                }
            }
        }

        updates.Clear();
    }

    // Reset the variables in this class to allow for a new
    // game to happen.
    public void ResetForNewGame()
    {
        //we'll draw them all again, so reset the id's
        sUniqueIDCount = 0;

        Debug.Log("resetting all game objects on screen - destroying game objects.");
        foreach(GameObject gameObject in HandCards.Values)
        {
            Card card = gameObject.GetComponent<Card>();
            //if (card.CanvasHolder != null)
            //{
            //    Destroy(card.CanvasHolder);
            //}
            Destroy(card);
            Destroy(gameObject);
        }

        foreach (GameObject gameObject in Discards.Values)
        {
            Card card = gameObject.GetComponent<Card>();
            //if (card.CanvasHolder != null)
            //{
            //    Destroy(card.CanvasHolder);
            //}
            Destroy(card);
            Destroy(gameObject);
        }

        foreach (GameObject gameObject in ActiveCards.Values)
        {
            Card card = gameObject.GetComponent<Card>();
            //if (card.CanvasHolder != null)
            //{
            //    Destroy(card.CanvasHolder);
            //}
            Destroy(card);
            Destroy(gameObject);
        }

        foreach (GameObject gameObject in ActiveFacilities.Values)
        {
            Card card = gameObject.GetComponent<Card>();
            if (card.CanvasHolder != null)
            {
                // now turn on the connection image in the interface
                Connections connections = card.CanvasHolder.GetComponent<Connections>();
                if (connections != null)
                {
                    foreach(GameObject conn in connections.connections)
                    {
                        if (conn != null)
                        {
                            conn.SetActive(false);
                        }
                    }
                }
                //Destroy(card.CanvasHolder); // this would destroy the facilities!
            }
            Destroy(card);
            Destroy(gameObject);
        }

        FacilityIDs.Clear();
        DeckIDs.Clear();
        HandCards.Clear();
        Discards.Clear();
        ActiveCards.Clear();
        ActiveFacilities.Clear();
        handSize = 0;
        mTotalFacilityValue = 0;
        mFacilityNumber = 0;
        mValueSpentOnVulnerabilities = 0;
        mFinalScore = 0;
        mUpdatesThisPhase.Clear();
    }
}
