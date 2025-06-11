using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class Flower : MonoBehaviour
{
    public Color fullFlowerColor = new Color(1f,0,.3f);

    public Color emptyFlowerColor = new Color(.5f,0,1f);

    public Collider nectarCollider;

    // the solid collider representing the flower petals
    private Collider flowerCollider;

    //flower's material
    private Material flowerMaterial;

    public Vector3 FlowerUpVector
    {
        get
        {
            return nectarCollider.transform.up;
        }
    }

    public Vector3 FlowerCenterPosition
    {
        get
        {
            return nectarCollider.transform.position;
        }
    }

    public float NectarAmount { get; private set;}

    public bool HasNectar
    {
        get
        {
            return NectarAmount > 0f;
        }
    }

    public float Feed(float amount)
    {
        float nectarTaken = Mathf.Clamp(amount,0f,NectarAmount);

        NectarAmount -= amount;

        if (NectarAmount <= 0)
        {
            NectarAmount=0;

            flowerCollider.gameObject.SetActive(false);
            nectarCollider.gameObject.SetActive(false);

            flowerMaterial.SetColor("_BaseColor", emptyFlowerColor);
        }

        return nectarTaken;
    }

    //reset flower
    public void ResetFlower()
    {
        //refill nectar
        NectarAmount = 1f;

        //enable colldier
        flowerCollider.gameObject.SetActive(true);
        nectarCollider.gameObject.SetActive(true);

        //change flower color
        flowerMaterial.SetColor("_BaseColor", fullFlowerColor);
    }

    //flower wakes up
    private void Awake()
    {
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        flowerMaterial = meshRenderer.material;

        flowerCollider = transform.Find("FlowerCollider").GetComponent<Collider>();
        nectarCollider = transform.Find("FlowerNectarCollider").GetComponent<Collider>();
    }


}



