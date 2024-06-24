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
    CardDiscarded,
};

public class Card : MonoBehaviour, IPointerClickHandler
{
    public CardData data;
    // this card needs a unique id since multiples of the same card can be played
    public int UniqueID; 
    public CardFront front;
    public CardState state;
    public GameObject cardZone;
    public GameObject originalParent;
    public Vector3 originalPosition;
    public GameObject CanvasHolder;
    public bool HasCanvas = false;
    public int stackNumber = 0;
    Vector2 mDroppedPosition;
    GameManager mManager;
    public GameObject OutlineImage;
    public List<int> ModifyingCards = new List<int>(10);
    List<ICardAction> mActionList = new List<ICardAction>(6);

    // Start is called before the first frame update
    void Start()
    {
        originalPosition = this.gameObject.transform.position;
        mManager = GameManager.instance;
        OutlineImage.SetActive(false);
    }


    public void OnPointerClick(PointerEventData eventData)
    {
       
        Debug.Log("click happened on card");
        if (this.state == CardState.CardDrawn && data.cardType != CardType.Station)
        {
            // note that click consumes the release of most drag and release motions
            Debug.Log("potentially card dropped.");
            state = CardState.CardDrawnDropped;
            mDroppedPosition = new Vector2(this.transform.position.x, this.transform.position.y);
        }
        else if (this.data.cardType == CardType.Station && mManager.CanStationsBeHighlighted())
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
    public void Play(CardPlayer player, Card cardActedUpon)
    {
        foreach(ICardAction action in mActionList)
        {
            action.Played(player, cardActedUpon);
        }
    }

    // Cancel this card
    public void Cancel(CardPlayer player, Card cardActedUpon)
    {
        foreach (ICardAction action in mActionList)
        {
            action.Canceled(player, cardActedUpon);
        }
    }
}