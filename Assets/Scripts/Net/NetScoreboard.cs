using System.Text;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using FirstGame.Core;
using FirstGame.UI;

namespace FirstGame.Net
{
    /// <summary>Hold TAB to show the networked scoreboard (players, PV, éliminations, morts).</summary>
    public class NetScoreboard : MonoBehaviour
    {
        GameObject _panel;
        Text _text;

        void Start()
        {
            var canvas = UIFactory.CreateCanvas("Scoreboard", 15);
            _panel = UIFactory.AddChild(canvas.transform, "SB").gameObject;
            UIFactory.Place((RectTransform)_panel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620, 380));
            UIFactory.Panel(_panel.transform, new Color(ArtPalette.UiInk.r, ArtPalette.UiInk.g, ArtPalette.UiInk.b, 0.92f));

            var title = UIFactory.Label(_panel.transform, "TABLEAU DES SCORES", 30, ArtPalette.NeonCyan, TextAnchor.UpperCenter, FontStyle.Bold);
            UIFactory.Place(title.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -18), new Vector2(600, 40));
            var hint = UIFactory.Label(_panel.transform, "(maintiens TAB)", 16, ArtPalette.UiDim, TextAnchor.UpperCenter);
            UIFactory.Place(hint.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -58), new Vector2(600, 24));

            _text = UIFactory.Label(_panel.transform, "", 24, ArtPalette.UiText, TextAnchor.UpperLeft, FontStyle.Bold);
            UIFactory.Place(_text.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(36, -96), new Vector2(560, 280));
            _panel.SetActive(false);
        }

        void Update()
        {
            bool show = Input.GetKey(KeyCode.Tab);
            if (_panel == null) return;
            if (_panel.activeSelf != show) _panel.SetActive(show);
            if (show) Refresh();
        }

        void Refresh()
        {
            var me = NetworkManager.Singleton != null ? NetworkManager.Singleton.LocalClientId : 0;
            var sb = new StringBuilder();
            foreach (var r in NetDamageRelay.All)
            {
                if (r == null) continue;
                string name = r.OwnerClientId == me ? "TOI" : $"Joueur {r.OwnerClientId}";
                sb.AppendLine($"{name}   —   PV {Mathf.CeilToInt(r.NetHp.Value)}    ✦ {r.Kills.Value} élim.    ✝ {r.Deaths.Value} morts");
            }
            if (sb.Length == 0) sb.Append("En attente de joueurs…");
            _text.text = sb.ToString();
        }
    }
}
