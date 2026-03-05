using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;

namespace ArcadeRacer.Core
{
    /// <summary>
    /// Gère l'authentification anonyme Firebase via la Firebase Auth REST API.
    /// Aucun SDK Firebase requis — utilise UnityWebRequest comme le reste du projet.
    ///
    /// Pour activer :
    ///   1. Firebase Console → Authentication → Sign-in method → Anonymous → Activer
    ///   2. Firebase Console → Project Settings → General → Web API Key
    ///   3. Coller la Web API Key dans le champ "Firebase Web Api Key" de ce composant
    ///
    /// Le userId (localId) et le refresh token sont persistés dans PlayerPrefs pour
    /// maintenir l'identité du joueur entre les sessions sans qu'il ait à se re-connecter.
    /// </summary>
    public class FirebaseAuthManager : MonoBehaviour
    {
        // ── PlayerPrefs keys ──────────────────────────────────────────────────
        private const string PREFS_USER_ID      = "Firebase_UserId";
        private const string PREFS_REFRESH_TOKEN = "Firebase_RefreshToken";

        // ── Firebase Auth REST endpoints ──────────────────────────────────────
        // Anonymous sign-in (creates a new anonymous account)
        private const string AUTH_SIGN_IN_URL =
            "https://identitytoolkit.googleapis.com/v1/accounts:signUp?key={0}";

        // Exchange a refresh token for a new idToken
        private const string AUTH_REFRESH_URL =
            "https://securetoken.googleapis.com/v1/token?key={0}";

        private const int REQUEST_TIMEOUT = 10;

        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("=== FIREBASE AUTHENTICATION ===")]
        [Tooltip("Web API Key trouvé dans Firebase Console → Paramètres du projet → Général.\n" +
                 "Laisser vide pour désactiver l'authentification (les scores seront écrits sans auth).")]
        [SerializeField] private string firebaseWebApiKey = "";

        // ── Singleton ─────────────────────────────────────────────────────────

        private static FirebaseAuthManager _instance;

        public static FirebaseAuthManager Instance => _instance;

        // ── Public state ──────────────────────────────────────────────────────

        /// <summary>True si la clé API est configurée (auth activée).</summary>
        public bool IsAuthEnabled => !string.IsNullOrEmpty(firebaseWebApiKey);

        /// <summary>True après un sign-in réussi.</summary>
        public bool IsAuthenticated { get; private set; }

        /// <summary>UID unique du joueur (persisté entre sessions).</summary>
        public string UserId { get; private set; }

        /// <summary>JWT à courte durée de vie à passer comme paramètre auth= aux requêtes DB.</summary>
        public string IdToken { get; private set; }

        // ── Private state ─────────────────────────────────────────────────────

        private string _refreshToken;

        // ── Unity lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            if (IsAuthEnabled)
                StartCoroutine(InitializeAuth());
        }

        // ── Auth flow ─────────────────────────────────────────────────────────

        private IEnumerator InitializeAuth()
        {
            string savedUserId       = PlayerPrefs.GetString(PREFS_USER_ID, "");
            string savedRefreshToken = PlayerPrefs.GetString(PREFS_REFRESH_TOKEN, "");

            if (!string.IsNullOrEmpty(savedRefreshToken))
            {
                // Returning player — refresh existing token
                yield return RefreshToken(savedRefreshToken, savedUserId);
            }

            if (!IsAuthenticated)
            {
                // New player or refresh failed — create anonymous account
                yield return SignInAnonymously();
            }
        }

        private IEnumerator SignInAnonymously()
        {
            string url  = string.Format(AUTH_SIGN_IN_URL, firebaseWebApiKey);
            string body = "{\"returnSecureToken\":true}";
            byte[] bodyBytes = Encoding.UTF8.GetBytes(body);

            using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
            {
                req.uploadHandler   = new UploadHandlerRaw(bodyBytes);
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");
                req.timeout = REQUEST_TIMEOUT;

                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.Success)
                {
                    AuthSignInResponse resp = JsonUtility.FromJson<AuthSignInResponse>(req.downloadHandler.text);
                    ApplySignInResponse(resp);
                    Debug.Log($"[FirebaseAuthManager] Connexion anonyme OK. UserId: {UserId}");
                }
                else
                {
                    Debug.LogWarning(
                        $"[FirebaseAuthManager] Échec connexion anonyme: {req.error}\n" +
                        $"Vérifier que l'authentification anonyme est activée dans la Firebase Console " +
                        $"(Authentication → Sign-in method → Anonymous).");
                }
            }
        }

        private IEnumerator RefreshToken(string refreshToken, string userId)
        {
            string url = string.Format(AUTH_REFRESH_URL, firebaseWebApiKey);
            RefreshTokenRequest requestBody = new RefreshTokenRequest { refresh_token = refreshToken };
            byte[] bodyBytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(requestBody));

            using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
            {
                req.uploadHandler   = new UploadHandlerRaw(bodyBytes);
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");
                req.timeout = REQUEST_TIMEOUT;

                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.Success)
                {
                    AuthRefreshResponse resp = JsonUtility.FromJson<AuthRefreshResponse>(req.downloadHandler.text);
                    IdToken        = resp.id_token;
                    _refreshToken  = resp.refresh_token;
                    // resp.user_id matches userId when both are present; fall back to the
                    // persisted value in the unlikely event the response field is empty
                    // (e.g. older Firebase project configurations).
                    UserId         = !string.IsNullOrEmpty(resp.user_id) ? resp.user_id : userId;
                    IsAuthenticated = true;
                    PersistSession();
                    Debug.Log($"[FirebaseAuthManager] Token rafraîchi. UserId: {UserId}");
                }
                else
                {
                    // Non-fatal — InitializeAuth will fall through to SignInAnonymously
                    Debug.LogWarning($"[FirebaseAuthManager] Rafraîchissement du token échoué: {req.error}");
                }
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void ApplySignInResponse(AuthSignInResponse resp)
        {
            IdToken        = resp.idToken;
            _refreshToken  = resp.refreshToken;
            UserId         = resp.localId;
            IsAuthenticated = true;
            PersistSession();
        }

        private void PersistSession()
        {
            PlayerPrefs.SetString(PREFS_USER_ID,       UserId);
            PlayerPrefs.SetString(PREFS_REFRESH_TOKEN, _refreshToken);
            PlayerPrefs.Save();
        }

        // ── JSON models ───────────────────────────────────────────────────────

        [System.Serializable]
        private class RefreshTokenRequest
        {
            public string grant_type    = "refresh_token";
            public string refresh_token;
        }

        [System.Serializable]
        private class AuthSignInResponse
        {
            public string idToken;
            public string refreshToken;
            public string localId;
            public string expiresIn;
        }

        [System.Serializable]
        private class AuthRefreshResponse
        {
            public string id_token;
            public string refresh_token;
            public string user_id;
            public string expires_in;
        }
    }
}
