using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

namespace SnakeGame.Powerups
{
    public class PowerupStash : MonoBehaviour
    {
        [System.Serializable]
        public class Entry
        {
            public PowerupSO data;
            public int count;
        }

        public int maxPerType = 1;
        public Transform container;
        public GameObject stashItemButtonPrefab;

        private Dictionary<PowerupType, Entry> _byType = new Dictionary<PowerupType, Entry>();

        public void Add(PowerupSO so)
        {
            if (so == null) return;
            Entry e;
            if (!_byType.TryGetValue(so.type, out e))
            {
                e = new Entry();
                e.data = so;
                e.count = 0;
                _byType.Add(so.type, e);
            }
            if (e.count >= maxPerType) return;
            e.count += 1;
            RefreshUI();
        }

        public void RemoveOne(PowerupSO so)
        {
            if (so == null) return;
            Entry e;
            if (_byType.TryGetValue(so.type, out e))
            {
                e.count -= 1;
                if (e.count < 0) e.count = 0;
                RefreshUI();
            }
        }

        private void RefreshUI()
        {
            if (container == null || stashItemButtonPrefab == null) return;
            for (int i = container.childCount - 1; i >= 0; i--)
                GameObject.Destroy(container.GetChild(i).gameObject);

            foreach (var kv in _byType)
            {
                Entry e = kv.Value;
                if (e.count <= 0) continue;
                GameObject go = GameObject.Instantiate(stashItemButtonPrefab, container);
                StashItemUI ui = go.GetComponent<StashItemUI>();
                if (ui == null) ui = go.AddComponent<StashItemUI>();
                ui.Bind(this, e);
            }
        }

        public void OnClickUse(Entry e)
        {
            if (e == null || e.data == null) return;
            PowerupSystem ps = PowerupSystem.Instance;
            if (ps != null)
            {
                ps.ActivateFromStash(e.data);
            }
        }
    }

    public class StashItemUI : MonoBehaviour
    {
        public Image icon;
        public Text countText;
        private PowerupStash _stash;
        private PowerupStash.Entry _entry;

        public void Bind(PowerupStash stash, PowerupStash.Entry e)
        {
            _stash = stash;
            _entry = e;

            if (icon == null) icon = GetComponentInChildren<Image>();
            if (countText == null) countText = GetComponentInChildren<Text>();

            if (icon != null && e.data != null) icon.sprite = e.data.sprite;
            if (countText != null) countText.text = e.count.ToString();
        }

        public void OnClick()
        {
            if (_stash != null) _stash.OnClickUse(_entry);
        }
    }
}
