# Conturi de client — API demonstrativ

Identity permite înregistrarea unui client numai în mediul `Development`. Nu sunt necesare migrații: utilizatorii și parolele hash-uite sunt gestionate de ASP.NET Core Identity în baza proprie serviciului.

## Contract HTTP

- `GET /api/auth/csrf` furnizează tokenul pentru antetul `X-CSRF-TOKEN`; cookie-ul asociat trebuie păstrat.
- `POST /api/auth/register`, corp `{ "email": "...", "password": "..." }`: 204 pentru cereri valide, inclusiv adrese deja existente; 400 pentru date invalide sau CSRF invalid; 409 dacă există deja o sesiune autentificată; 429 la depășirea limitei. În afara Development, o cerere cu CSRF valid primește 404.
- Înregistrarea nu autentifică automat și nu schimbă parola unui cont existent. Clientul poate apoi folosi `POST /api/auth/login`, `GET /api/auth/me` și `POST /api/auth/logout`, cu protecțiile de sesiune existente.
- Limitarea înregistrării este globală per proces: 10 cereri/minut, fără coadă. Nu este o soluție distribuită de protecție împotriva abuzurilor.

Adresa este curățată de spațiile exterioare și limitată la 254 de caractere. Parola nu este curățată sau trunchiată: 12–128 caractere, cel puțin o majusculă ASCII, o minusculă ASCII, o cifră ASCII și un caracter nealfanumeric ASCII, compatibil cu politica Identity curentă. Normalizarea Identity și indexul unic pentru numele de utilizator previn conturile duplicate inclusiv pentru înregistrări concurente și diferențe de majuscule.

Conturile noi nu au roluri administrative și au `EmailConfirmed=false`. Cererea nu permite alegerea rolurilor, identificatorului sau stării de confirmare. Un răspuns 204 comun reduce dezvăluirea existenței contului prin status/corp; nu garantează eliminarea diferențelor de timp de răspuns. Nu se trimit e-mailuri. Inițializarea administratorului refuză să promoveze un cont existent fără rol Administrator; o adresă ocupată prin înregistrarea publică nu poate fi folosită pentru preluarea rolului. Inițializarea repetată a administratorului existent rămâne permisă.

## Organizare și limite

`Identity.Application` conține handler-ul `RegisterCustomerHandler` și portul `ICustomerAccounts`, fără referințe la ASP.NET Core, EF sau Infrastructure. `Identity.Infrastructure/Accounts/CustomerAccounts` adaptează UserManager. Endpoint-ul, cererea HTTP și limitarea accesului sunt în API. Nu adăugăm un Domain gol: nu există încă un model comercial de profil de client.

Interfața Angular este disponibilă la `/cont`, din navigația magazinului inclusiv pe mobil. Include înregistrare cu confirmarea parolei, autentificare, citirea sesiunii după reîncărcare și deconectare. Starea este verificată numai în browser, după randare, și din nou la revenirea ferestrei în prim-plan. Erorile de citire nu sunt prezentate drept deconectare; există reîncercare. Parolele sunt golite la trimiterea formularului și nu sunt persistate în stocarea browserului. Sesiunea Administrator este recunoscută, cu avertizare că deconectarea se aplică și administrării.

API-ul de asociere explicită a comenzilor încheiate este descris în [account-order-ownership.md](account-order-ownership.md). Interfața acestei asocieri, proprietatea coșului activ, verificarea e-mailului și recuperarea parolei rămân etape următoare. Coșul și comenzile încă neasociate continuă să fie identificate prin cookie-ul anonim existent; comenzile asociate sunt citite prin API-ul contului. Autentificarea nu revendică date pe baza adresei de e-mail și nu mută istoricul între conturi.

Înainte de lansare sunt necesare confirmarea e-mailului, resetarea parolei, politici de sesiune și abuz pentru producție și asocierea explicită a datelor cu identificatorul stabil al utilizatorului. Înregistrarea nu se activează comercial prin această etapă; checkout-ul și noindex rămân în regimul demonstrativ.
