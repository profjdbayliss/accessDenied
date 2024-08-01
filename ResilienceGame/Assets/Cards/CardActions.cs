using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ActionAddDefenseWorthToStation : ICardAction
{
    public void Played(CardPlayer player, CardPlayer opponent, Card cardActedUpon, Card card)
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
 
}

public class ActionMitigateCard : ICardAction
{
    public void Played(CardPlayer player, CardPlayer opponent, Card cardActedUpon, Card card)
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
            GameObject tmpCardObject = opponent.GetActiveCardObject(attackingCardInfo);
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
                    if (tmpCard.data.worth <= cardValue)
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
            opponentCard.state = CardState.CardNeedsToBeDiscarded;
           
            // put our card in the discard pile as well
            card.state = CardState.CardNeedsToBeDiscarded;

        }

    }
}


public class ActionImpactFacilityWorth : ICardAction
{
    public void Played(CardPlayer player, CardPlayer opponent, Card cardActedUpon, Card card)
    {
        Debug.Log("card " + card.front.title + " played to attack the selected station.");
        cardActedUpon.DefenseHealth += card.data.worth;
        player.AddAttackUpdateToList(new AttackUpdate
        {
            UniqueFacilityID=cardActedUpon.UniqueID,
            ChangeInValue=card.data.worth
        });
        card.state = CardState.CardNeedsToBeDiscarded;
        TextMeshProUGUI[] tempTexts = cardActedUpon.GetComponentsInChildren<TextMeshProUGUI>(true);
        for (int i = 0; i < tempTexts.Length; i++)
        {
            if (tempTexts[i].name.Equals("Description Text"))
            {
                tempTexts[i].color = Color.red;
                tempTexts[i].text = "<size=600%>" + cardActedUpon.DefenseHealth;
            }
        }
    }
}

// a lateral movement card adds this action
// to the attack card that was played before it
public class ActionLateralMovement: ICardAction
{
    public void Played(CardPlayer player, CardPlayer opponent, Card cardActedUpon, Card card)
    {
        Debug.Log("lateral movement card played on station in zone: " + card.WhichFacilityZone);
        card.state = CardState.CardNeedsToBeDiscarded;

        // for each connection to this facility
        // play this attack card
        foreach (FacilityConnectionInfo connections in cardActedUpon.ConnectionList)
        {
            GameObject connectedFacility = player.ActiveFacilities[connections.UniqueFacilityID];
            if (connectedFacility != null)
            {
                
                Card connectedCard = connectedFacility.GetComponent<Card>();
                Debug.Log("lateral movement played on zone " + connectedCard.WhichFacilityZone);
                card.Play(player, opponent, connectedCard);
            }
        }
         
    }

}