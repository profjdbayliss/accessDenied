using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Visual parts of the card
public class CardFront : MonoBehaviour
{
    public CardType type;
    public bool worthCircle;
    public bool costCircle;
    public Color titleColor;
    public string title;
    public string description;
    //public GameObject innerTexts;
    public Texture2D background;
    public Texture2D img;
}
