﻿using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIUnitIcon : UIButtonHover, IPointerClickHandler
{
	public event System.Action OnClick;

	public TMP_Text text;
	public Image icon;
	public Vector3 anchoredPosition
	{
		get => rTransform.anchoredPosition;
		set => rTransform.anchoredPosition = value;
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		OnClick?.Invoke();
	}

	public void ClearClickEvents() => OnClick = null;

	public override void ClearAllEvents()
	{
		base.ClearAllEvents();
		ClearClickEvents();
	}

}
