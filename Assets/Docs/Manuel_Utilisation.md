# Manuel d’utilisation

## Contrôles
- Déplacements: WASD
- Regarder: Souris
- Interagir / Ramasser / Lâcher: E
- Faire tourner l’objet tenu: maintenir R + bouger la souris
- Rapprocher/Éloigner l’objet tenu: molette
- Sprint: Shift
- Saut: Espace

## Options disponibles (in‑game)
- Sensibilité caméra (PlayerPrefs "sensitivity")
- Volume principal (PlayerPrefs "volume")

Note: pas de FOV, Motion Blur, ni remappage des touches dans le build actuel.

## Boucle de jeu
1. Depuis la StartRoom (étage -2), utilisez l’ascenseur pour rejoindre un étage de jeu.
2. Cherchez et récupérez les modules: Navigation et Security.
3. À la console d’extraction:
   - Insérez les deux modules dans leurs sockets.
   - Tournez les trois sélecteurs (Priority/Protocol/Destination) via E pour obtenir la bonne combinaison.
   - Appuyez sur le bouton de validation.
   - Si la séquence est correcte, la diode passe au vert et l’Extraction Pod sera activé à l’étage 2.
4. Rejoignez l’ascenseur et montez à l’étage 2 pour déclencher la sortie.

## IA ennemie (survie)
- Le mob patrouille, poursuit en cas de détection (vue/son), et peut ouvrir des portes bloquantes.
- Se cacher ou mettre de la distance permet de le semer; il cherchera autour du dernier point connu.

## Sauvegardes
- Automatiques lors des changements d’étage et à la fermeture/pause.
- Emplacement: `%USERPROFILE%/AppData/LocalLow/DefaultCompany/Deep Neurosis/FloorsData/`
- Fichiers: `startRoom.json`, `floor_<index>.json`
