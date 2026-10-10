using System.Collections;
using UnityEngine;

public class ColorFlash : MonoBehaviour
{
    [SerializeField] private Color flashColor = Color.white;
    private float maxFlashTime = 1.0f;

    private SpriteRenderer[] spriteRenderers;
    private Material[] spriteMaterials;

    private Coroutine colorFlash;

    private bool flashing = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        GrabMat();
    }

    void GrabMat()
    {
        spriteMaterials = new Material[spriteRenderers.Length];
        for (int i = 0;  i < spriteRenderers.Length; i++)
        {
            spriteMaterials[i] = spriteRenderers[i].material;
        }
    }

    void OnEnable()
    {
        if (flashing)
        {
            if (colorFlash != null)
            {
                StopCoroutine(colorFlash);
            }
            SetTrans(0);
            flashing = false;
        }
    }

    public void CallFlash(Color flashTint, float flashTime)
    {
        flashColor = flashTint;
        maxFlashTime = flashTime;
        if(colorFlash == null)
        {
            colorFlash = StartCoroutine(FlashColor());
        }
        else
        {
            SetTrans(0);
            StopCoroutine(colorFlash);
            colorFlash = StartCoroutine(FlashColor());
        }
    }


    IEnumerator FlashColor()
    {
        flashing = true;
        SetColor();
        float currentFlashAmount = 0;
        float flashTime = 0f;
        while (flashTime < maxFlashTime)
        {
            flashTime += Time.deltaTime;
            currentFlashAmount = Mathf.Lerp(1, 0, flashTime / maxFlashTime);
            SetTrans(currentFlashAmount);
            yield return null;
        }
        flashing = false;
    }

    private void SetColor()
    {
        foreach(Material mat in spriteMaterials)
        {
            mat.SetColor("_FlashColor", flashColor);
        }
    }

    private void SetTrans(float transAmount)
    {
        foreach (Material mat in spriteMaterials)
        {
            mat.SetFloat("_FlashTrans", transAmount);
        }
    }
}
