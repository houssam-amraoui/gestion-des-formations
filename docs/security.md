# Sécurité Production

- Secrets exclusivement par variables/coffre ; jamais dans Git, logs ou vues.
- Cookie Identity `HttpOnly`, `Secure=Always`, `SameSite=Lax`, durée 8 h et expiration glissante.
- Tous les POST métier utilisent antiforgery ; les callbacks fournisseurs futurs devront avoir une
  authentification/signature distincte.
- Forwarded headers traités avant HSTS et routage. Railway ne publie pas une plage fixe documentée :
  la confiance est limitée à deux sauts, mais les proxys/réseaux ne peuvent être listés. Restreindre
  dès qu’une plage stable est fournie.
- Headers : nosniff, referrer strict, permissions minimales, SAMEORIGIN et CSP.
- La CSP garde temporairement `unsafe-inline` pour les scripts/styles Razor existants. Migrer vers
  des fichiers statiques et nonces. `unsafe-eval` et les wildcards générales sont interdits.
- Rate limits distincts : compte, certificats, downloads, exports, quiz et IA, partitionnés par
  identité lorsque disponible.
- Upload multipart global 6 Mo ; audio limité à 5 Mo et MIME contrôlé.
- URLs externes limitées à HTTP(S), iframes YouTube/Vimeo ; `javascript:`, `data:`, `file:` et
  `ftp:` sont refusés par les services.
- Erreurs sans stack/SQL/chemin ; `X-Correlation-ID` borné à 64 caractères.
- Ne jamais journaliser cookies, mots de passe, tokens, connexions, prompts ou corps audio.

Checklist : rotation des secrets, revue des rôles, CSP, cookies, CSRF, headers, volume privé,
permissions Linux, logs, dépendances, sauvegardes et configuration IA désactivée si incomplète.
