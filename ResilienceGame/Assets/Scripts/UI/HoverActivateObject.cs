using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class HoverActivateObject : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public GameObject targetObject;
    public float delay = 0.5f; 
    public float maxHeightOffset = 100;
    private Vector3 originalPosition;
    private float timer = 0; 
    private bool isHovering = false; 
    private bool isScaled = false;

    void Start()
    {
        originalPosition = transform.position;
    }
    void Update()
    {
        //will scale the object that is being hovered over and will count a timer until it shows extra card info. 
        if (isHovering)
        {
            if (!isScaled) ScaleCard(.5f);

            timer += Time.deltaTime;  
            if (timer >= delay)
            {
                targetObject.SetActive(true);
                if(targetObject.transform.localPosition.y < originalPosition.y + maxHeightOffset)
                {
                    targetObject.transform.localPosition += new Vector3(0, 1, 0);
                }
            }
        }
        //toggles the scaling effect to scale it back to its original size
        else if (isScaled) ScaleCard(-.5f);
    }

    public void ScaleCard(float scaleAmount)
    {
        if(!isScaled)
        this.gameObject.layer = 30; 
        else
        {
            this.gameObject.layer = 6;
        }

        Vector2 tempScale = targetObject.transform.parent.localScale;
        Vector3 offset = targetObject.transform.parent.localPosition;
        tempScale.x = (float)(targetObject.transform.parent.localScale.x + scaleAmount);
        tempScale.y = (float)(targetObject.transform.parent.localScale.y + scaleAmount);
        offset.y = offset.y + scaleAmount * 200;

        targetObject.transform.parent.localScale = tempScale;
        targetObject.transform.parent.localPosition = offset;
        
        isScaled = !isScaled;
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true; 
        timer = 0;     
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;       
        timer = 0;
        targetObject.transform.SetLocalPositionAndRotation(new Vector3(), targetObject.transform.rotation);
        targetObject.SetActive(false);
    }
}
