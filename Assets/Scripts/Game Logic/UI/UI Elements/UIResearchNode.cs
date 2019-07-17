﻿using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIResearchNode : UIHover
{
	public TMP_Text titleText;
	public Image icon;
	public RectTransform costDisplay;
	public Button button;
	public Outline outline;
	public RectTransform resourceCostPrefab;
	public int nodeId;


	private RectTransform _thisRect;
	private UIResearchResource[] _uIResearchResources;

	protected override void Awake()
	{
		base.Awake();
		_thisRect = GetComponent<RectTransform>();
	}

	public void SetAnchoredPos(Vector3 pos)
	{
		_thisRect.anchoredPosition = pos;
	}

	public void SetSize(Vector2 size)
	{
		_thisRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);
		_thisRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
	}

	public void InitResources(ResourceIndentifier[] resources)
	{
		if (_uIResearchResources == null)
			_uIResearchResources = new UIResearchResource[resources.Length];
		if (_uIResearchResources.Length < resources.Length)
			Array.Resize(ref _uIResearchResources, resources.Length);
		for (int i = 0; i < resources.Length; i++)
		{
			if (_uIResearchResources[i] != null)
				continue;
			var uiRR = _uIResearchResources[i] = Instantiate(resourceCostPrefab, costDisplay).GetComponent<UIResearchResource>();
			uiRR.SetSize(costDisplay.rect.height, costDisplay.rect.height);
			uiRR.icon.sprite = ResourceDatabase.GetSprite(resources[i].id);
		}
	}

	public void UpdateProgress(ResourceIndentifier[] resources, int[] progress)
	{
		for (int i = 0; i < _uIResearchResources.Length; i++)
		{
			if(i >= resources.Length)
			{
				_uIResearchResources[i]?.gameObject.SetActive(false);
				continue;
			}
			_uIResearchResources[i].gameObject.SetActive(true);
			_uIResearchResources[i].UpdateData(progress[i], (int)resources[i].ammount, 0);
		}
	}
}
