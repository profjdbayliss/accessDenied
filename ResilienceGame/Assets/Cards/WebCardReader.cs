using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class WebCardReader : MonoBehaviour
{
    //// are we done reading the file yet?
    //public bool IsDone = false;

    //// deck name to use for the deck
    //public string DeckName;

    //// filename - directory path is assumed to be Application.streamingAssetsPath
    //// extension is assumed to be csv
    //public string cardFileName;

    //// output atlas filename
    //public string outputAtlasName;

    //// filename + directory path
    //protected string fileLocation;

    //// card prefab that all cards are made from
    //public GameObject cardPrefab;

    //// the card id is static specifically so we no longer have to worry about
    //// conflicting card id's for different deck files.
    //protected static int sCardID = 0;
    //protected bool isRunning = false;

    //protected string allCardText = "";

    //// card info
    //public List<Card> Cards = new List<Card>(50);

    //// should we create an atlas?
    //public bool CreateAtlas = false;

    //bool mFileRead = false;
    //bool mImageFileRead = false;
    //byte[] mImageBytes = null;

    //public void CSVRead(bool createAtlas)
    //{
    //    Texture2D tex = new Texture2D(1, 1);
        

    //    // Split the read in CSV file into seperate objects at the new line character
    //    string[] allCSVObjects = allCardText.Split("\n");
    //        //Debug.Log("Number of lines in csv file is: " + allCSVObjects.Length);

    //        // get all the image elements in the csv file
    //        // the web version doesn't allow the creation of an image atlas and 
    //        // ignores the var
    //        if (!tex.LoadImage(mImageBytes))
    //    {
    //        Debug.Log("atlas image wasn't properly loaded!");
    //    }
        

    //    // get all the textual elements in the csv file
    //    // NOTE: row 0 is always headings and not data
    //    for (int i = 1; i < allCSVObjects.Length; i++)
    //        {
    //            // Then in each of the lines of csv data, split them based on commas to get the different pieces of information on each object
    //            // and instantiate a base card object to then fill in with data.
    //            string[] individualCSVObjects = allCSVObjects[i].Split(",");
    //            if (individualCSVObjects.Length > 1) // excel adds an empty line at the end
    //            {
    //                // columns in the spreadsheet: changes depending on game
    //                // 0:  how many cards of this type in the deck
    //                // 1:  type of the card
    //                // 2: played on (some cards are only played on a specific infrastructure type)
    //                // 3:  title
    //                // 4:  title color
    //                // 5:  card image
    //                // 6:  background image
    //                // 7:  does it have a worth circle?
    //                // 8:  what's in the worth circle?
    //                // 9:  cost of card if there is one
    //                // 10: the category of the card if there is one
    //                // 11: column
    //                // 12: row
    //                // 13: mitigates
    //                // 14+:  text description

    //                // 0: if there's one or more cards to be inserted into the deck
    //                int numberOfCards = int.Parse(individualCSVObjects[0].Trim());
    //                if (numberOfCards > 0)
    //                {

    //                    // get appropriate game objects to set up
    //                    GameObject tempCardObj = Instantiate(cardPrefab);

    //                    // Get a reference to the Card component on the card gameobject.
    //                    Card tempCard = tempCardObj.GetComponent<Card>();
    //                    tempCard.data.DeckName = DeckName;
    //                    CardFront tempCardFront = tempCard.GetComponent<CardFront>();
    //                    tempCard.data.data.cardID = sCardID;
    //                    sCardID++;

    //                    // 1: which type of card is this?
    //                    // NOTE: here is where we add appropriate card actions
    //                    // WORK: just add appropriate action to the actions list
    //                    string type = individualCSVObjects[1].Trim();
    //                    switch (type)
    //                    {
    //                        case "Defense":
    //                            tempCard.data.cardType = CardType.Defense;
    //                            tempCardFront.cardType = CardType.Defense;
    //                            break;
    //                        case "Mitigation":
    //                            tempCard.data.cardType = CardType.Mitigation;
    //                            tempCardFront.cardType = CardType.Mitigation;
    //                            break;
    //                        case "Vulnerability":
    //                            tempCard.data.cardType = CardType.Vulnerability;
    //                            tempCardFront.cardType = CardType.Vulnerability;
    //                            break;
    //                        case "Station":
    //                            tempCard.data.cardType = CardType.Station;
    //                            tempCardFront.cardType = CardType.Station;
    //                            break;
    //                        case "Instant":
    //                            tempCard.data.cardType = CardType.Instant;
    //                            tempCardFront.cardType = CardType.Instant;
    //                            break;
    //                        case "Special":
    //                            tempCard.data.cardType = CardType.Special;
    //                            tempCardFront.cardType = CardType.Special;
    //                            break;
    //                        default:
    //                            break;
    //                    }

    //                    // 2: is this card only played on a specific player type?
    //                    string onlyPlayedOn = individualCSVObjects[2].Trim();
    //                    switch (onlyPlayedOn)
    //                    {
    //                        case "any":
    //                            tempCard.data.onlyPlayedOn = PlayerType.Any;
    //                            break;
    //                        case "power":
    //                            tempCard.data.onlyPlayedOn = PlayerType.Energy;
    //                            break;
    //                        case "water":
    //                            tempCard.data.onlyPlayedOn = PlayerType.Water;
    //                            break;
    //                        default:
    //                            break;
    //                    }

    //                    // 3: set up the card title
    //                    // WORK: do we really need to set both of these?
    //                    tempCardObj.name = individualCSVObjects[3];
    //                    tempCardFront.title = tempCardObj.name;

    //                    // 4: set up the title color, which also 
    //                    // determines the card type in this game
    //                    // NOTE: the format required by the physical card game program
    //                    // is a bit different than Unity's format, which requires the 
    //                    // # sign rather than just straight hex code.
    //                    // so we change it appropriately
    //                    string[] htmlColorInfo = individualCSVObjects[4].Trim().Split("x");
    //                    string htmlColor = "";
    //                    if (htmlColorInfo.Length == 2)
    //                    {
    //                        htmlColor = "#" + htmlColorInfo[1];
    //                    }
    //                    Color titleColor;
    //                    bool success = ColorUtility.TryParseHtmlString(htmlColor, out titleColor);
    //                    if (success)
    //                    {
    //                        tempCardFront.titleColor = titleColor;
    //                    }
    //                    else
    //                    {
    //                        Debug.Log("title color wasn't parsed " + individualCSVObjects[4].Trim());
    //                    }


    //                    // 5: card image
    //                    Texture2D tex3 = new Texture2D(TextureAtlas.SIZE, TextureAtlas.SIZE); // This needs to match the textureatlas pixel width
    //                    string imageFilename = individualCSVObjects[5].Trim();
    //                Debug.Log("image name is :" + imageFilename + " col and row are " + individualCSVObjects[11] + ":" + individualCSVObjects[12]);

    //                if (!imageFilename.Equals(string.Empty) && !imageFilename.Equals(""))
    //                {
    //                    int col = int.Parse(individualCSVObjects[11].Trim());
    //                    int row = int.Parse(individualCSVObjects[12].Trim());
    //                    //Debug.Log("col is " + col + " row is " + row);

    //                    Color[] tempColors = tex.GetPixels((col * TextureAtlas.SIZE), (row * TextureAtlas.SIZE), TextureAtlas.SIZE, TextureAtlas.SIZE); // This needs to match the textureatlas pixel width
    //                    tex3.SetPixels(tempColors);
    //                    tex3.Apply();

    //                }
    //                    tempCardFront.img = tex3;

    //                    // 6: card background
    //                    // we're currently ignoring this as it's set inside
    //                    // the unity editor

    //                    // 13: mitigation list
    //                    string[] mitigations = individualCSVObjects[13].Trim().Split(";");
    //                    foreach (string mitigation in mitigations)
    //                    {
    //                        if (!mitigation.Equals(""))
    //                        {
    //                            tempCard.MitigatesWhatCards.Add(mitigation);
    //                        }
    //                    }

    //                    // 14: text description
    //                    // pick up any extra things with commas in them that got incorrectly separated
    //                    tempCardFront.description = individualCSVObjects[14];

    //                    if (individualCSVObjects.Length > 14)
    //                    {
    //                        // means the text description itself contains commas, which are our
    //                        // separator. So now put these pieces together!
    //                        StringBuilder fullDescription = new StringBuilder(tempCardFront.description);

    //                        int extras = individualCSVObjects.Length - 15; // this is one more than the # we have
    //                                                                       //Debug.Log("Extra commas: length should be " + individualCSVObjects.Length + " with extras as " + extras);
    //                        for (int j = 0; j < extras; j++)
    //                        {
    //                            fullDescription.Append("," + individualCSVObjects[14 + j]);
    //                        }
    //                        tempCardFront.description = fullDescription.ToString();
    //                    }

    //                    // 8:  does it have a worth circle?
    //                    if (individualCSVObjects[7].Equals(string.Empty) ||
    //                        individualCSVObjects[7].Equals(""))
    //                    {
    //                        tempCardFront.worthCircle = false;
    //                        tempCard.data.worth = 0;
    //                    }
    //                    else
    //                    {
    //                        //Debug.Log("worth circle is true and will be: " + individualCSVObjects[8].Trim());
    //                        tempCardFront.worthCircle = true;
    //                        // 8:  what's in the worth circle?
    //                        tempCard.data.worth = int.Parse(individualCSVObjects[8].Trim());
    //                    }

    //                    // 9:  cost of card if there is one
    //                    if (individualCSVObjects[9].Equals(string.Empty) ||
    //                        individualCSVObjects[9].Equals(""))
    //                    {
    //                        tempCardFront.costCircle = false;
    //                        tempCard.data.cost = 0;
    //                    }
    //                    else
    //                    {
    //                        tempCardFront.costCircle = true;
    //                        // 9:  what's in the worth circle?
    //                        tempCard.data.cost = int.Parse(individualCSVObjects[9].Trim());
    //                    }

    //                    // 10: the category of the card if there is one
    //                    // WORK - not in simpler version of initial game

    //                    // now add one copy of this card for every instance in the card game
    //                    tempCard.data.numberInDeck = numberOfCards;

    //                    // now add to the deck of single types of cards

    //                    Cards.Add(tempCard);
    //                }
    //            }

    //        }
            
    //    IsDone = true;
    //}

    //public void Update()
    //{
    //    if (IsDone)
    //    {
    //        // most likely case for the whole game

    //    }
    //    else
    //    if (!isRunning)
    //    {
    //        isRunning = true;
    //        // Check to see if the file exists
    //        fileLocation = Application.streamingAssetsPath + "/" + cardFileName;
    //        Debug.Log("trying to read file at location: " + fileLocation);
    //        StartCoroutine(GetRequest(fileLocation));
    //        StartCoroutine(GetImageAtlasRequest(Application.streamingAssetsPath + "/" + outputAtlasName));
    //    }
    //    else if (mFileRead && mImageFileRead)
    //    {
    //        CSVRead(CreateAtlas);
    //        // make sure we don't read the file multiple times
    //        mFileRead = false;
    //        mImageFileRead = false;
    //    }
    //}

    //IEnumerator GetRequest(string uri)
    //{
    //    using (UnityWebRequest webRequest = UnityWebRequest.Get(uri))
    //    {
    //        // Request and wait for the desired page.
    //        yield return webRequest.SendWebRequest();

    //        string[] pages = uri.Split('/');
    //        int page = pages.Length - 1;

    //        switch (webRequest.result)
    //        {
    //            case UnityWebRequest.Result.ConnectionError:
    //            case UnityWebRequest.Result.DataProcessingError:
    //                Debug.LogError(pages[page] + ": Error: " + webRequest.error);
    //                break;
    //            case UnityWebRequest.Result.ProtocolError:
    //                Debug.LogError(pages[page] + ": HTTP Error: " + webRequest.error);
    //                break;
    //            case UnityWebRequest.Result.Success:
    //                Debug.Log(pages[page] + ":\nReceived");
    //                allCardText = webRequest.downloadHandler.text;
    //                mFileRead = true;
    //                break;
    //        }
    //    }
    //}

    //IEnumerator GetImageAtlasRequest(string uri)
    //{
    //    using (UnityWebRequest webRequest = UnityWebRequest.Get(uri))
    //    {
    //        // Request and wait for the desired page.
    //        yield return webRequest.SendWebRequest();

    //        string[] pages = uri.Split('/');
    //        int page = pages.Length - 1;

    //        switch (webRequest.result)
    //        {
    //            case UnityWebRequest.Result.ConnectionError:
    //            case UnityWebRequest.Result.DataProcessingError:
    //                Debug.LogError(pages[page] + ": Error: " + webRequest.error);
    //                break;
    //            case UnityWebRequest.Result.ProtocolError:
    //                Debug.LogError(pages[page] + ": HTTP Error: " + webRequest.error);
    //                break;
    //            case UnityWebRequest.Result.Success:
    //                Debug.Log(pages[page] + ":\nReceived");
    //                mImageBytes = webRequest.downloadHandler.nativeData.ToArray();
    //                Debug.Log("number of bytes in image is: " + mImageBytes.Length);
    //                mImageFileRead = true;
    //                break;
    //        }
    //    }
    //}
}
