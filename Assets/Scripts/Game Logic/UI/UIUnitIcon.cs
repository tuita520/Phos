﻿using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIUnitIcon : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
	public event System.Action OnMouseEnter;
	public event System.Action OnMouseExit;
	public event System.Action OnClick;

	public TMP_Text text;
	public Image icon;
	public Vector3 anchoredPosition
	{
		get => _rTransform.anchoredPosition;
		set => _rTransform.anchoredPosition = value;
	}


	private RectTransform _rTransform;

	void Awake()
	{
		_rTransform = GetComponent<RectTransform>();
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		OnMouseEnter?.Invoke();
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		OnMouseExit?.Invoke();
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		OnClick?.Invoke();
	}
}
