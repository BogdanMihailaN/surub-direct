# Backup și verificarea recuperării locale

## Salvarea bazelor

```sh
python3 scripts/dev.py backup
```

Necesită PostgreSQL din Docker Compose pornit. Salvează cele cinci baze active (Catalog inclusiv Pricing, Inventory, Identity, Ordering și Purchasing) în format PostgreSQL custom. Nu modifică bazele, nu include baza legacy și nu exportă parolele rolurilor.

Rezultatul apare în `work/backups/services-<UTC>-<identificator>/`: cinci fișiere `.dump` și `manifest.json` cu baza, dimensiunea și SHA-256. Directorul are permisiuni 0700, fișierele 0600 încă de la creare. `work` este exclus din Git. Arhivele conțin date private ale clienților și hashurile parolelor Identity; nu sunt anonimizate sau criptate. Păstrează copiile externe numai în spațiu privat criptat, conform politicii de acces stabilite pentru mediu.

O rulare nouă nu suprascrie copiile existente. Publicarea numelui final se face numai după salvarea celor cinci baze, verificarea dimensiunilor/sumelor de control și citirea indexului arhivelor prin `pg_restore --list`. O întrerupere lasă un director `.partial`, care nu este acceptat de comanda de verificare ca backup complet. Fișierele parțiale nu sunt șterse automat; pot fi inspectate privat și eliminate separat. Nu există încă retenție automată.

**Consistența este per bază**, conform snapshot-ului `pg_dump`, nu simultană între microservicii. Pentru un punct comun de recuperare trebuie oprite în prealabil toate sursele de scriere: API-uri, workeri, consumatori, teste, integrări și acces direct. O simplă oprire a gateway-ului nu este suficientă. Comanda nu pretinde și nu verifică această oprire. În producție este necesar un protocol separat de coordonare, inclusiv brokerul și resursele externe.

## Verificarea unei copii

```sh
python3 scripts/dev.py verify-backup --backup-path work/backups/services-<UTC>-<identificator>
```

Respinge lipsuri, duplicate, nume/căi nepermise, legături simbolice, manifest excesiv sau invalid, dimensiuni și checksum-uri diferite. Citește indexul fiecărei arhive fără a restaura date. Verificarea checksum-ului detectează modificările față de manifest; nu dovedește autenticitatea unui manifest modificat împreună cu arhivele. Folosește numai copii locale de încredere.

Verificarea indexului nu înlocuiește restaurarea: nu decomprimă și nu execută toate intrările arhivei.

## Exercițiul de restaurare izolat

```sh
LOCAL_BACKUP_DIR="$PWD/work/backups/services-<UTC>-<identificator>" \
python3 -m unittest discover -s backend/tests/SurubDirect.Tooling.Tests/Integration -v
```

Testul este explicit și necesită Docker/PostgreSQL și rolurile serviciilor deja provisionate. Verifică arhivele, apoi creează câte o bază nouă `surub_restore_test_<UUID>` pentru fiecare serviciu. Restaurează în tranzacție, sub rolul serviciului, cu oprire la prima eroare, fără proprietarii și ACL-urile din arhivă. Verifică existența tabelelor și proprietarii lor; elimină exclusiv baza temporară creată de test, inclusiv la eșecul restaurării. Dacă procesul este întrerupt forțat, poate rămâne o bază temporară care trebuie identificată și curățată separat. Nu este modificată configurația aplicației și nu sunt contactate API-urile sau procesatorii externi.

Exercițiul dovedește că arhivele pot fi restaurate în PostgreSQL local cu proprietarii așteptați. Nu verifică încă echivalența fiecărui rând, funcționarea întregii aplicații după recuperare sau consistența distribuită. Nu rulează implicit în `dev.py test`; fără `LOCAL_BACKUP_DIR`, testul este omis.

## Recuperare reală și resurse neincluse

Nu există comandă automată care suprascrie bazele active. Pentru un incident: izolează scrierile, păstrează o copie a stării curente, alege un set verificat, restaurează în baze noi și verifică schemele/migrațiile, proprietarii, permisiunile și fluxurile aplicației înainte de schimbarea conexiunilor. Reconcilierea Ordering–Inventory și Outbox/Inbox trebuie verificată înainte de repornirea consumatorilor; nu șterge cheile de deduplicare. Schimbarea bazelor active și reluarea operării necesită un plan separat adaptat incidentului.

Copiile de aici nu includ: imagini din `work/media/catalog`, fișierele locale de e-mail, cheile Data Protection, `.env`/secrete, volumele/cozile RabbitMQ, jurnalul operațional sau baza legacy. Configurația și secretele se gestionează separat; nu se introduc în Git. Recuperarea completă a magazinului, backupurile externe/criptate, retenția, RPO/RTO și restaurarea brokerului rămân de implementat și verificat înainte de producție. Baza legacy este păstrată.
