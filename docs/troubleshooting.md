# Diagnostic Production

| Symptôme | Vérification |
|---|---|
| Aucun port écouté | `PORT` numérique, logs de démarrage, binding `0.0.0.0` |
| Erreur SQL | DNS/firewall, chiffrement, droits, base existante ; ne pas coller la connexion dans les logs |
| Migration échouée | sauvegarde, code de sortie, `--list`, verrou/tâche unique |
| Boucle HTTPS | forwarded proto, proxy placé avant HSTS ; le conteneur ne redirige pas lui-même en Production |
| Cookies invalides après redémarrage | volume `/app/data/keys`, ApplicationName et permissions |
| Certificat introuvable | volume, `BasePath`, `PdfRelativePath` et sauvegarde cohérente |
| Volume absent | mount `/app/data`, sachant qu’il n’est disponible qu’au runtime |
| IA désactivée | `AiTrainer__Enabled`, fournisseur et clé correspondante |
| Readiness en échec | SQL, `/app/data/keys`, `/app/data/certificates`, utilisateur Linux |
| Permission Linux | propriétaire/UID du volume ; ne jamais appliquer `chmod 777` |
| Erreur CSP | console navigateur, domaine précis à ajouter ; ne pas ajouter `*` |
| SQLite utilisée par erreur | vérifier `ASPNETCORE_ENVIRONMENT=Production` et le provider dans les diagnostics |

Utiliser le `X-Correlation-ID` pour rapprocher une page d’erreur des logs, sans demander au client
de transmettre cookies, mots de passe ou tokens.
