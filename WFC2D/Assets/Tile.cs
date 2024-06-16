using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Tile : MonoBehaviour
{
    SpriteRenderer spriteRenderer;
    TextMesh text;

    void Awake()
    {
        this.spriteRenderer = GetComponent<SpriteRenderer>();
        this.text = GetComponentInChildren<TextMesh>();
    }

    public void SetSprite(Sprite sprite)
    {
        spriteRenderer.color = Color.white;
        spriteRenderer.sprite = sprite;
        this.text.text = "";
    }

    public void SetColor(Color color)
    {
        spriteRenderer.color = color;
    }

    public void SetText(string text)
    {
        this.text.text = text;
    }
}
