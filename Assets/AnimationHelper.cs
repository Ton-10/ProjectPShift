using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationHelper : MonoBehaviour
{ 
    public void Invis()
    {
        gameObject.transform.parent.GetComponent<CarController>().PauseDisentegrate();
    }

    public void End()
    {
        gameObject.transform.parent.GetComponent<CarController>().HideDisentegrate();
    }
}
