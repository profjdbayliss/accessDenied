using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Visual parts of the card
public class CardFront : MonoBehaviour
{
    public CardType cardType;
    public bool worthCircle;
    public bool costCircle; // TODO: Needs three, one for each meeple color
    public Color color; // TODO: Change name if needed
    public string title;
    public string description;
    public string flavor;
    //public GameObject innerTexts;
    public Texture2D background;
    public Texture2D img;
}
