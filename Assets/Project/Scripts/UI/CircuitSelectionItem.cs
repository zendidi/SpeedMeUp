using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using ArcadeRacer.Settings;
using Unity.VisualScripting;

namespace ArcadeRacer.UI
{
    /// <summary>
    /// Composant d'un item de sélection de circuit dans l'UI.
    /// Affiche le thumbnail et le nom du circuit.
    /// </summary>
    public class CircuitSelectionItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("=== UI REFERENCES ===")]
        [SerializeField] private Image thumbnailImage;
        [SerializeField] private TextMeshProUGUI circuitNameText;
        [SerializeField] private Button selectButton;

        [Header("=== VISUAL ===")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 0.8f);
        [SerializeField] private Color hoverColor = new Color(0.9f, 0.9f, 1f, 1f);
        [SerializeField] private Color selectedColor = new Color(0.8f, 1f, 0.8f, 1f);

        private CircuitData _circuitData;
        private System.Action<CircuitData> _onSelected;
        private bool _isSelected;

        #region Initialization

        /// <summary>
        /// Configure l'item avec les données d'un circuit
        /// </summary>
        public void Setup(CircuitData circuitData, System.Action<CircuitData> onSelected)
        {
            _circuitData = circuitData;
            _onSelected = onSelected;

            if (circuitData != null)
            {
                // Configurer le thumbnail
                if (thumbnailImage != null)
                {
                    if (circuitData.thumbnail != null)
                    {
                        thumbnailImage.sprite = circuitData.thumbnail;
                        thumbnailImage.enabled = true;
                    }
                    else
                    {
                        thumbnailImage.enabled = false;
                    }

                    thumbnailImage.raycastTarget = false;
                }

                // Configurer le nom
                if (circuitNameText != null)
                {
                    circuitNameText.text = circuitData.circuitName;
                    circuitNameText.raycastTarget = false; // Ne doit pas bloquer les clics du bouton
                }

                // La backgroundImage doit avoir raycastTarget = true pour que le bouton fonctionne
            }

            // Configurer le bouton
            if (selectButton != null)
            {
                Debug.Log($"[CircuitSelectionItem] Configuration du bouton pour le circuit: {circuitData?.circuitName}");
                Debug.Log($"[CircuitSelectionItem] Button interactable: {selectButton.interactable}"); // 🔧 NOUVEAU
                selectButton.onClick.RemoveAllListeners();
                selectButton.onClick.AddListener(OnClicked);
            }
            else
            {
                Debug.LogError("[CircuitSelectionItem] selectButton est NULL !"); // 🔧 NOUVEAU
            }

            SetSelected(false);
        }

        #endregion

        #region Events

        public void OnClicked()
        {
            Debug.Log($"[CircuitSelectionItem] CLICK");

            if (_circuitData != null)
            {
                _onSelected?.Invoke(_circuitData);
                Debug.Log($"[CircuitSelectionItem] Circuit sélectionné: {_circuitData.circuitName}");
            }
        }

        #endregion

        #region Visual State

        /// <summary>
        /// Définit l'état visuel de sélection
        /// </summary>
        public void SetSelected(bool selected)
        {
            _isSelected = selected;
            UpdateVisualState();
        }

        private void UpdateVisualState()
        {
            if (backgroundImage != null)
            {
                backgroundImage.color = _isSelected ? selectedColor : normalColor;
            }
        }

        #endregion

        #region Hover Effect (IPointerHandler)

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!_isSelected && backgroundImage != null)
            {
                backgroundImage.color = hoverColor;
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!_isSelected && backgroundImage != null)
            {
                backgroundImage.color = normalColor;
            }
        }

        #endregion

        #region Properties

        public CircuitData CircuitData => _circuitData;
        public bool IsSelected => _isSelected;

        #endregion
    }
}
