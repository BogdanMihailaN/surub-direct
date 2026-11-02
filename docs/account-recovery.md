# Confirmarea e-mailului și recuperarea parolei

Flux demonstrativ API + Angular — 2 noiembrie 2026. Livrarea printr-un furnizor de e-mail urmează. Funcțiile sunt disponibile numai în Development; autentificarea demonstrativă nu impune încă e-mail confirmat.

| Endpoint POST | Corp | Rezultat |
|---|---|---|
| `/api/auth/request-email-confirmation` | `email` | 204 pentru o adresă validă, inclusiv inexistentă/deja confirmată |
| `/api/auth/confirm-email` | `userId`, `token` | 204 la confirmare; 400 pentru date/token invalid, expirat sau deja folosit |
| `/api/auth/request-password-reset` | `email` | 204 pentru o adresă validă, inclusiv inexistentă |
| `/api/auth/reset-password` | `userId`, `token`, `password` | 204 la resetare; 400 pentru date/token invalid, expirat sau folosit |

Toate mutațiile cer CSRF Identity, inclusiv accesul direct pe Identity și cererile anonime. Răspunsurile handlerelor au `Cache-Control: no-store`. Cererile de recuperare împart o limită locală de 20/minut pe instanță, fără coadă. Tokenurile și parolele sunt trimise în corp, nu în query string; tokenurile nu apar în răspunsuri HTTP. Conturile Administrator sunt excluse din fluxul demonstrativ pentru clienți.

Tokenurile ASP.NET Identity Data Protection expiră după două ore și sunt legate de utilizator și scop. Resetarea reușită schimbă parola și security stamp; cookie-urile sunt revalidate la fiecare cerere Identity, inclusiv `/internal/session`, pentru a respinge imediat sesiunile anterioare. Concurența EF Identity împiedică două resetări cu același token să fie confirmate ambele. Confirmarea e-mailului deja confirmat este refuzată. Solicitarea unui token nou nu revocă automat toate tokenurile încă valabile; resetarea parolei le invalidează prin security stamp.

## Mesaje locale

`scripts/dev.py` configurează `AccountMail__Directory` către `work/identity-mail`. Fiecare mesaj este un fișier JSON cu tipul operației, adresa, identificatorul utilizatorului, tokenul și momentele de creare/expirare. Fișierul se publică prin redenumire după scriere; directorul nou are permisiuni Unix 0700, fișierele 0600. Aceste date sunt sensibile, locale și excluse din Git. Nu există endpoint de citire a mesajelor, logare a tokenurilor sau expediere externă. În absența configurării, adaptorul Development folosește un director temporar local.

Mesajele locale includ acum și `subject`, `textBody`, `htmlBody` și `actionUrl`. Linkul complet poate fi deschis direct din fișierul privat. Composer-ul este separat de adaptorul de stocare și poate fi reutilizat de viitorul furnizor de e-mail. Mesajele sunt în română, fără imagini externe sau urmărire.

`AccountMail:StorefrontOrigin` configurează originea magazinului; scriptul local citește `STOREFRONT_ORIGIN` din `.env`, implicit `http://localhost:4200`. Originea nu este derivată din antetul Host al cererii. Sunt acceptate origini HTTPS fără cale, credențiale, query sau fragment; HTTP este permis numai pe loopback în Development. Configurația invalidă oprește pornirea Development. Funcțiile rămân dezactivate în producție, fără a impune configurarea unei livrări încă inexistente.

Nu sunt modificări de schemă. Înainte de activarea comercială sunt necesare furnizorul de e-mail, URL-uri publice validate, persistența/rotația cheilor Data Protection, protecție anti-abuz distribuită, retenția mesajelor și decizia privind impunerea e-mailului confirmat. Răspunsurile generice nu reprezintă o garanție de timp constant sau de indisponibilitate uniformă a furnizorului.

## Interfața Angular

Pagina `/cont/recuperare`, accesibilă din Contul meu, solicită un mesaj de confirmare e-mail sau resetare parolă. Mesajul de succes rămâne generic pentru adrese existente/inexistente. Interfața precizează că expedierea e-mailurilor nu este încă activă.

Linkurile de aplicare folosesc `/cont/confirma-email#userId=…&token=…` și `/cont/reseteaza-parola#userId=…&token=…`, cu valorile codificate prin `URLSearchParams`. Pentru testele locale, linkul complet este în `actionUrl` din mesajul JSON privat; fișierele nu sunt expuse prin HTTP. Fragmentul nu este trimis serverului la GET și este eliminat imediat după citirea în browser. Tokenul rămâne doar în starea componentei, apoi este trimis în corpul POST cu CSRF și eliminat după succes/distrugere. Nu este păstrat în localStorage/sessionStorage.

Deschiderea linkului nu execută automat mutația: confirmarea e-mailului cere apăsarea butonului, iar resetarea cere parola nouă și repetarea ei. Parolele sunt validate și șterse din formular imediat după trimitere. După succes formularul dispare și se oferă autentificarea; nu se creează automat o sesiune. Răspunsurile incerte nu repetă cererea automat. Reîncărcarea paginii după eliminarea fragmentului cere redeschiderea linkului complet sau solicitarea unui mesaj nou.

Decizie utilizator, 2 noiembrie 2026: păstrăm momentan exclusiv mesajele locale. Nu se configurează și nu se activează un furnizor extern în această etapă.
