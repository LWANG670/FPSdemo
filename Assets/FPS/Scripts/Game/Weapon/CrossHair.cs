using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrossHair : MonoBehaviour
{
	[Tooltip("�ӵ���׼��ɫ")]
	public Texture2D crosshairTexture;
	[Tooltip("�ӵ�������ɫ")]
	public Texture2D hitTexture;
	[Tooltip("��׼�߳���")]
	public int crosshairLength = 20;
	[Tooltip("��׼�߿��")]
	public int crosshairWidth = 4;
	[Tooltip("��׼�ߺ�����������")]
	public float startingCrosshairSize = 10.0f;
	private Texture2D curTexture;//��ǰ��ɫ
	private float currentCrosshairSize;//�ӵ�����������������λ��                 														
	void Start()
    {
		currentCrosshairSize = startingCrosshairSize;
		curTexture = crosshairTexture;
	}

    // Update is called once per frame
    void Update()
    {
        
    }
	void OnGUI()
	{
		Vector2 center = new Vector2(Screen.width / 2, Screen.height / 2);
		// Left
		Rect leftRect = new Rect(center.x - crosshairLength - currentCrosshairSize, center.y - (crosshairWidth / 2), crosshairLength, crosshairWidth);
		GUI.DrawTexture(leftRect, curTexture, ScaleMode.StretchToFill);
		// Right
		Rect rightRect = new Rect(center.x + currentCrosshairSize, center.y - (crosshairWidth / 2), crosshairLength, crosshairWidth);
		GUI.DrawTexture(rightRect, curTexture, ScaleMode.StretchToFill);
		// Top
		Rect topRect = new Rect(center.x - (crosshairWidth / 2), center.y - crosshairLength - currentCrosshairSize, crosshairWidth, crosshairLength);
		GUI.DrawTexture(topRect, curTexture, ScaleMode.StretchToFill);
		// Bottom
		Rect bottomRect = new Rect(center.x - (crosshairWidth / 2), center.y + currentCrosshairSize, crosshairWidth, crosshairLength);
		GUI.DrawTexture(bottomRect, curTexture, ScaleMode.StretchToFill);
	}

	public void onHit()
	{
		curTexture = hitTexture;
		Invoke("changeTexture", 0.1f);
	}

	public void changeTexture()
	{
		curTexture = crosshairTexture;
	}
}
