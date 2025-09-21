
// Paste into SnakeController.cs (replace the whole SyncWorldBuffers method)
private void SyncWorldBuffers(bool first)
{
    int len = cells.Count;
    // remember previous length to detect newly-added tail entry
    int prevLen = currW.Count;

    EnsureBufLen(len);

    for (int i = 0; i < len; i++)
    {
        var w = (Vector2)board.CellToWorld(cells[i]);
        if (first)
        {
            prevW[i] = w;
            currW[i] = w;
        }
        else
        {
            if (i >= prevLen)
            {
                // NEW segment (grew this frame): start it already at its target so it doesn't lerp from (0,0)
                prevW[i] = w;
                currW[i] = w;
            }
            else
            {
                prevW[i] = currW[i];
                currW[i] = w;
            }
        }
    }
    EnsureSegments(len);
}
