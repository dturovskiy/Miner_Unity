using UnityEngine;

public class Crack : MonoBehaviour
{
    public Sprite crackSprite1;
    public Sprite crackSprite2;
    public Sprite crackSprite3;

    public void HitCrack(int hit)
    {
        switch (hit)
        {
            case 1:
                GetComponent<SpriteRenderer>().sprite = crackSprite3;
                break;
            case 2:
                GetComponent<SpriteRenderer>().sprite = crackSprite2;
                break;
            case 3:
                GetComponent<SpriteRenderer>().sprite = crackSprite1;
                break;
            case 0:
                Destroy(gameObject);
                break;
        }
    }
}
