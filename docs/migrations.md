# Migrations

Création et revue locale :

```powershell
dotnet ef migrations add Nom --project src/TrainingManagement.Infrastructure --startup-project src/TrainingManagement.Web --output-dir Persistence/Migrations
dotnet ef migrations list --project src/TrainingManagement.Infrastructure --startup-project src/TrainingManagement.Web
dotnet ef database update --project src/TrainingManagement.Infrastructure --startup-project src/TrainingManagement.Web -- --environment Development
dotnet ef migrations script --idempotent --project src/TrainingManagement.Infrastructure --startup-project src/TrainingManagement.Web -o artifacts/migrations.sql -- --environment Production
```

Avant Production : sauvegarder, tester la restauration, relire le SQL et vérifier les opérations
destructives/verrous. Exécuter une seule instance :

```text
dotnet /app/migrations/TrainingManagement.Migrations.dll --list
dotnet /app/migrations/TrainingManagement.Migrations.dll
```

EF Core utilise son verrou de migration ; la procédure impose en plus une seule tâche de migration.
La commande liste les migrations en attente, masque la connexion et quitte avec un code non nul si
la connexion ou l’application échoue. Le web ne migre pas en Production.

Un rollback se fait par une nouvelle migration corrective ou, après décision d’incident, par
restauration de la sauvegarde et redéploiement de l’ancienne version. Ne jamais utiliser un
`database update` arrière improvisé sur une base active.
