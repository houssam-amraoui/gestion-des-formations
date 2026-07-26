# Railway

Railway détecte le `Dockerfile` racine et injecte `PORT`. `railway.json` utilise le builder
`DOCKERFILE`, `/health/live`, un timeout de 300 secondes et `ON_FAILURE`. Selon la documentation,
le health check sert au basculement du déploiement et non à la surveillance continue.

Procédure non exécutée :

1. Créer un projet Railway et connecter le dépôt GitHub ou une image approuvée.
2. Créer le service web depuis le Dockerfile.
3. Ajouter les variables décrites dans `production-configuration.md`.
4. Utiliser un SQL Server externe géré (par exemple Azure SQL). Railway présente officiellement
   Postgres/MySQL/Mongo/Redis mais ne fournit pas de SQL Server managé standard ; ne pas substituer
   PostgreSQL.
5. Attacher un volume au service avec le mount path `/app/data`. Les volumes n’existent qu’au
   runtime, pas au build ni au pre-deploy.
6. Pour l’utilisateur non root, définir au besoin `RAILWAY_RUN_UID=1654` selon les permissions du
   volume.
7. Générer un domaine, puis définir `Application__PublicBaseUrl` avec son URL HTTPS.
8. Exécuter le conteneur de migration comme tâche unique avant le web. Ne pas utiliser le
   pre-deploy Railway pour une tâche qui exige le volume.
9. Déployer après revue des staged changes.
10. Vérifier `/health/live`, puis `/health/ready`, logs, connexion, PDF et rôles.
11. Activer une sauvegarde quotidienne/hebdomadaire du volume et la sauvegarde du SQL externe.

Références officielles : [Dockerfiles](https://docs.railway.com/builds/dockerfiles),
[configuration](https://docs.railway.com/config-as-code/reference),
[health checks](https://docs.railway.com/deployments/healthchecks),
[variables](https://docs.railway.com/variables) et
[volumes](https://docs.railway.com/volumes).
