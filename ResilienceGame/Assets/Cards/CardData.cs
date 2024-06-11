using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Card;

public enum CardType
{
    Station,
    Defense,
    Vulnerability,
    Mitigation,
    Instant,
    Special,
    None
}

public struct CardData
{
    public int numberInDeck;
    public CardType type;
    public float percentSuccess;
    public int cardID;
    public int teamID;
    public int cost;
    public int worth;
    public PlayerType onlyPlayedOn;
}
