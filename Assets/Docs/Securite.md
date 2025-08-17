# Mesures de sécurité

Contexte: jeu solo/offline, aucune communication réseau dans le client.

- Intégrité du binaire
  - Diffusion d’un hash SHA256 par release (vérification recommandée avant exécution).
- Entrées/Sorties sûres
  - Sauvegardes sous Unity persistentDataPath (Windows):
    - `%USERPROFILE%/AppData/LocalLow/DefaultCompany/Deep Neurosis/FloorsData/`
    - Fichiers JSON: `startRoom.json`, `floor_<index>.json`
  - Pas d’accès arbitraire au système de fichiers hors de ce répertoire.
  - Sérialisation via `JsonUtility` (types DTO spécifiques; pas d’évaluation de code).
- Résilience des sauvegardes
  - Écriture à la fermeture/pause et lors des changements d’étage.
  - Champs supplémentaires tolérés; champs manquants doivent recevoir des valeurs par défaut côté code.
- Surface d’attaque minimisée
  - Pas de modules réseau, pas de chargement dynamique de scripts.
  - Pas de secrets embarqués, pas de tokens/API keys dans le client.
- Confidentialité
  - Pas de télémétrie ni collecte de données personnelles.
  - Préférences locales via `PlayerPrefs` (ex: sensitivity, volume).
- Chaîne d’approvisionnement
  - Version Unity et packages gelés via ProjectSettings/Packages; revue des mises à jour lors des upgrades.

Bonnes pratiques recommandées (backlog):
- Ajouter un champ `schemaVersion` aux JSON pour faciliter les migrations.
- Valider la structure des JSON à la lecture et ignorer les champs inconnus.
- Écrire de façon atomique (temp file + move) pour minimiser les corruptions.
