# SpeedMeUp (Vroum Vroum) — Documentation DFD XR LAB

Jeu de course arcade développé sous Unity 6, avec système de circuits procéduraux et classement en ligne (Firebase). Cette documentation a été créée en 2026-07 pour permettre de reprendre le projet efficacement après une pause, en consolidant les notes de session éparpillées à la racine du dépôt.

## Sommaire

1. [Aperçu du projet](01-overview.md) — concept, stack technique, où trouver quoi
2. [Architecture du code](02-architecture.md) — systèmes principaux, patterns, fichiers clés
3. [Système de glissement (survirage/sous-virage)](03-slip-system.md) — physique arcade, réglages
4. [Highscores & Firebase](04-highscores-firebase.md) — état actuel du système de classement en ligne
5. [Historique des décisions](05-historique-decisions.md) — bugs rencontrés/corrigés, choix techniques passés
6. [Roadmap / TODO](06-roadmap-todo.md) — pistes d'amélioration identifiées

## Démarrage rapide

- Scène principale : `Assets/Project/Scene/Core/SampleScene.unity`
- Éditeur de circuits : `Assets/Project/Scene/Core/CircuitEditor.unity`
- Code source : `Assets/Project/Scripts/` (ne pas confondre avec `Assets/_Project`, presque vide et non utilisé)
- Setup Firebase : voir [FIREBASE_SETUP_GUIDE.md](../FIREBASE_SETUP_GUIDE.md) à la racine (toujours à jour, conservé tel quel)

## Anciens fichiers de session (racine du dépôt)

Le dépôt contient une quinzaine de fichiers `.md` produits pendant les sessions Claude Code précédentes (HIGHSCORE_*, CHECKPOINT_*, LAPTIMER_*, RESUME_FRANCAIS, etc.). Leur contenu utile a été consolidé dans [05-historique-decisions.md](05-historique-decisions.md) et dans les docs système correspondantes. `FIREBASE_SETUP_GUIDE.md` et `SLIP_SYSTEM_MANUAL.md` restent la référence à jour et ne sont pas dupliqués ici — `04-highscores-firebase.md` et `03-slip-system.md` les résument mais renvoient vers eux pour le détail pas-à-pas.
