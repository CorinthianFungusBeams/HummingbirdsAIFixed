using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlowerArea : MonoBehaviour
{
    //diameter of area where agent and flower can be

    public const float AreaDiameter = 20f;

    //list of all flower plants in a flower area (plants have multiple flowers)
    private List<GameObject> flowerPlants;

    //lookup dict for looking up flower from nectar collider
    private Dictionary<Collider, Flower> nectarFlowerDictionary;

    //list of all flowers in flower area
    public List<Flower> Flowers { get; private set;}


    public void ResetFlowers()
    {
        //reset the flowers!

        //Rotate the plants
        foreach (GameObject flowerPlant in flowerPlants)
        {
            float xRotation = UnityEngine.Random.Range(-5f,5f);
            float yRotation = UnityEngine.Random.Range(-180f,180f);
            float zRotation = UnityEngine.Random.Range(-5f,5f);
            flowerPlant.transform.localRotation = Quaternion.Euler(xRotation,yRotation,zRotation);
        }

        foreach (Flower flower in Flowers)
        {
            flower.ResetFlower();
        }
    }

    public Flower GetFlowerFromNectar(Collider collider)
    {
        return nectarFlowerDictionary[collider];
    }

    private void Awake()
    {
        //init vars
        flowerPlants = new List<GameObject>();
        nectarFlowerDictionary = new Dictionary<Collider,Flower>();
        Flowers = new List<Flower>();
    }

    private void Start()
    {
        //find all the child flowers
        FindChildFlowers(transform);
    }

    private void FindChildFlowers(Transform parent)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);

            if (child.CompareTag("flower_plant"))
            {
                //flower plant found and detected
                flowerPlants.Add(child.gameObject);

                FindChildFlowers(child);
            }
            else
            {
                //look for flower component
                Flower flower = child.GetComponent<Flower>();
                if (flower != null)
                {
                    //add to flower list
                    Flowers.Add(flower);

                    nectarFlowerDictionary.Add(flower.nectarCollider, flower);
                }
                else
                {
                    FindChildFlowers(child);
                }
            }
        }
    }
}
