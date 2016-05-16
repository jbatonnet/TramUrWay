# TramUrWay

TramUrWay est une application Android permettant de suivre les transports en commun sur le réseau TaM à Montpellier.

Voici les fonctionnalités disponibles dans l'application :
- Affichage des horaires réels en direct
- Affichage des horaires théoriques hors-ligne
- Affichage de la position estimée des tramways et bus en temps réel

Voici les fonctionnalités prévues et en cours de développement :
- Un widget affichant les horaires de vos stations favorites
- Affichage des incidents sur les lignes
- Calcul d'itinéraires en mode déconnecté

Développé par un utilisateur du service TaM, pour les utilisateurs afin qu'ils aient un accès pratique aux horaires.

<p align="center">
    <img width="48" src="https://raw.githubusercontent.com/jbatonnet/tramurway/master/Data/Logo.png" />
</p>

## Téléchargement

Actuellement en beta ouverte, l'application TramUrWay est disponible en téléchargement sur le Google Play Store en suivant ce lien : [TramUrWay](https://play.google.com/apps/testing/net.thedju.TramUrWay)

Certains bugs et incompatibilités empêchent l'application de fonctionner correctement :
- Crash au démarrage sur certains modèles de **Wiko** ([Issue 1](https://github.com/jbatonnet/tramurway/issues/1))
- Ecran noir sur **Samsung Galaxy S3/S4 avec Android 4.3** ou inférieur ([Issue 2](https://github.com/jbatonnet/tramurway/issues/2))

## Structure

- **[Shared]** : Projets utilitaires partagés pour simplifier le développement .NET et Android. Ces projets sont disponibles sut GitHub en suivant ce lien: [Shared](https://github.com/jbatonnet/shared)
- **TramUrWay.Dumper** : Un outil pour extraire les horaires des transports en commun à partir du site de la TaM.
- **TramUrWay.Baker** : Un outil pour compiler les données des horaires, des trajectoires, des lignes et des stations dans un format réutilisable.
- **TramUrWay.Android** : L'application Android
- **TramUrWay.UITest** : Un projet permettant l'automatisation des tests d'interface de l'application, directement dnas l'émulateur, ou dans Xamarin Test Cloud

<p align="center">
    <img width="48" src="https://raw.githubusercontent.com/jbatonnet/tramurway/master/Data/Logo.png" />
</p>

## Développement

Cette application est développée en C# 6 en utilisant Xamarin. Elle peut être modifiée et compilée en utilisant [Visual Studio 2015 Community](https://www.visualstudio.com/fr-fr/visual-studio-homepage-vs.aspx), disponible gratuitement.

Cette application m'a servi à expérimenter le développement Android en C#, et à construire des projets partagés pour faciliter la conception de nouvelles applications.

Vous pouvez retrouver les utilitaires Android de mes projets partagés ici : [Shared Android project](https://github.com/jbatonnet/shared/tree/master/Android)

## Liens

L'application et les développeurs ne sont pas affiliés à la TaM et ne sont pas responsables de la fiabilité des données présentées.

- [TaM voyages](http://www.tam-voyages.com), pour les horaires des transports
- [TaM direct](http://www.tam-direct.com), pour les données temps-réel des transports
- [Xamarin](https://www.xamarin.com/platform), pour le développement d'applications Android en utilisant les technologies .NET
- [Android Studio](http://developer.android.com/tools/studio/index.html), pour la conception d'interfaces utilisateurs
- [Google Material Icons](https://design.google.com/icons/), pour les icônes utilisées dans l'interface