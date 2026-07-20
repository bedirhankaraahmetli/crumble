using UnityEngine;
using UnityEngine.UI;

namespace Crumble.UI
{
    /// <summary>Generic HUD button that opens (activates) a panel GameObject.</summary>
    [RequireComponent(typeof(Button))]
    public sealed class PanelOpenButton : MonoBehaviour
    {
        [SerializeField] private GameObject target;

        private void Start()
        {
            GetComponent<Button>().onClick.AddListener(() =>
            {
                if (target != null)
                {
                    target.SetActive(true);
                }
            });
        }
    }
}
