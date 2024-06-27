using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ActionAddDefenseWorthToStation : ICardAction
{
    public void Played(CardPlayer player, Card cardActedUpon, Card card)
    {
        Debug.Log("card " + card.front.title + " played.");
        cardActedUpon.DefenseHealth += card.data.worth;
        TextMeshProUGUI[] tempTexts = cardActedUpon.GetComponentsInChildren<TextMeshProUGUI>(true);
        for (int i = 0; i < tempTexts.Length; i++)
        {
          if (tempTexts[i].name.Equals("Description Text"))
            {
                tempTexts[i].color = Color.blue;
                tempTexts[i].text = "<size=600%>+" + cardActedUpon.DefenseHealth;
            }         
        }
    }
    public void Canceled(CardPlayer player, Card cardActedUpon, Card card)
    {
        cardActedUpon.DefenseHealth -= card.data.worth;
      
        TextMeshProUGUI[] tempTexts = cardActedUpon.GetComponentsInChildren<TextMeshProUGUI>(true);
        for (int i = 0; i < tempTexts.Length; i++)
        {
            if (cardActedUpon.DefenseHealth > 0 && tempTexts[i].name.Equals("Description Text"))
            {
                tempTexts[i].color = Color.red;
                tempTexts[i].text = "<size=600%>+" + cardActedUpon.DefenseHealth;
            }
            else
            {
                tempTexts[i].text = "";
            }
        }
        
    }
}

public class ActionMitigateCard : ICardAction
{
    public void Played(CardPlayer player, Card cardActedUpon, Card card)
    {
        Debug.Log("card " + card.front.title + " played to mitigate a card on the selected station.");
        bool canMitigate = false;
        string attackName = "";
        GameObject opponentCardObject = null;
        Card opponentCard = null;
        int cardValue = 0;

       foreach (CardIDInfo attackingCardInfo in cardActedUpon.AttackingCards)
        {
            Debug.Log("checking for attack card " + attackingCardInfo.CardID);
            GameObject tmpCardObject = GameManager.instance.GetOpponentActiveCardObject(attackingCardInfo);
            if (tmpCardObject != null)
            {
                Debug.Log("opponent card object obtained!");
                Card tmpCard = tmpCardObject.GetComponent<Card>();
                if (card.CanMitigate(tmpCard.front.title))
                {
                    canMitigate = true;
                    Debug.Log(attackName + " can be mitigated by " + card.front.title);
                    // our goal is to find whichever card is the most negative
                    // and mitigate that one!
                    if (tmpCard.data.worth < cardValue)
                    {
                        opponentCardObject = tmpCardObject;
                        opponentCard = tmpCard;
                        cardValue = tmpCard.data.worth;
                    }
                }
            }
           
        }

       if (canMitigate)
        {
            // get the right card from the station to clear it from the station
            // and send it to opponent's discard pile
            GameManager.instance.DiscardOpponentActiveCard(cardActedUpon.UniqueID, 
                new CardIDInfo {
                    CardID = opponentCard.data.cardID,
                    UniqueID = opponentCard.UniqueID,
                    }, true);

            // put our card in the discard pile as well
            player.DiscardSingleActiveCard(cardActedUpon.UniqueID, 
                new CardIDInfo
                {
                    CardID = card.data.cardID,
                    UniqueID = card.UniqueID
                }, false);

        }

    }
    public void Canceled(CardPlayer player, Card cardActedUpon, Card card)
    {
       // we never cancel mitigations
       // if we had to we'd have to cache the info for the card in this class
    }
}

public class ActionImpactFacilityWorth : ICardAction
{
    public void Played(CardPlayer player, Card cardActedUpon, Card card)
    {
        Debug.Log("card " + card.front.title + " played to attack the selected station.");
        cardActedUpon.DefenseHealth += card.data.worth;
        card.state = CardState.CardNeedsToBeDiscarded;
        TextMeshProUGUI[] tempTexts = cardActedUpon.GetComponentsInChildren<TextMeshProUGUI>(true);
        for (int i = 0; i < tempTexts.Length; i++)
        {
            if (tempTexts[i].name.Equals("Description Text"))
            {
                tempTexts[i].color = Color.red;
                tempTexts[i].text = "<size=600%>+" + cardActedUpon.DefenseHealth;
            }
        }
    }

    public void Canceled(CardPlayer player, Card cardActedUpon, Card card)
    {
        Debug.Log("card " + card.front.title + " attack undone.");
        cardActedUpon.DefenseHealth -= card.data.worth;      
        TextMeshProUGUI[] tempTexts = cardActedUpon.GetComponentsInChildren<TextMeshProUGUI>(true);
        for (int i = 0; i < tempTexts.Length; i++)
        {
            if (tempTexts[i].name.Equals("Description Text"))
            {
                tempTexts[i].color = Color.blue;
                tempTexts[i].text = "<size=600%>+" + cardActedUpon.DefenseHealth;
            }
        }
    }
}
