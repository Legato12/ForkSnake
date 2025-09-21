using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class BoardBackground : MonoBehaviour
{
    [SerializeField] private Board board;
    private SpriteRenderer sr;

    private void Awake(){ sr = GetComponent<SpriteRenderer>(); Apply(); }
    private void OnValidate(){ if (sr == null) sr = GetComponent<SpriteRenderer>(); Apply(); }

    private void Apply()
    {
        if (board == null || sr == null) return;
        sr.drawMode = SpriteDrawMode.Tiled;
        float w = (board.borderX * 2 + 1) * board.tileWorldSize;
        float h = (board.borderY * 2 + 1) * board.tileWorldSize;
        sr.size = new Vector2(w, h);
        sr.sortingOrder = -10;
        transform.position = Vector3.zero;
        if (sr.sprite == null)
        {
            // make safe sprite
            var tex = new Texture2D(16,16);
            for (int y=0;y<16;y++) for (int x=0;x<16;x++) tex.SetPixel(x,y, new Color(0.07f,0.09f,0.12f));
            tex.Apply();
            sr.sprite = Sprite.Create(tex, new Rect(0,0,tex.width,tex.height), new Vector2(0.5f,0.5f), tex.width);
        }
    }
}
