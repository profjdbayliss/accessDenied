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

       foreach (int attackCardID in cardActedUpon.AttackingCards)
        {
            Debug.Log("checking for attack card " + attackCardID);
            opponentCardObject = GameManager.instance.GetOpponentActiveCardObject(attackCardID);
            if (opponentCardObject != null)
            {
                Debug.Log("opponent card object obtained!");
                opponentCard = opponentCardObject.GetComponent<Card>();
                if (card.CanMitigate(opponentCard.front.title))
                {
                    canMitigate = true;
                    Debug.Log(attackName + " can be mitigated by " + card.front.title);
                    break;
                }
            }
           
        }

       if (canMitigate)
        {
            // get the right card from the station to clear it from the station
            // and send it to opponent's discard pile
            GameManager.instance.DiscardOpponentActiveCard(cardActedUpon.UniqueID, opponentCard.data.cardID, true);

            // put our card in the discard pile as well
            player.DiscardSingleActiveCard(cardActedUpon.UniqueID, card.data.cardID, false);

        }

    }
    public void Canceled(CardPlayer player, Card cardActedUpon, Card card)
    {
       // we never cancel mitigations
       // if we had to we'd have to cache the info for the card in this class
    }
}
