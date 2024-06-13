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
    CardInPlay,
    CardDiscarded
};

public class Card : MonoBehaviour, IDropHandler
{
    public CardData data;
    public CardFront front;
    public CardState state;
    public GameObject cardPlayedZone;
    public GameObject handDropZone;
    public GameObject discardDropZone;
    public GameObject gameCanvas;
    public GameObject originalParent;
    public Vector3 originalPosition;
  
    // Start is called before the first frame update
    void Start()
    {
        //img = this.gameObject.GetComponent<RawImage>();
        //Debug.Log("Card Made");
        //reader = GameObject.Find("Card Reader").GetComponent<CardReader>();
        //handDropZone = this.gameObject.transform.parent.gameObject;
        //originalParent = this.gameObject.transform.parent.transform.parent.gameObject;
        //this.gameObject.transform.localScale = Vector3.one;
        //gameCanvas = GameObject.Find("Central Map");
        originalPosition = this.gameObject.transform.position;
        //this.gameObject.GetComponent<HoverScale>().UpdateOriginalPosition(originalPosition);
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (this.state == CardState.CardDrawn) // Make sure that the card actually has a reference to the card drop location where it will be dropped and that it is currently in the players hand
        {
            Debug.Log("drawn card dropped!");
            // Get the bounds of the played card Drop Zone
            Vector2 cardDropMin = new Vector2();
            cardDropMin.x = cardPlayedZone.GetComponent<RectTransform>().localPosition.x - (cardPlayedZone.GetComponent<RectTransform>().rect.width / 2);
            cardDropMin.y = cardPlayedZone.GetComponent<RectTransform>().localPosition.y - (cardPlayedZone.GetComponent<RectTransform>().rect.height / 2);
            Vector2 cardDropMax = new Vector2();
            cardDropMax.x = cardPlayedZone.GetComponent<RectTransform>().localPosition.x + (cardPlayedZone.GetComponent<RectTransform>().rect.width / 2);
            cardDropMax.y = cardPlayedZone.GetComponent<RectTransform>().localPosition.y + (cardPlayedZone.GetComponent<RectTransform>().rect.height / 2);
            this.gameObject.transform.SetParent(originalParent.transform, true); // Now set the card's parent to be the player they are attached to instead of the hand zone

            Vector2 discardDropMin = new Vector2();
            discardDropMin.x = discardDropZone.GetComponent<RectTransform>().localPosition.x - (discardDropZone.GetComponent<RectTransform>().rect.width / 2);
            discardDropMin.y = discardDropZone.GetComponent<RectTransform>().localPosition.y - (discardDropZone.GetComponent<RectTransform>().rect.height / 2);
            Vector2 discardDropMax = new Vector2();
            discardDropMax.x = discardDropZone.GetComponent<RectTransform>().localPosition.x + (discardDropZone.GetComponent<RectTransform>().rect.width / 2);
            discardDropMax.y = discardDropZone.GetComponent<RectTransform>().localPosition.y + (discardDropZone.GetComponent<RectTransform>().rect.height / 2);


            // DO a AABB collision test to see if the card is on the card drop

            if (this.transform.localPosition.y < cardDropMax.y &&
               this.transform.localPosition.y > cardDropMin.y &&
               this.transform.localPosition.x < cardDropMax.x &&
               this.transform.localPosition.x > cardDropMin.x)
            {
                Debug.Log("card dropped in card played zone");
                //// check the cards teamID to see which team they belong to so they can call the proper Select facility method to then see if they have met all conditions to play the card
                //if (this.data.teamID == 0)
                //{
                //    //if (this.gameObject.GetComponentInParent<CardPlayer>().SelectFacility(this.data.cardID))
                //    //{
                //    //    //if (FindObjectOfType<GameManager>()) // Reduce funds of the local player when play a card
                //    //    //{
                //    //    //    //GameManager gm = FindObjectOfType<GameManager>();
                //    //    //    ////gm.AddFunds(-100);
                //    //    //    //foreach (var facility in this.gameObject.GetComponentInParent<CardPlayer>().facilitiesActedUpon)
                //    //    //    //{
                //    //    //    //    facility.GetComponent<FacilityV3>().health += 20;
                //    //    //    //    if (facility.GetComponent<FacilityV3>().health > 100)
                //    //    //    //    {
                //    //    //    //        facility.GetComponent<FacilityV3>().health = 100;
                //    //    //    //    }
                //    //    //    //    facility.GetComponent<FacilityV3>().Health.text = facility.GetComponent<FacilityV3>().health.ToString();
                //    //    //    //}
                //    //    //    //List<FacilityV3Info> tempFacs = new List<FacilityV3Info>();
                //    //    //    //Debug.Log("Facilities Count: " + gm.allFacilities.Count + ", " + teamID);
                //    //    //    //for (int i = 0; i < gm.allFacilities.Count; i++)
                //    //    //    //{
                //    //    //    //    tempFacs.Add(gm.allFacilities[i].GetComponent<FacilityV3>().ToFacilityV3Info());
                //    //    //    //}
                //    //    //    //RGNetworkPlayerList.instance.AskUpdateFacilities(tempFacs); //Update facilities' info
                //    //    //}
                //    //    this.state = CardState.CardInPlay;
                //    //    this.gameObject.GetComponentInParent<slippy>().enabled = false;
                //    //    // Set the time the card is to be disposed of by adding the duration of the card to the current turn count
                //    //}
                //    //else
                //    //{
                //        this.gameObject.transform.SetParent(handDropZone.transform, false);

                //    //}
                //}
                //else if (this.data.teamID == 1)
                //{
                //    if (this.gameObject.GetComponentInParent<CardPlayer>().SelectFacility(this.data.cardID))
                //    {
                //        //if (FindObjectOfType<GameManager>()) // Reduce funds of the local player when play a card
                //        //{
                //        //    //GameManager gm = FindObjectOfType<GameManager>();
                //        //    //gm.AddFunds(-100);
                //        //    //foreach (var facility in this.gameObject.GetComponentInParent<CardPlayer>().facilitiesActedUpon)
                //        //    //{
                //        //    //    facility.GetComponent<FacilityV3>().health -= 30;
                //        //    //    if (facility.GetComponent<FacilityV3>().health <= 0)
                //        //    //    {
                //        //    //        gm.EndGame(1, true);
                //        //    //        //RGNetworkPlayerList.instance.CmdEndGame(1);
                //        //    //    }
                //        //    //    facility.GetComponent<FacilityV3>().Health.text = facility.GetComponent<FacilityV3>().health.ToString();
                //        //    //}

                //        //    //List<FacilityV3Info> tempFacs = new List<FacilityV3Info>();
                //        //    //Debug.Log("Facilities Count: " + gm.allFacilities.Count + ", " + teamID);
                //        //    //for (int i = 0; i < gm.allFacilities.Count; i++)
                //        //    //{
                //        //    //    tempFacs.Add(gm.allFacilities[i].GetComponent<FacilityV3>().ToFacilityV3Info());
                //        //    //}
                //        //    //RGNetworkPlayerList.instance.AskUpdateFacilities(tempFacs); //Update facilities' info
                //        //}
                //        this.state = CardState.CardInPlay;
                //        this.gameObject.GetComponentInParent<slippy>().enabled = false;
                //        this.gameObject.GetComponentInParent<slippy>().ResetScale();
                //        Debug.Log("card reset scale done");
                //    }
                //    else
                //    {
                //        this.gameObject.transform.SetParent(handDropZone.transform, false);
                //    }
                //}
            }
            else if (this.transform.localPosition.y < discardDropMax.y &&
               this.transform.localPosition.y > discardDropMin.y &&
               this.transform.localPosition.x < discardDropMax.x &&
               this.transform.localPosition.x > discardDropMin.x)
            {
                Debug.Log("card dropped in discard zone");
                if (GameManager.instance.isDiscardAllowed)
                {
                    this.state = CardState.CardDiscarded;
                    this.gameObject.GetComponentInParent<slippy>().enabled = false;
                    this.gameObject.GetComponentInParent<slippy>().ResetScale();
                    this.gameObject.transform.SetParent(discardDropZone.transform, false);
                    GameManager.instance.HandleDiscard(data.cardID);
                    Debug.Log("discard should have been done now");
                }
            }
            else { 
                Debug.Log("card not dropped in card drop zone");
                // If it fails, parent it back to the hand location and then set its state to be in hand and make it grabbable again
                this.gameObject.transform.SetParent(handDropZone.transform, false);
                this.state = CardState.CardDrawn;
                this.gameObject.GetComponentInParent<slippy>().enabled = true;
                //this.gameObject.GetComponent<HoverScale>().ResetPosition(this.gameObject.transform.localPosition);
                this.gameObject.GetComponent<HoverScale>().Drop();
                Debug.Log("card reset position done");
            }
        }
    }
}