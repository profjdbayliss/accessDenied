using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ElectricityDistribution : FacilityV3
{
    // Start is called before the first frame update
    override public void Start()
    {
        base.Start();
        SetMaterial();
        FindFacilities();
        Invoke("SetFacilityData", 5);
    }
   
    override public void SetFacilityData()
    {
        feedback = Random.Range(1, 11);
        hardness = Random.Range(1, 11); //hardness vulnerability to cyber
        maintenance = Random.Range(1, 11); //maintenance(Age) vulnerability to natural disaster or physical threat
        type = Type.ElectricityDistribution;

        // internal
        workers = FindClosestFacility<ElectricityGeneration>().workers;
        it_level = FindClosestFacility<ElectricityGeneration>().it_level;
        ot_level = FindClosestFacility<ElectricityGeneration>().ot_level;
        phys_security = FindClosestFacility<ElectricityGeneration>().phys_security;
        funding = FindClosestFacility<ElectricityGeneration>().funding;
        
        //external
        electricity = FindClosestFacility<ElectricityGeneration>().electricity;
        water = FindClosestFacility<ElectricityGeneration>().water;
        fuel = FindClosestFacility<ElectricityGeneration>().fuel;
        communications = FindClosestFacility<ElectricityGeneration>().communications;
        commodities = FindClosestFacility<ElectricityGeneration>().commodities;
        health = FindClosestFacility<ElectricityGeneration>().health;
        security = FindClosestFacility<Security>().security;
        public_goods = FindClosestFacility<ElectricityGeneration>().public_goods;
        city_resource = FindClosestFacility<ElectricityGeneration>().city_resource;
        
        FindClosestFacility<Water>();
        FindClosestFacility<Fuel>();
        FindClosestFacility<Communications>();
        FindClosestFacility<Commodity>();
        FindClosestFacility<Health>();
        FindClosestFacility<Security>();
        FindClosestFacility<PublicGoods>();

        //FindClosestFacilityWater();
        //FindClosestFacilityFuel();
        //FindClosestFacilityComms();
        //FindClosestFacilityCommodities();
        //FindClosestFacilityHealth();
        //FindClosestFacilitySec();
        //FindClosestFacilityPG();


        // //internal
        // workers = FindClosestFacilityElectricity().workers;
        // it_level = FindClosestFacilityElectricity().it_level;
        // ot_level = FindClosestFacilityElectricity().ot_level;
        // phys_security = FindClosestFacilityElectricity().phys_security;
        // funding = FindClosestFacilityElectricity().funding;
        // 
        // //external
        // electricity = FindClosestFacilityElectricity().electricity;
        // water = FindClosestFacilityElectricity().water;
        // fuel = FindClosestFacilityElectricity().fuel;
        // communications = FindClosestFacilityElectricity().communications;
        // commodities = FindClosestFacilityElectricity().commodities;
        // health = FindClosestFacilityElectricity().health;
        // security = FindClosestFacilityElectricity().security;
        // public_goods = FindClosestFacilityElectricity().public_goods;
        // city_resource = FindClosestFacilityElectricity().city_resource;
        // 
        // FindClosestFacilityWater();
        // FindClosestFacilityFuel();
        // FindClosestFacilityComms();
        // FindClosestFacilityCommodities();
        // FindClosestFacilityHealth();
        // FindClosestFacilitySec();
        // FindClosestFacilityPG();

        //if (FindClosestFacilityElectricity().workers <= 0 || FindClosestFacilityElectricity().it_level <= 0 || FindClosestFacilityElectricity().ot_level <= 0 || FindClosestFacilityElectricity().phys_security <= 0 || FindClosestFacilityElectricity().funding <= 0
        //    || FindClosestFacilityElectricity().electricity <= 0 || FindClosestFacilityElectricity().water <= 0 || FindClosestFacilityElectricity().fuel <= 0 || FindClosestFacilityElectricity().communications <= 0 || FindClosestFacilityElectricity().commodities <= 0 || FindClosestFacilityElectricity().health <= 0 || FindClosestFacilityElectricity().security <= 0 || FindClosestFacilityElectricity().public_goods <= 0)
        //{
        //    Invoke("SearchAgain", 3);
        //}


        output_flow = 0f;

        Update();
    }

    //void SearchAgain()
    //{
    //    workers = FindClosestFacilityElectricity().workers;
    //    it_level = FindClosestFacilityElectricity().it_level;
    //    ot_level = FindClosestFacilityElectricity().ot_level;
    //    phys_security = FindClosestFacilityElectricity().phys_security;
    //    funding = FindClosestFacilityElectricity().funding;

    //    //external
    //    electricity = FindClosestFacilityElectricity().electricity;
    //    water = FindClosestFacilityElectricity().water;
    //    fuel = FindClosestFacilityElectricity().fuel;
    //    communications = FindClosestFacilityElectricity().communications;
    //    commodities = FindClosestFacilityElectricity().commodities;
    //    health = FindClosestFacilityElectricity().health;
    //    security = FindClosestFacilityElectricity().security;
    //    public_goods = FindClosestFacilityElectricity().public_goods;
    //    city_resource = FindClosestFacilityElectricity().city_resource;
    //}

    override public void Update()
    {
        if (this.gameObject.name.Contains("Clone"))
        {
            FeedbackPanel();
            CalculateFlow();
        }
    }
}
