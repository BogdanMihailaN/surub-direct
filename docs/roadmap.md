# Plan de implementare și automatizare

Automatizarea din acest chat rulează la fiecare 30 de minute și continuă etapele de mai jos. Utilizatorul a autorizat explicit implementarea întregului backlog, cu teste, documentație, commit și push după fiecare etapă. Automatizarea nu se oprește la finalul unei etape; se dezactivează numai când întregul proiect convenit este implementat și verificat. Activarea comercială și publicarea în producție necesită acord separat.

## Funcționalități finalizate

- [x] Servicii independente .NET cu Clean Architecture și gateway YARP; baze/credențiale separate.
- [x] Catalog, variante, ambalaje, prețuri, taxonomie, imagini, import CSV și administrare.
- [x] Identity: sesiune Administrator și CSRF, inclusiv direct pe servicii.
- [x] Furnizori, condiții SKU, comenzi de aprovizionare cu snapshot-uri și versiuni.
- [x] Recepții parțiale cu jurnal persistent, retry manual și deduplicare Inventory.

## Etape restante

- [x] Ajustări de stoc și jurnal al mișcărilor — implementate și verificate.
- [ ] Corecții legate de recepțiile originale, coerente între Purchasing și Inventory.
- [ ] Închiderea comenzilor de aprovizionare, inclusiv cantitatea rămasă nelivrată.
- [ ] Coș și flux de comandă pentru clienți persoane fizice și firme.
- [ ] Rezervări atomice, expirări, anulări și compensări.
- [ ] RabbitMQ, Outbox/Inbox, deduplicare, retry și dead-letter pentru fluxurile distribuite.
- [ ] Integrări de plăți, curier și facturare, cu verificări în mediile de test.
- [ ] Retururi și cerințele comerciale convenite pentru lansare.
- [ ] Validarea completă a fluxurilor comerciale, securității și operării; documentarea recuperării și backupurilor.

## Informații care trebuie furnizate sau confirmate

Furnizorii și datele comerciale reale, prețurile/TVA-ul, procesatorul de plăți, curierul, soluția de facturare, conturile/credențialele și condițiile de lansare. Nu se inventează aceste date și nu se declară proiectul terminat cât există cerințe restante ori blocaje. Se continuă lucrările independente și se cer deciziile necesare când devin relevante.

Progresul detaliat este în `implementation-status.md`, iar dovezile verificărilor în `verification.md`. Datele demonstrative și `noindex` rămân active până la îndeplinirea condițiilor de lansare și autorizarea separată a utilizatorului.
