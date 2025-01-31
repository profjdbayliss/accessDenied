using Mirror.Examples.Pong;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum UICommandType
{
    ShowDiscard,
    HideDiscard
}

public class UserInterface : MonoBehaviour
{
    // things to be set
    public GameObject AllDiscardElements;
    public GameObject DiscardDrop;
    public float AnimDuration = 0.4f;

    // place to send commands
    Queue<UICommandType> commands = new Queue<UICommandType>(10);

    // coroutine for discard animation
    private Coroutine discardMoveRoutine;
    private readonly Vector2 discardHiddenPos = new Vector2(-709, 500);
    private readonly Vector2 discardVisiblePos = new Vector2(-709, -224);
    RectTransform discardRect;

    public void AddCommand(UICommandType command)
    {
        commands.Enqueue(command);
    }

    //// Start is called before the first frame update
    void Start()
    {
        discardRect = DiscardDrop.GetComponent<RectTransform>();
        discardMoveRoutine = null;
    }

    // Update is called once per frame
    void Update()
    {
        if (commands.Count > 0)
        {
            int count = commands.Count;
            for(int i=0; i<count; i++)
            {
                UICommandType command = commands.Dequeue();
                switch (command)
                {
                    case UICommandType.ShowDiscard:
                        //AllDiscardElements.SetActive(true);
                        EnableDiscardDrop();
                        break;
                    case UICommandType.HideDiscard:
                        //AllDiscardElements.SetActive(false);
                        DisableDiscardDrop();
                        break;
                    default:
                        break;
                }
            }
            
        }
    }

    public void EnableDiscardDrop()
    {
        if (discardMoveRoutine != null) StopCoroutine(discardMoveRoutine);
       
        discardMoveRoutine =
            StartCoroutine(
                MoveMenuItem(discardRect, discardVisiblePos, AnimDuration));
        Debug.Log("move in routine enabled");
    }
    public void DisableDiscardDrop()
    {
        if (discardMoveRoutine != null) StopCoroutine(discardMoveRoutine);
        discardMoveRoutine =
            StartCoroutine(
                MoveMenuItem(discardRect, discardHiddenPos, AnimDuration));
        Debug.Log("move disable routine started");
    }

    private IEnumerator MoveMenuItem(RectTransform transform, Vector2 finalPos, float duration)
    {
        var initPos = transform.anchoredPosition;
        float time = 0;
        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            t = CubicEaseInOut(t);
            transform.anchoredPosition = Vector2.Lerp(initPos, finalPos, t);
            yield return null;
        }
        transform.anchoredPosition = finalPos;

    }

    private float CubicEaseInOut(float t)
    {
        if (t < 0.5f)
        {
            return 4f * t * t * t;
        }
        else
        {
            float f = (2f * t) - 2f;
            return 0.5f * f * f * f + 1f;
        }
    }
}
