using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace ArcadeRacer.UI
{
    /// <summary>
    /// Modal UI pour la saisie du nom du joueur quand il réalise un top 10.
    /// Affiche un input field TMPro avec validation et confirmation.
    /// </summary>
    public class HighscoreNameInputUI : MonoBehaviour
    {
        [Header("=== UI COMPONENTS ===")]
        [SerializeField] private GameObject modalPanel;
        [SerializeField] private TMP_InputField nameInputField;
        [SerializeField] private Button confirmButton;
        //[SerializeField] private Button cancelButton;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI messageText;

        [Header("=== SETTINGS ===")]
        [SerializeField, Tooltip("Nombre maximum de caractères pour le nom")]
        private int maxCharacters = 20;

        [SerializeField, Tooltip("Nom par défaut si le joueur annule")]
        private string defaultPlayerName = "Player";

        [SerializeField, Tooltip("Bloquer les inputs du jeu pendant la saisie")]
        private bool blockGameInputWhileOpen = true;

        [Header("=== MESSAGES ===")]
        [SerializeField] private string titleMessage = "🏆 NOUVEAU RECORD !";
        [SerializeField] private string promptMessage = "Entrez votre nom :";

        // Events
        public event Action<string> OnNameSubmitted;
        public event Action OnCancelled;

        // Runtime
        private bool _isOpen = false;
        private float _lapTime;
        private string _circuitName;
        private float _previousTimeScale = 1f;

        #region Properties

        public bool IsOpen => _isOpen;
        public int MaxCharacters
        {
            get => maxCharacters;
            set
            {
                maxCharacters = Mathf.Max(1, value);
                if (nameInputField != null)
                {
                    nameInputField.characterLimit = maxCharacters;
                }
            }
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeComponents();
            SetupInputField();
            SetupButtons();
            
            // Cacher le modal au démarrage (Awake s'exécute une seule fois à l'initialisation)
            Hide();
        }

        private void Start()
        {
            // Start peut être appelé plusieurs fois si l'objet est désactivé/réactivé
            // Ne plus cacher le modal ici pour éviter de fermer un modal actif pendant la course
        }

        #endregion

        #region Initialization

        private void InitializeComponents()
        {
            // Auto-find components si non assignés
            if (modalPanel == null)
            {
                // Chercher un panel enfant nommé "Modal" ou "Panel"
                Transform panelTransform = transform.Find("Modal") ?? transform.Find("Panel");
                if (panelTransform != null)
                {
                    modalPanel = panelTransform.gameObject;
                }
                else
                {
                    modalPanel = gameObject;
                    Debug.LogWarning("[HighscoreNameInputUI] Modal panel non assigné, utilise gameObject");
                }
            }

            if (nameInputField == null)
            {
                nameInputField = GetComponentInChildren<TMP_InputField>();
                if (nameInputField == null)
                {
                    Debug.LogError("[HighscoreNameInputUI] TMP_InputField non trouvé!");
                }
            }

            if (confirmButton == null)
            {
                Button[] buttons = GetComponentsInChildren<Button>();
                if (buttons.Length > 0)
                {
                    confirmButton = buttons[0];
                    Debug.LogWarning("[HighscoreNameInputUI] Confirm button auto-assigné au premier bouton trouvé");
                }
            }

            if (titleText == null)
            {
                TextMeshProUGUI[] texts = GetComponentsInChildren<TextMeshProUGUI>();
                foreach (var text in texts)
                {
                    if (text.name.Contains("Title"))
                    {
                        titleText = text;
                        break;
                    }
                }
            }

            if (messageText == null)
            {
                TextMeshProUGUI[] texts = GetComponentsInChildren<TextMeshProUGUI>();
                foreach (var text in texts)
                {
                    if (text.name.Contains("Message") || text.name.Contains("Prompt"))
                    {
                        messageText = text;
                        break;
                    }
                }
            }
        }

        private void SetupInputField()
        {
            if (nameInputField != null)
            {
                nameInputField.characterLimit = maxCharacters;
                nameInputField.contentType = TMP_InputField.ContentType.Standard;
                
                // Listener pour validation en temps réel
                nameInputField.onValueChanged.AddListener(OnInputValueChanged);
                
                // Submit avec Enter
                nameInputField.onSubmit.AddListener(OnInputSubmit);
            }
        }

        private void SetupButtons()
        {
            if (confirmButton != null)
            {
                confirmButton.onClick.AddListener(OnConfirmClicked);
            }

            //if (cancelButton != null)
            //{
            //    cancelButton.onClick.AddListener(OnCancelClicked);
            //}
        }

        #endregion

        #region Public API

        /// <summary>
        /// Affiche le modal pour saisir le nom du joueur
        /// </summary>
        public void Show(float lapTime, string circuitName)
        {
            _lapTime = lapTime;
            _circuitName = circuitName;

            // Afficher le modal
            if (modalPanel != null)
            {
                modalPanel.SetActive(true);
            }

            // Mettre à jour les textes
            string formattedTime = ArcadeRacer.Core.HighscoreEntry.FormatTime(lapTime);
            
            if (titleText != null)
            {
                titleText.text = titleMessage;
            }

            if (messageText != null)
            {
                messageText.text = $"{promptMessage}\n\nTemps: {formattedTime} sur {circuitName}";
            }

            // Réinitialiser et focus l'input
            if (nameInputField != null)
            {
                nameInputField.text = "";
                nameInputField.Select();
                nameInputField.ActivateInputField();
            }

            // Désactiver le bouton confirm si vide
            UpdateConfirmButton();

            _isOpen = true;

            // Bloquer les inputs du jeu si nécessaire
            if (blockGameInputWhileOpen)
            {
                _previousTimeScale = Time.timeScale;
                Time.timeScale = 0f;
            }

            Debug.Log($"[HighscoreNameInputUI] Modal affiché pour {circuitName} - {formattedTime}");
        }

        /// <summary>
        /// Cache le modal
        /// </summary>
        public void Hide()
        {
            if (modalPanel != null)
            {
                modalPanel.SetActive(false);
            }

            _isOpen = false;

            // Réactiver les inputs du jeu (restaurer la valeur précédente)
            if (blockGameInputWhileOpen)
            {
                Time.timeScale = _previousTimeScale;
            }

            Debug.Log("[HighscoreNameInputUI] Modal caché");
        }

        /// <summary>
        /// Configure le message du titre
        /// </summary>
        public void SetTitleMessage(string message)
        {
            titleMessage = message;
            if (titleText != null && _isOpen)
            {
                titleText.text = message;
            }
        }

        /// <summary>
        /// Configure le message de prompt
        /// </summary>
        public void SetPromptMessage(string message)
        {
            promptMessage = message;
            if (messageText != null && _isOpen)
            {
                messageText.text = message;
            }
        }

        #endregion

        #region Input Validation

        private void OnInputValueChanged(string value)
        {
            // Validation en temps réel (peut être étendue)
            UpdateConfirmButton();
        }

        private void UpdateConfirmButton()
        {
            if (confirmButton != null && nameInputField != null)
            {
                // Activer le bouton seulement si le nom n'est pas vide
                bool isValid = !string.IsNullOrWhiteSpace(nameInputField.text);
                confirmButton.interactable = isValid;
            }
        }

        private string GetValidatedName()
        {
            if (nameInputField == null)
            {
                return defaultPlayerName;
            }

            string name = nameInputField.text.Trim();

            // Si vide, utiliser le nom par défaut
            if (string.IsNullOrWhiteSpace(name))
            {
                return defaultPlayerName;
            }

            // Limiter la longueur (sécurité supplémentaire)
            if (name.Length > maxCharacters)
            {
                name = name.Substring(0, maxCharacters);
            }

            return name;
        }

        #endregion

        #region Event Handlers

        private void OnConfirmClicked()
        {
            string playerName = GetValidatedName();

            Debug.Log($"[HighscoreNameInputUI] Nom confirmé: {playerName}");

            // Notifier les listeners
            OnNameSubmitted?.Invoke(playerName);

            // Cacher le modal
            Hide();
        }

        private void OnCancelClicked()
        {
            Debug.Log("[HighscoreNameInputUI] Saisie annulée");

            // Notifier les listeners
            OnCancelled?.Invoke();

            // Cacher le modal
            Hide();
        }

        private void OnInputSubmit(string value)
        {
            // Soumettre avec Enter seulement si le nom est valide
            if (!string.IsNullOrWhiteSpace(value))
            {
                OnConfirmClicked();
            }
        }

        #endregion

        #region Context Menus (Debug)

#if UNITY_EDITOR
        [ContextMenu("Test: Show Modal")]
        private void TestShow()
        {
            Show(65.432f, "Test Circuit");
        }

        [ContextMenu("Test: Hide Modal")]
        private void TestHide()
        {
            Hide();
        }
#endif

        #endregion

        private void OnDestroy()
        {
            // Cleanup listeners
            if (nameInputField != null)
            {
                nameInputField.onValueChanged.RemoveListener(OnInputValueChanged);
                nameInputField.onSubmit.RemoveListener(OnInputSubmit);
            }

            if (confirmButton != null)
            {
                confirmButton.onClick.RemoveListener(OnConfirmClicked);
            }

            //if (cancelButton != null)
            //{
            //    cancelButton.onClick.RemoveListener(OnCancelClicked);
            //}
        }
    }
}
