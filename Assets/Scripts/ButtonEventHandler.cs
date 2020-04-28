using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

public class ButtonEventHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public UnityAction<PointerEventData> onPointerDown;
    public UnityAction<PointerEventData> onPointerUp;
    public void OnPointerDown(PointerEventData eventData) => onPointerDown?.Invoke(eventData);
    public void OnPointerUp(PointerEventData eventData) => onPointerUp?.Invoke(eventData);
}
