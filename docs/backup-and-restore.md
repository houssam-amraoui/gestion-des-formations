# Sauvegarde et restauration

## SQL Server

Activer les sauvegardes automatiques du fournisseur, au minimum quotidiennes avec une rétention
adaptée au besoin métier. Créer une sauvegarde avant chaque migration. Tester trimestriellement une
restauration vers une base isolée et mesurer RPO/RTO. Le code web ne simule aucune sauvegarde SQL.

## Volume

Sauvegarder `/app/data/certificates` avec la base : `PdfRelativePath` doit continuer à pointer vers
le même fichier. Sauvegarder aussi `/app/data/keys`. La perte des clés invalide cookies et tokens
protégés ; leur exposition permet de déchiffrer des données protégées.

## Checklist de restauration

1. Isoler la nouvelle instance et arrêter les écritures.
2. Restaurer SQL Server à l’instant choisi.
3. Restaurer les PDF cohérents avec cette sauvegarde.
4. Restaurer les clés Data Protection avec permissions non root.
5. Lancer `--list`, puis les migrations éventuellement nécessaires.
6. Vérifier readiness, connexion existante, PDF et QR.
7. Basculer le trafic et surveiller les erreurs.
