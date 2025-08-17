# Accessibilité (a11y)

## Actuellement implémenté (confirmé dans le code)
- Indication visuelle des objets interactifs (OutlineMeshCreator) et icône d’interaction.
- Sensibilité de la caméra réglable (SettingsManager → PlayerPrefs "sensitivity").
- Volume principal (Master) réglable (SettingsManager → PlayerPrefs "volume").
- Confort de manipulation d’objet:
  - Verrouillage temporaire de la caméra pendant la rotation d’un objet (R maintenu).
  - Distance de tenue de l’objet ajustable à la molette.

## Contrôles utiles pour l’accessibilité
- Interagir / Ramasser / Lâcher: E
- Faire tourner l’objet tenu: maintenir R + bouger la souris
- Rapprocher/Éloigner l’objet tenu: molette
- Sprint: Shift
- Saut: Espace

## Backlog prioritaire (non implémenté à ce jour)
- Sous-titres/captions pour dialogues et SFX clés.
- Réglage du FOV et désactivation Motion Blur.
- Modes daltonisme et préréglages haut contraste.
- Remappage des touches en jeu.
- Séparation des volumes Musique/SFX.

Notes:
- Les éléments ci‑dessus sont souhaités mais absents du code actuel (aucune occurrence FOV/MotionBlur/remapping). Ils devront être ajoutés côté UI + persistence (PlayerPrefs) et appliqués aux caméras/post‑process.
