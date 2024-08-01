using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text;
using System;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Networking;
using Mirror;
using System.Net;
using System.Threading.Tasks;
using System.Net.Http;

public class CardReader : MonoBehaviour
{
    // are we done reading the file yet?
    public bool IsDone = false;

    // deck name to use for the deck
    public string DeckName;

    // filename - directory path is assumed to be Application.streamingAssetsPath
    // extension is assumed to be csv
    public string cardFileName;

    // output atlas filename
    public string outputAtlasName;

    // filename + directory path
    protected string fileLocation;

    // card prefab that all cards are made from
    public GameObject cardPrefab;

    // the card id is static specifically so we no longer have to worry about
    // conflicting card id's for different deck files.
    protected static int sCardID = 0;
    protected bool isRunning = false;
   
    protected string allCardText = "";

    // card info
    public List<Card> Cards = new List<Card>(50);

    // should we create an atlas?
    public bool CreateAtlas = false;

    public void CSVRead(bool createAtlas)
    {
        if (File.Exists(fileLocation))
        {

            FileStream stream = File.OpenRead(fileLocation);
            TextReader reader = new StreamReader(stream);
            Texture2D tex = new Texture2D(1, 1);
            allCardText = reader.ReadToEnd();

            // Split the read in CSV file into seperate objects at the new line character
            string[] allCSVObjects = allCardText.Split("\n");
            Debug.Log("Number of lines in csv file is: " + allCSVObjects.Length);

            // get all the image elements in the csv file
            if (createAtlas)
            {

                // Make sure to get the atlas first, as we only need to query it once. 
                TextureAtlas currentAtlas = new TextureAtlas();

                // make a list of all filenames
                List<string> filenames = new List<string>(50);
                for (int i = 1; i < allCSVObjects.Length; i++)
                {
                    string[] singleLineCSVObjects = allCSVObjects[i].Split(",");
                    if (singleLineCSVObjects.Length > 1) // excel adds an empty line at the end
                    {
                        Debug.Log("number of items in a line is: " + singleLineCSVObjects.Length);
                        if (!singleLineCSVObjects[5].Equals(string.Empty) && !singleLineCSVObjects[5].Equals(""))
                        {
                            filenames.Add(singleLineCSVObjects[5].Trim());
                        }
                        else
                        {
                            filenames.Add(string.Empty);
                        }
                    }
                }

                // create atlas and load back in from image
                currentAtlas.CreateAtlasFromFilenameList("images/", outputAtlasName, filenames);
                byte[] tempBytes = File.ReadAllBytes(Application.streamingAssetsPath + "/" + outputAtlasName);
                tex.LoadImage(tempBytes);
            }
            else
            {
                byte[] tempBytes = File.ReadAllBytes(Application.streamingAssetsPath + "/" + outputAtlasName);
                tex.LoadImage(tempBytes);
            }

            // get all the textual elements in the csv file
            // NOTE: row 0 is always headings and not data
            for (int i = 1; i < allCSVObjects.Length; i++)
            {
                // Then in each of the lines of csv data, split them based on commas to get the different pieces of information on each object
                // and instantiate a base card object to then fill in with data.
                string[] individualCSVObjects = allCSVObjects[i].Split(",");
                if (individualCSVObjects.Length > 1) // excel adds an empty line at the end
                {
                    // columns in the spreadsheet: changes depending on game
                    // 0:  how many cards of this type in the deck
                    // 1:  type of the card
                    // 2: played on (some cards are only played on a specific infrastructure type)
                    // 3:  title
                    // 4:  title color
                    // 5:  card image
                    // 6:  background image
                    // 7:  does it have a worth circle?
                    // 8:  what's in the worth circle?
                    // 9:  cost of card if there is one
                    // 10: the category of the card if there is one
                    // 11: column
                    // 12: row
                    // 13: mitigates
                    // 14+:  text description

                    // 0: if there's one or more cards to be inserted into the deck
                    int numberOfCards = int.Parse(individualCSVObjects[0].Trim());
                    if (numberOfCards > 0)
                    {

                        // get appropriate game objects to set up
                        GameObject tempCardObj = Instantiate(cardPrefab);

                        // Get a reference to the Card component on the card gameobject.
                        Card tempCard = tempCardObj.GetComponent<Card>();
                        tempCard.DeckName = DeckName;
                        CardFront tempCardFront = tempCard.GetComponent<CardFront>();
                        tempCard.data.cardID = sCardID;
                        sCardID++;

                        // 1: which type of card is this?
                        // NOTE: here is where we add appropriate card actions
                        // WORK: just add appropriate action to the actions list
                        string type = individualCSVObjects[1].Trim();
                        switch (type)
                        {
                            case "Defense":
                                tempCard.data.cardType = CardType.Defense;
                                tempCardFront.cardType = CardType.Defense;
                                tempCard.ActionList.Add(new ActionAddDefenseWorthToStation());
                                break;
                            case "Mitigation":
                                tempCard.data.cardType = CardType.Mitigation;
                                tempCardFront.cardType = CardType.Mitigation;
                                tempCard.ActionList.Add(new ActionMitigateCard());
                                break;
                            case "Vulnerability":
                                tempCard.data.cardType = CardType.Vulnerability;
                                tempCardFront.cardType = CardType.Vulnerability;
                                tempCard.ActionList.Add(new ActionImpactFacilityWorth());
                                break;                          
                            case "Station":
                                tempCard.data.cardType = CardType.Station;
                                tempCardFront.cardType = CardType.Station;
                                break;
                            case "Instant":
                                tempCard.data.cardType = CardType.Instant;
                                tempCardFront.cardType = CardType.Instant;
                                tempCard.ActionList.Add(new ActionImpactFacilityWorth());
                                break;
                            case "Halt":
                                tempCard.data.cardType = CardType.Halt;
                                tempCardFront.cardType = CardType.Halt;
                                tempCard.ActionList.Add(new ActionMitigateCard());
                                break;
                            case "Lateral Movement":
                                tempCard.data.cardType = CardType.LateralMovement;
                                tempCardFront.cardType = CardType.LateralMovement;
                                tempCard.ActionList.Add(new ActionLateralMovement());
                                break;
                            case "Special":
                                tempCard.data.cardType = CardType.Special;
                                tempCardFront.cardType = CardType.Special;
                                break;
                            default:
                                break;
                        }

                        // 2: is this card only played on a specific player type?
                        string onlyPlayedOn = individualCSVObjects[2].Trim();
                        switch (onlyPlayedOn)
                        {
                            case "any":
                                tempCard.data.onlyPlayedOn = PlayerType.Any;
                                break;
                            case "power":
                                tempCard.data.onlyPlayedOn = PlayerType.Energy;
                                break;
                            case "water":
                                tempCard.data.onlyPlayedOn = PlayerType.Water;
                                break;
                            default:
                                break;
                        }

                        // 3: set up the card title
                        // WORK: do we really need to set both of these?
                        tempCardObj.name = individualCSVObjects[3];
                        tempCardFront.title = tempCardObj.name;

                        // 4: set up the title color, which also 
                        // determines the card type in this game
                        // NOTE: the format required by the physical card game program
                        // is a bit different than Unity's format, which requires the 
                        // # sign rather than just straight hex code.
                        // so we change it appropriately
                        string[] htmlColorInfo = individualCSVObjects[4].Trim().Split("x");
                        string htmlColor = "";
                        if (htmlColorInfo.Length == 2)
                        {
                            htmlColor = "#" + htmlColorInfo[1];
                        }
                        Color titleColor;
                        bool success = ColorUtility.TryParseHtmlString(htmlColor, out titleColor);
                        if (success)
                        {
                            tempCardFront.titleColor = titleColor;
                        }
                        else
                        {
                            Debug.Log("title color wasn't parsed " + individualCSVObjects[4].Trim());
                        }


                        // 5: card image
                        Texture2D tex3 = new Texture2D(TextureAtlas.SIZE, TextureAtlas.SIZE); // This needs to match the textureatlas pixel width
                        string imageFilename = individualCSVObjects[5].Trim();
                        //Debug.Log("image name is :" + imageFilename + " col and row are " + individualCSVObjects[11] + ":" + individualCSVObjects[12]);

                        if (!imageFilename.Equals(string.Empty) && !imageFilename.Equals(""))
                        {
                            int col = int.Parse(individualCSVObjects[11].Trim());
                            int row = int.Parse(individualCSVObjects[12].Trim());
                            //Debug.Log("col is " + col + " row is " + row);

                            Color[] tempColors = tex.GetPixels((col * TextureAtlas.SIZE), (row * TextureAtlas.SIZE), TextureAtlas.SIZE, TextureAtlas.SIZE); // This needs to match the textureatlas pixel width
                            tex3.SetPixels(tempColors);
                            tex3.Apply();

                        }
                        tempCardFront.img = tex3;

                        // 6: card background
                        // we're currently ignoring this as it's set inside
                        // the unity editor

                        // 13: mitigation list
                        string[] mitigations = individualCSVObjects[13].Trim().Split(";");
                        foreach (string mitigation in mitigations)
                        {
                            if (!mitigation.Equals(""))
                            {
                                tempCard.MitigatesWhatCards.Add(mitigation);
                            }
                        }

                        // 14: text description
                        // pick up any extra things with commas in them that got incorrectly separated
                        tempCardFront.description = individualCSVObjects[14];

                        if (individualCSVObjects.Length > 14)
                        {
                            // means the text description itself contains commas, which are our
                            // separator. So now put these pieces together!
                            StringBuilder fullDescription = new StringBuilder(tempCardFront.description);

                            int extras = individualCSVObjects.Length - 15; // this is one more than the # we have
                                                                           //Debug.Log("Extra commas: length should be " + individualCSVObjects.Length + " with extras as " + extras);
                            for (int j = 0; j < extras; j++)
                            {
                                fullDescription.Append("," + individualCSVObjects[14 + j]);
                            }
                            tempCardFront.description = fullDescription.ToString();
                        }

                        // 8:  does it have a worth circle?
                        if (individualCSVObjects[7].Equals(string.Empty) ||
                            individualCSVObjects[7].Equals(""))
                        {
                            tempCardFront.worthCircle = false;
                            tempCard.data.worth = 0;
                        }
                        else
                        {
                            //Debug.Log("worth circle is true and will be: " + individualCSVObjects[8].Trim());
                            tempCardFront.worthCircle = true;
                            // 8:  what's in the worth circle?
                            tempCard.data.worth = int.Parse(individualCSVObjects[8].Trim());
                        }

                        // 9:  cost of card if there is one
                        if (individualCSVObjects[9].Equals(string.Empty) ||
                            individualCSVObjects[9].Equals(""))
                        {
                            tempCardFront.costCircle = false;
                            tempCard.data.cost = 0;
                        }
                        else
                        {
                            tempCardFront.costCircle = true;
                            // 9:  what's in the worth circle?
                            tempCard.data.cost = int.Parse(individualCSVObjects[9].Trim());
                        }

                        // 10: the category of the card if there is one
                        // WORK - not in simpler version of initial game

                        // now add one copy of this card for every instance in the card game
                        tempCard.data.numberInDeck = numberOfCards;

                        // now add to the deck of single types of cards

                        Cards.Add(tempCard);
                    }
                }

            }
            // Close at the end
            reader.Close();
            stream.Close();
        }
        else
        {
            Debug.Log("file doesn't exist at the proper location.");
        }

        IsDone = true;
    }

    public void Update()
    {
        if (IsDone)
        {
            // most likely case for the whole game

        } else 
        if (!isRunning)
        {
            isRunning = true;
            // Check to see if the file exists
            fileLocation = Application.streamingAssetsPath + "/" + cardFileName;
            Debug.Log("trying to read file at location: " + fileLocation);
            CSVRead(CreateAtlas);
            IsDone = true;
        }
    }

}
