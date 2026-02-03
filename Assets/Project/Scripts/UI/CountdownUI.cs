using UnityEngine;
using TMPro;
using ArcadeRacer.RaceSystem;

namespace ArcadeRacer.UI
{
    /// <summary>
    /// Affiche le compte à rebours avant le départ.  
    /// 3...  2... 1...   GO!  
    /// </summary>
    public class CountdownUI : MonoBehaviour
    {
        [Header("=== REFERENCES ===")]
        [SerializeField] private RaceManager raceManager;

        [Header("=== UI ELEMENTS ===")]
        [SerializeField] private TextMeshProUGUI countdownText;

        [Header("=== SETTINGS ===")]
        [SerializeField] private Color countdownColor = Color.white;
        [SerializeField] private Color goColor = Color.green;
        [SerializeField] private float goDisplayDuration = 1f;

        private bool _isShowingGo = false;
        private float _goTimer = 0f;

        #region Unity Lifecycle

        private void Awake()
        {
            if (raceManager == null)
            {
                raceManager = FindFirstObjectByType<RaceManager>();
            }
        }

        private void OnEnable()
        {
            // Reset quand on active
            _isShowingGo = false;
            _goTimer = 0f;
        }

        private void Update()
        {
            if (raceManager == null) return;

            // Mode COUNTDOWN :  afficher les chiffres
            if (raceManager.CurrentState == RaceManager.RaceState.Countdown)
            {
                ShowCountdown();
            }
            // Mode RACING : afficher GO!  puis se cacher
            else if (raceManager.CurrentState == RaceManager.RaceState.Racing)
            {
                if (!_isShowingGo)
                {
                    ShowGo();
                }
                else
                {
                    // Compter le temps d'affichage de GO!
                    _goTimer += Time.deltaTime;

                    if (_goTimer >= goDisplayDuration)
                    {
                        // Le UIManager va nous désactiver, pas besoin de le faire ici
                        // On indique juste qu'on a fini
                        gameObject.SetActive(false);
                    }
                }
            }
        }

        #endregion

        #region Display

        private void ShowCountdown()
        {
            if (countdownText == null) return;

            int secondsRemaining = Mathf.CeilToInt(raceManager.CountdownTimer);

            if (secondsRemaining > 0)
            {
                countdownText.text = secondsRemaining.ToString();
                countdownText.color = countdownColor;
            }
        }

        private void ShowGo()
        {
            if (countdownText == null) return;

            countdownText.text = "GO! ";
            countdownText.color = goColor;

            _isShowingGo = true;
            _goTimer = 0f;

            Debug.Log("[CountdownUI] Showing GO!");
        }

        #endregion

        #region Public API

        public void Reset()
        {
            _isShowingGo = false;
            _goTimer = 0f;

            if (countdownText != null)
            {
                countdownText.text = "";
            }
        }

        #endregion
    }
}