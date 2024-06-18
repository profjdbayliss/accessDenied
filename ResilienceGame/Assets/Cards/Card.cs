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

public class Card : MonoBehaviour, IDropHandler
{
    public CardData data;
    public CardFront front;
    public CardState state;
    public GameObject cardZone;
    public GameObject discardDropZone;
    public GameObject originalParent;
    public Vector3 originalPosition;
    Vector2 mDroppedPosition;

    // Start is called before the first frame update
    void Start()
    {
        originalPosition = this.gameObject.transform.position;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (this.state == CardState.CardDrawn) // Make sure that the card actually has a reference to the card drop location where it will be dropped and that it is currently in the players hand
        {
            Debug.Log("card drop method run.");
            state = CardState.CardDrawnDropped;     
            mDroppedPosition = new Vector2(this.transform.position.x, this.transform.position.y);

        }

    }

    // we save the exact position of dropping so others can look at it
    public Vector2 getDroppedPosition() {
        return mDroppedPosition;
    }
}