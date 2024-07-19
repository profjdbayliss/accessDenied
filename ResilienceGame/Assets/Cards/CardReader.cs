using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text;
using System;
using UnityEngine.UI;

public class CardReader : MonoBehaviour
{
    // TODO: Currently used in two objects, one for each of Jessica's decks

    // deck name to use for the deck
    public string DeckName;

    // filename - directory path is assumed to be Application.streamingAssetsPath
    // extension is assumed to be csv
    public string cardFileName;

    // output atlas filename
    public string outputAtlasName;

    // filename + directory path
    string fileLocation;

    // card prefab that all cards are made from
    public GameObject cardPrefab;

    // the card id is static specifically so we no longer have to worry about
    // conflicting card id's for different deck files.
    private static int sCardID = 0;

    public List<Card> CSVRead(bool createAtlas)
    {
        List<Card> cards = new List<Card>(50); // TODO: Get number per deck

        // Check to see if the file exists
        fileLocation = Application.streamingAssetsPath + "/" + cardFileName;
        //Debug.Log("trying to read file at location: " + fileLocation);
        if (File.Exists(fileLocation))
        {
            //Debug.Log("reading the file!");
            FileStream stream = File.OpenRead(fileLocation);
            TextReader reader = new StreamReader(stream);
            Texture2D tex = new Texture2D(1, 1);
            string allCardText = reader.ReadToEnd();

            // Split the read in CSV file into seperate objects at the new line character
            string[] allCSVObjects = allCardText.Split("\n");
            //Debug.Log("Number of lines in csv file is: " + allCSVObjects.Length);

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
                        //Debug.Log("number of items in a line is: " + singleLineCSVObjects.Length);
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
                //TODO: Create check for deck type

                // Then in each of the lines of csv data, split them based on commas to get the different pieces of information on each object
                // and instantiate a base card object to then fill in with data.
                string[] individualCSVObjects = allCSVObjects[i].Split(","); 
                if (individualCSVObjects.Length > 1) // excel adds an empty line at the end
                {
                    // TODO: update list and numbering

                    // columns in the spreadsheet: changes depending on game
                    //  0:  Team of the card // TODO: Split ; in Team
                    //  1:  How many cards of this type in the deck
                    //  2:  Method called
                    //  3:  Target Type
                    //  4:  Played on (some cards are only played on a specific infrastructure type)
                    //  5:  Amount of targets
                    //  6:  Title
                    //  7:  Background color
                    //  8:  Card image
                    //  9:  col
                    // 10:  row
                    // 11:  Background image
                    // 12:  col
                    // 13:  row
                    // 14:  Meeple type changed
                    // 15:  Number of Meeples changed
                    // 16:  Meeple cost
                    // 17:  Blue cost
                    // 18:  Black cost
                    // 19:  Purple cost
                    // 20:  Network damage
                    // 21:  Physical damage
                    // 22:  Financial damage
                    // 23:  Cards drawn
                    // 24:  Cards discarded
                    // 25:  Cards shuffled
                    // 26:  Effect placed
                    // 27:  Effect removed
                    // 28:  Duration
                    // 29:  Amount playable on a turn
                    // 30:  Effect during doom clock
                    // 31:  Dice roll minimum
                    // 32:  Text description

                    // 0: Read only cards of the correct team
                    if (individualCSVObjects[0].Trim().ToLower() != DeckName.ToLower())
                    {
                        continue;
                    }

                    Debug.Log(DeckName);

                    // 1:if there's one or more cards to be inserted into the deck
                    int numberOfCards = int.Parse(individualCSVObjects[1].Trim());
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

                        // 2: which type of card is this?
                        // NOTE: here is where we add appropriate card actions
                        // WORK: just add appropriate action to the actions list
                        string type = individualCSVObjects[2].Trim();
                        switch (type)
                        {
                            // TODO: Should be used to assign method called on played
                            // TODO: Should be converted to match card player line 350
                            /*case "Defense":
                                tempCard.data.cardType = CardType.Defense;
                                tempCardFront.cardType = CardType.Defense;
                                break;
                            case "Mitigation":
                                tempCard.data.cardType = CardType.Mitigation;
                                tempCardFront.cardType = CardType.Mitigation;
                                break;
                            case "Vulnerability":
                                tempCard.data.cardType = CardType.Vulnerability;
                                tempCardFront.cardType = CardType.Vulnerability;
                                break;
                            case "Station":
                                tempCard.data.cardType = CardType.Station;
                                tempCardFront.cardType = CardType.Station;
                                break;
                            case "Instant":
                                tempCard.data.cardType = CardType.Instant;
                                tempCardFront.cardType = CardType.Instant;
                                break;
                            case "Special":
                                tempCard.data.cardType = CardType.Special;
                                tempCardFront.cardType = CardType.Special;
                                break;*/
                            default:
                                tempCard.data.cardType = CardType.None;
                                tempCardFront.cardType = CardType.None;
                                break;
                        }

                        // 3: Target type
                        string target = individualCSVObjects[3].Trim();
                        switch (target)
                        {
                            /*case "any":
                                tempCard.data.onlyPlayedOn = PlayerType.Any;
                                break;
                            case "power":
                                tempCard.data.onlyPlayedOn = PlayerType.Energy;
                                break;
                            case "water":
                                tempCard.data.onlyPlayedOn = PlayerType.Water;
                                break;*/
                            default:
                                tempCard.data.onlyPlayedOn = PlayerTeam.Any;
                                break;
                        }

                        // 4: is this card only played on a specific player type?
                        string onlyPlayedOn = individualCSVObjects[4].Trim();
                        switch (onlyPlayedOn)
                        {
                            // TODO: This should be sector names
                            case "any":
                                tempCard.data.onlyPlayedOn = PlayerTeam.Any;
                                break;
                            case "energy":
                                //tempCard.data.onlyPlayedOn = PlayerTeam.Red;
                                break;
                            case "water":
                                //tempCard.data.onlyPlayedOn = PlayerTeam.Water;
                                break;
                            default:
                                tempCard.data.onlyPlayedOn = PlayerTeam.Any;
                                break;
                        }

                        // 5: Amount of possible targets

                        // 6: set up the card title
                        // WORK: do we really need to set both of these?
                        tempCardObj.name = individualCSVObjects[6];
                        tempCardFront.title = tempCardObj.name;

                        // 7: set up the title color, which also 
                        // determines the card type in this game
                        // NOTE: the format required by the physical card game program
                        // is a bit different than Unity's format, which requires the 
                        // # sign rather than just straight hex code.
                        // so we change it appropriately

                        // TODO: check this effect
                        string[] htmlColorInfo = individualCSVObjects[7].Trim().Split("x");
                        string htmlColor="";
                        if (htmlColorInfo.Length == 2)
                        {
                            htmlColor = "#" + htmlColorInfo[1];
                        }
                        Color titleColor;
                        bool success = ColorUtility.TryParseHtmlString(htmlColor, out titleColor);
                        if (success)
                        {
                            tempCardFront.titleColor = titleColor;
                        } else
                        {
                            Debug.Log("title color wasn't parsed " + individualCSVObjects[4].Trim());
                        }
                        

                        // 8: card image
                        Texture2D tex3 = new Texture2D(TextureAtlas.SIZE, TextureAtlas.SIZE); // This needs to match the textureatlas pixel width
                        string imageFilename = individualCSVObjects[8].Trim();
                        //Debug.Log("image name is :" + imageFilename + " col and row are " + individualCSVObjects[11] + ":" + individualCSVObjects[12]);

                        if (!imageFilename.Equals(string.Empty) && !imageFilename.Equals(""))
                        {
                            int col = int.Parse(individualCSVObjects[9].Trim());
                            int row = int.Parse(individualCSVObjects[10].Trim());
                            //Debug.Log("col is " + col + " row is " + row);

                            Color[] tempColors = tex.GetPixels((col * TextureAtlas.SIZE), (row * TextureAtlas.SIZE), TextureAtlas.SIZE, TextureAtlas.SIZE); // This needs to match the textureatlas pixel width
                            tex3.SetPixels(tempColors);
                            tex3.Apply();

                        }
                        tempCardFront.img = tex3;

                        // 11: card background 12/13
                        // we're currently ignoring this as it's set inside
                        // the unity editor
                        // TODO: If needed set programmatically

                        // 14:  Meeple type changed
                        // 15:  Number of Meeples changed
                        // 16:  Meeple cost
                        // 17:  Blue cost
                        // 18:  Black cost
                        // 19:  Purple cost
                        // 20:  Network damage
                        // 21:  Physical damage
                        // 22:  Financial damage
                        // 23:  Cards drawn
                        // 24:  Cards discarded
                        // 25:  Cards shuffled
                        // 26:  Effect placed
                        // 27:  Effect removed
                        // 28:  Duration
                        // 29:  Amount playable on a turn
                        // 30:  Effect during doom clock
                        // 31:  Dice roll minimum

                        // 32: text description



                        // pick up any extra things with commas in them that got incorrectly separated
                        tempCardFront.description = individualCSVObjects[29]; // TODO: OOI err

                        if (individualCSVObjects.Length > 29)
                        {
                            // means the text description itself contains commas, which are our
                            // separator. So now put these pieces together!
                            StringBuilder fullDescription = new StringBuilder(tempCardFront.description);

                            int extras = individualCSVObjects.Length - 14; // this is one more than the # we have
                                                                           //Debug.Log("Extra commas: length should be " + individualCSVObjects.Length + " with extras as " + extras);
                            for (int j = 0; j < extras; j++)
                            {
                                fullDescription.Append("," + individualCSVObjects[14 + j]);
                            }
                            tempCardFront.description = fullDescription.ToString();
                        }

                        /*
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
                        }*/

                        // 10: the category of the card if there is one
                        // WORK - not in simpler version of initial game

                        // 14: mitigation list TODO
                        /*string[] mitigations = individualCSVObjects[14].Trim().Split(";");
                        foreach(string mitigation in mitigations)
                        {
                            if(!mitigation.Equals(""))
                            {
                                tempCard.MitigatesWhatCards.Add(mitigation);// TODO: Remove mitigation
                            }       
                        }*/

                        // now add one copy of this card for every instance in the card game
                        tempCard.data.numberInDeck = numberOfCards;

                        // now add to the deck of single types of cards
                        cards.Add(tempCard);
                    }
                }

            }
            // Close at the end
            reader.Close();
            stream.Close();

        }

        return cards;
    }
}
