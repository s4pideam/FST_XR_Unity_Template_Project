using System.Collections;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;
using UnityEngine;

public class TouchScript : MonoBehaviour, IMixedRealityTouchHandler
{
    private Color _color;

    private ToolTip  _tooltip;
    // Start is called before the first frame update
    void Start()
    {
        _color = gameObject.GetComponent<MeshRenderer>().material.color;
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void setToolTop(ToolTip tooltip)
    {
        _tooltip = tooltip;
    }

    public void OnTouchStarted(HandTrackingInputEventData eventData)
    {
        _tooltip.gameObject.SetActive(true);
        _tooltip.ToolTipText = gameObject.GetComponent<TooltipData>().show();
        gameObject.GetComponent<MeshRenderer>().material.color = Color.red;
    }

    public void OnTouchCompleted(HandTrackingInputEventData eventData)
    {
        _tooltip.gameObject.SetActive(false);
        _tooltip.ToolTipText = "";
        gameObject.GetComponent<MeshRenderer>().material.color = _color;
    }

    public void OnTouchUpdated(HandTrackingInputEventData eventData)
    {

    }
}