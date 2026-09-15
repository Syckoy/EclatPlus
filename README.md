# EclatPlus (Yeshua)

Application Windows pour augmenter **l’éclat numérique** : les couleurs deviennent plus riches et plus intenses, **sans changer la teinte**. Un orange reste orange, plus beau — comme le Digital Vibrance NVIDIA ou la saturation AMD, avec la possibilité d’aller plus loin.

## Fonctionnalités

- Curseur d’éclat en direct (0–200)
- Presets : Naturel, Plus coloré, Éclat fort, Extrême
- **Limiter au pilote** : uniquement le réglage officiel NVIDIA / AMD (pour les jeux)
- Au-delà du max NVIDIA/AMD : couleurs plus saturées, teinte inchangée
- Démarrage avec Windows, icône dans la barre d’état
- Vérification de mise à jour **uniquement au lancement**, installation depuis l’appli
- Les réglages utilisateur ne sont pas touchés par la mise à jour
- En **plein écran** (KovaaK’s, Apex, etc.), l’éclat extra reste appliqué. Quelques FPS peuvent partir ; le gros crash de perfs venait d’une Loupe allumée même à l’arrêt.

## Jeux (Valorant, Apex, Fortnite)

L’option **Limiter au pilote** n’utilise que le curseur du driver, comme le panneau NVIDIA ou AMD : pas d’injection dans le jeu, pas de lecture mémoire, pas d’overlay.

Coche cette case **avant** de lancer Valorant, Apex ou Fortnite. Décoche-la pour retrouver l’éclat plus fort sur le bureau.

Aucun éditeur ne peut garantir un ban à 0 %. Ce n’est pas un outil de triche ; reste dans le mode pilote en compétitif si tu veux rester aligné avec le panneau officiel.

L’éclat extra vise tout l’écran, y compris en jeu. Si un titre en **plein écran exclusif** ignore encore l’effet, passe en bordless. Pour zéro surcoût Loupe, coche **Limiter au pilote**.

## Utilisation

1. Lance `EclatPlus.exe`
2. **50** = couleurs naturelles
3. **100** = maximum officiel NVIDIA / AMD
4. **150–200** = plus d’éclat que les panneaux GPU
5. Fermer la fenêtre range l’appli dans la barre d’état (l’éclat reste actif)
6. **Quitter et restaurer** remet les couleurs d’origine

Les préférences sont stockées dans `%AppData%\EclatPlus\settings.json`.

## Prérequis

- Windows 10 / 11, 64 bits
- Carte **NVIDIA** ou **AMD** (le mode pilote passe par NvAPI / ADL)
- Pour compiler : [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

## Compilation

```bash
dotnet build EclatPlus.csproj -c Release
```

L’exécutable se trouve dans :

`bin/Release/net8.0-windows/EclatPlus.exe`

## Mises à jour

Au démarrage seulement, l’appli consulte ce dépôt. Si une version plus récente existe, tu peux **l’installer depuis Yeshua** (tes réglages dans `%AppData%` restent).

Pour publier une version :

1. Augmenter `Version` dans `EclatPlus.csproj` et `"version"` dans `version.json`
2. Pousser sur `main`
3. Créer une release taguée `vX.Y.Z` avec un fichier **`EclatPlus.zip`** (contenu de `bin/Release/net8.0-windows/`)

## Licence

Apache License 2.0
