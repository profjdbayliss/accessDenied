using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using Unity.Collections;

// Enum to track the state of the card
public enum CardState
{
    NotInDeck,
    CardInDeck,
    CardDrawn,
    CardDrawnDropped,
    CardInPlay,
    CardNeedsToBeDiscarded,
    CardDiscarded,
};

public struct CardIDInfo
{
    public int UniqueID;
    public int CardID;
};

public class ReadInCardData
{
    public CardData data;
    public CardFront front;
    public string DeckName;
    // NOTE: this is a string currently because mitigations are for 
    // cards from the other player's deck.
    public List<string> MitigatesWhatCards = new List<string>(10);
    public List<ICardAction> ActionList = new List<ICardAction>(6);
}

public class Card : MonoBehaviour, IPointerClickHandler
{

    [HideInInspector]
    public ReadInCardData data;

    // this card needs a unique id since multiples of the same card can be played
    public int UniqueID; 
    //public CardFront front;
    public CardState state;   
    public int WhichFacilityZone = 0;
    public GameObject cardZone;
    public GameObject originalParent;
    public Vector3 originalPosition;
    public GameObject CanvasHolder;
    public bool HasCanvas = false;
    public int stackNumber = 0;
    public GameObject OutlineImage;
    public int DefenseHealth = 0;
    public List<int> ModifyingCards = new List<int>(10);
    public List<CardIDInfo> AttackingCards = new List<CardIDInfo>(10);
    public List<FacilityConnectionInfo> ConnectionList = new List<FacilityConnectionInfo>(3);


    Vector2 mDroppedPosition;
    GameManager mManager; 

    public int HandPosition { get; set; } = 0;

    // Start is called before the first frame update
    public void Start()
    {
        originalPosition = this.gameObject.transform.position;
        mManager = GameObject.FindObjectOfType<GameManager>();
        OutlineImage.SetActive(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
       
        Debug.Log("click happened on card");
        if (this.state == CardState.CardDrawn && data.data.cardType != CardType.Station)
        {
            // note that click consumes the release of most drag and release motions
            Debug.Log("potentially card dropped.");
            state = CardState.CardDrawnDropped;
            mDroppedPosition = new Vector2(this.transform.position.x, this.transform.position.y);
        }
        else if (data.data.cardType == CardType.Station && mManager.CanStationsBeHighlighted())
        {
            // only station type cards can be highlighted and played on
            // for this game
            Debug.Log("right card type and phase for highlight");
            if (OutlineImage.activeSelf)
            {
                // turn off activation
                OutlineImage.SetActive(false);
            }
            else
            {
                OutlineImage.SetActive(true);
            }
        }
    }

    public bool OutlineActive()
    {
        return OutlineImage.activeSelf;
    }

    // we save the exact position of dropping so others can look at it
    public Vector2 getDroppedPosition() {
        return mDroppedPosition;
    }

    // Play all of a cards actions
    public void Play(CardPlayer player, CardPlayer opponent, Card cardActedUpon, Card cardForAttack)
    {
        foreach (ICardAction action in data.ActionList)
        {
            action.Played(player, opponent, cardActedUpon, cardForAttack);
        }
    }

    // Play all of a cards actions
    public void Play(CardPlayer player, CardPlayer opponent, Card cardActedUpon)
    {
        foreach(ICardAction action in data.ActionList)
        {
            action.Played(player, opponent, cardActedUpon, this);
        }
    }

    public bool CanMitigate(string attackName)
    {
        bool canMitigate = false;

        foreach(string mitigation in data.MitigatesWhatCards)
        {
            if (attackName.Equals(mitigation)) {
                canMitigate = true;
                break;
            }
        }
        return canMitigate;
    }
}