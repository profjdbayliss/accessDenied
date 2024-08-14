using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Facility : MonoBehaviour
{
    public enum FacilityName
    {
        Production,
        Transmission,
        Distribution
    };

    public enum SectorProduct
    {
        Energy,
        Water,
        Agricultural,
        Transportation,
        Manufacturing,
        Government,
        Defense,
        Healthcare,
        EmergencyServices,
        Information,
        Technology,
        Financial,
        Commercial,
        Nuclear,
        Dams,
        Communications,
        Chemical,
    };

    public FacilityName facilityName;
    public SectorProduct[] products;
    private int physicalPoints, finacialPoints, networkPoints;
    [SerializeField] private GameObject facilityCanvas;
    private TextMeshProUGUI[] pointsUI;

    // TODO: Effect class and reference here
    // For now it is just written as two bools
    public bool hasBackdoor;
    public bool hasFortify;

    public bool isDown;

    // Start is called before the first frame update
    void Start()
    {
        for(int i = 0; i < facilityCanvas.transform.childCount; i++)
        {
            pointsUI[i] = facilityCanvas.transform.GetChild((int)facilityName).Find("Points").GetChild(i).GetComponentInChildren<TextMeshProUGUI>();
        }
        pointsUI[0].text = physicalPoints.ToString();
        pointsUI[1].text = finacialPoints.ToString();
        pointsUI[2].text = networkPoints.ToString();
    }

    public void ChangeFacilityPoints(string[] targets, int value)
    {
        foreach(string target in targets)
        {
            switch (target)
            {
                case "physical": physicalPoints += value;
                        break;
                case "finacial": finacialPoints += value;
                    break;
                case "network": networkPoints += value;
                    break;
            }
        }
    }
}
