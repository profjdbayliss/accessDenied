using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class HoverScale : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public GameObject targetObject;
    public float delay = 0.5f; 
    public float maxHeightOffset = 100;
    private float timer = 0; 
    private bool isHovering = false; 
    private bool isScaled = false;
    private bool wasDropped = false;
    public Vector2 previousScale = Vector2.zero;
    void Start()
    {
        previousScale = this.gameObject.transform.localScale;
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
                //if(targetObject.transform.localPosition.y < originalPosition.y + maxHeightOffset)
                //{
                //    targetObject.transform.localPosition += new Vector3(0, 1, 0);
                //}
            }
        }
        //toggles the scaling effect to scale it back to its original size
        else if (isScaled && wasDropped) { ResetScale(); wasDropped = false; }
        else if (isScaled) { ScaleCard(-.5f); }
        //else if (isScaled) { ResetScale(); ResetPosition(originalPosition); }
    }

    public void ScaleCard(float scaleAmount)
    {
        if(!isScaled)
        this.gameObject.layer = 30; 
        else
        {
            this.gameObject.layer = 6;
        }

        Vector2 tempScale = targetObject.transform.localScale;
        Vector3 offset = targetObject.transform.localPosition;
        previousScale = tempScale;
        tempScale.x = (float)(targetObject.transform.localScale.x + scaleAmount);
        tempScale.y = (float)(targetObject.transform.localScale.y + scaleAmount);
        offset.y = offset.y + scaleAmount * 200;

        targetObject.transform.localScale = tempScale;
        targetObject.transform.localPosition = offset;
        
        isScaled = !isScaled;
        //Debug.Log("scaled card by " + scaleAmount);
    }
    public void ResetScale()
    {
        isScaled = false;
        targetObject.transform.localScale = previousScale;
    }
    public void Drop()
    {
        wasDropped = true;
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
    }

}
