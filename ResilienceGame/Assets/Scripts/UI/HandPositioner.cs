using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

#region Events

// Custom UnityEvent that passes a Card object
//[System.Serializable]
//public class CardHoverEvent : UnityEvent<Card> { }

#endregion
/// <summary>
/// This class is responsible for arranging the cards in the player's hand.
/// </summary>
public class HandPositioner : MonoBehaviour
{

    //private CardHoverEvent onCardHover = new CardHoverEvent();
    public List<Card> cards = new List<Card>();
    public float arcRadius = 3300f;     //the size of the arc/curve the cards will be placed on
    public float arcAngle = 20f;
    public float cardAngle = 5f;
    private float hoverHeight = 150f;   //how far to move the card up when hovered over
    public float hoverScale = 1f;       //scale of the cards when hovered over
    public float defaultScale = .5f;    //default scale of the cards when in the hand
    public float cardWidth = 125f;      //approximate width of a card
    public float hoverTransitionSpeed = 10f; // transition speed of hover animation faster is faster

    private RectTransform rect; //the rect transform of the hand positioner

    //TODO: unknown if there is a real max cards in game rules but there should be for technical reasons if nothing else
    private const int MIN_CARDS = 1;
    //private const int MAX_CARDS = 10;


    public Card CurrentHoveredCard;
    public Card DraggedCard;

    public bool IsDraggingCard { get; private set; } = false;
    public HashSet<Card> CardsBeingDragged = new HashSet<Card>();
    
    private void Start()
    {
        rect = GetComponent<RectTransform>();     
    }

    /// <summary>
    /// Tells the hand positioner that a card is being dragged
    /// </summary>
    /// <param name="card">The game object of the card being dragged</param>
    public void NotifyCardDragStart(GameObject card)
    {
        CardsBeingDragged.Add(card.GetComponent<Card>());
        IsDraggingCard = true;
        card.transform.localRotation = Quaternion.identity;
        if (CurrentHoveredCard == card)
        {
            CurrentHoveredCard = null;
        }
    }

    /// <summary>
    /// Return a card to its proper place in the hand
    /// </summary>
    /// <param name="card">The card to enter the hand</param>
    public void ReturnCardToHand(Card card)
    {
        //card.SetCardState(CardState.CardDrawn);
        Debug.Log($"Sending {card.front.title} to hand");
        card.transform.SetParent(transform, false);
        card.transform.SetSiblingIndex(card.HandPosition);
        ResetCardSiblingIndices();
    }

    /// <summary>
    /// Tells the hand positioner that the card was dropped after being dragged
    /// </summary>
    /// <param name="card">The card that was dropped</param>
    public void NotifyCardDragEnd(GameObject card)
    {
        CardsBeingDragged.Remove(card.GetComponent<Card>());
        IsDraggingCard = false;
        ////card was played somewhere, so we need to do something with it
        //var dropLoc = GameManager.instance.actualPlayer.hoveredDropLocation;
        //if (dropLoc) {
        //    // Debug.Log($"card was played on: {dropLoc.name}");

        //}
        //else {
        //    //reset scale and reset sibling index to position it correctly in the hand
        //    card.transform.localScale = Vector3.one * defaultScale;
        //    var tCard = card.GetComponent<Card>();
        //    // Debug.Log($"returning {tCard.data.front.title} to position {tCard.HandPosition}");
        //    card.transform.SetSiblingIndex(tCard.HandPosition);
        //    ArrangeCards(); // Rearrange cards when dragging ends
        //}
    }

    public void DiscardCard(GameObject card)
    {
        cards.Remove(card.GetComponent<Card>());
        LayoutElement element = card.GetComponent<LayoutElement>();
        element.ignoreLayout = false;
        // renumber the remaining cards
        UpdateCardPositions();
    }

    private void Update()
    {
        //HandleNewCards();
        ArrangeCards();

        if (!IsDraggingCard)
        {
            HandleHovering();
        }     
    }

    /// <summary>
    /// handles adding and removing cards from this class's card tracker (cards list)
    /// </summary>
    public void HandleNewCard(Card card)
    {
        // Set the scale of the new cards to the default scale
        card.transform.localScale = Vector3.one * defaultScale;
        LayoutElement element = card.GetComponent<LayoutElement>();
        element.ignoreLayout = true;

        // add new card and update positions
        cards.Add(card);
        UpdateCardPositions();
    }

    // Helper method to update card positions
    private void UpdateCardPositions()
    {
        for (int x = 0; x < cards.Count; x++)
        {
            cards[x].GetComponent<Card>().HandPosition = x;
        }
    }

    //handles arranging the cards in the hand by fanning them out in an arc
    //spreads the cards out in the x direction to fill the hand position rect
    private void ArrangeCards()
    {
        int cardCount = cards.Count;
        if (cardCount == 0) return;

        // Calculate the angle step based on the number of cards
        float currentArcAngle = Mathf.Min(arcAngle, Mathf.Max(0, (cardCount - MIN_CARDS) * 5f));
        float angleStep = currentArcAngle / (cardCount - 1);
        if (cardCount <= MIN_CARDS) angleStep = 0;      //3 cards or less should be in a straight line

        //calculate the overlap factor using the max allowed width (hand positioner size) and the total width of the cards
        float startAngle = -currentArcAngle / 2f;
        float maxScreenWidth = rect.rect.width;
        float totalCardWidth = cardWidth * cardCount;

        float overlapFactor = 1f;
        if (totalCardWidth > maxScreenWidth)
        {
            overlapFactor = maxScreenWidth / totalCardWidth;
        }

        float horizontalSpacing = cardWidth * overlapFactor;

        // Loop through all the cards and position them
        for (int i = 0; i < cardCount; i++)
        {
            var card = cards[i];
            // Skip cards that are being dragged
            if (CardsBeingDragged.Contains(card)) continue;
            
            //calculate the angle and x position of the card
            float angle = startAngle + (i * angleStep);
            float x = (i - (cardCount - 1) / 2f) * horizontalSpacing;
            //calculate the y position of the card
            float baseY = Mathf.Cos(angle * Mathf.Deg2Rad) * arcRadius - arcRadius;

            Vector3 targetPosition = new Vector3(x, baseY, 0);
            Quaternion targetRotation = Quaternion.Euler(0, 0, -angle);

            //if the card is the current hover card, rotate it to straight and push it up a bit
            if (card == CurrentHoveredCard)
            {
                targetPosition.y += hoverHeight;
                targetRotation = Quaternion.identity;
            }

            // Smooth position and rotation transition
            card.transform.SetLocalPositionAndRotation(
                Vector3.Lerp(
                    card.transform.localPosition,
                    targetPosition,
                    Time.deltaTime * hoverTransitionSpeed),
                Quaternion.Slerp(
                    card.transform.localRotation,
                    targetRotation,
                    Time.deltaTime * hoverTransitionSpeed));

            // Smooth scale transition
            float targetScale = card == CurrentHoveredCard ? hoverScale : defaultScale;
            card.transform.localScale = Vector3.Lerp(card.transform.localScale, Vector3.one * targetScale, Time.deltaTime * hoverTransitionSpeed);
        }
    }
    ////Allows other classes to subscribe to the onCardHover event
    //public void AddCardHoverListener(UnityAction<Card> listener)
    //{
    //    onCardHover.AddListener(listener);
    //}
    /// <summary>
    /// Handles determining which card is being hovered over
    /// </summary>
    private void HandleHovering()
    {
        Card newHoveredCard = null;

        // If no cards are being dragged, check which card is under the mouse
        if (CardsBeingDragged.Count == 0)
        {
            Vector2 localMousePosition = rect.InverseTransformPoint(Mouse.current.position.ReadValue());
            newHoveredCard = GetCardUnderMouse(localMousePosition.x);
        }

        if (newHoveredCard != CurrentHoveredCard)
        {
            CurrentHoveredCard = newHoveredCard;

            if (CurrentHoveredCard != null)
            {
                CurrentHoveredCard.transform.SetAsLastSibling();
            }
            else
            {
                ResetCardSiblingIndices();
            }

        }
    }

    public void ResetCardSiblingIndices()
    {
        foreach (var card in cards)
        {
            card.transform.SetSiblingIndex(card.GetComponent<Card>().HandPosition);
        }
    }

    /// <summary>
    /// Determines if the mouse is inside of the hand positioner rect
    /// </summary>
    /// <returns>True if the mouse is inside of the hand positioner</returns>
    private bool IsMouseInsideRect()
    {
        // Get the mouse position in screen space
        Vector2 mousePositionScreen = Mouse.current.position.ReadValue();

        // Convert screen space to local space of the RectTransform
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, mousePositionScreen, null, out Vector2 localPoint);

        // Check if the local point is inside the rect
        return rect.rect.Contains(localPoint);
    }

    /// <summary>
    /// Gets the card under the mouse, this is done by finding the card with the closest x position to the mouse to provide a seemless transition between selecting neighboring cards
    /// </summary>
    /// <param name="mouseX">The local x position of the mouse</param>
    /// <returns>A game object, the card in the hand that is closest the the x position of the mouse</returns>
    private Card GetCardUnderMouse(float mouseX)
    {

        if (!IsMouseInsideRect()) return null; //dont hover if the mouse is outside of the hand area

        float minDistance = float.MaxValue;
        Card closestCard = null;

        foreach (var card in cards)
        {
            float cardX = card.transform.localPosition.x;
            float distance = Mathf.Abs(mouseX - cardX);

            if (distance < minDistance)
            {
                minDistance = distance;
                closestCard = card;
            }
        }

        return closestCard;
    }

}