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

    // TODO: Add Card Types for SD
}

public struct CardData
{
    public int numberInDeck;
    public CardType cardType;
    public float percentSuccess;
    public int cardID;
    public int teamID;
    public int cost; // TODO: 3 for meeples
    public int worth;
    public PlayerTeam onlyPlayedOn;
}
