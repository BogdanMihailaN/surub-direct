# Coș anonim persistent

Ordering deține tabela `ordering.Carts`, în baza și cu credențialul propriu. Migrarea `PersistedCarts` se aplică prin `python3 scripts/dev.py init`; baza legacy nu este modificată. Sunt salvate numai slug-ul produsului, ID-ul ambalajului și numărul de ambalaje, maximum 50 de linii distincte. Prețurile și stocul sunt recitite separat prin recalculare. Nu se creează comenzi sau rezervări.

## Acces și protocol

- `GET /api/v1/ordering/cart`: citește coșul cookie-ului curent; 204 dacă lipsește. Vizitarea magazinului nu creează automat rânduri.
- `GET /api/v1/ordering/cart/csrf`: emite tokenul CSRF.
- `POST /api/v1/ordering/cart/session`: recuperează coșul activ sau creează un coș gol și cookie-ul. Cere header `X-CART-CSRF`.
- `PUT /api/v1/ordering/cart`: înlocuiește selecția cu `{ cartId, version, lines }`, numai pentru proprietarul cookie-ului și versiunea curentă. Cere CSRF.

Cookie-ul `SurubDirect.Cart` conține 32 de octeți aleatori, codificați hex. Baza păstrează doar SHA-256, nici DTO-urile, nici localStorage nu conțin secretul. Cookie-ul este HttpOnly, SameSite Strict, path `/api/v1/ordering`, Secure în afara dezvoltării locale. ID-ul public al coșului nu acordă acces. CSRF pentru aceste mutații anonime aparține Ordering; sesiunea Administrator și validarea CSRF administrativă rămân în Identity.

Înlocuirile sunt serializate prin blocare PostgreSQL a rândului, în tranzacție. ID-ul și versiunea sunt verificate după obținerea blocării. Un conflict produce 409, lipsa proprietarului 401, expirarea 410. Un retry cu versiunea veche nu suprascrie o salvare deja confirmată de baza de date.

Coșul expiră la 30 de zile de la creare, fără prelungire automată. Datele expirate nu mai sunt accesibile; curățarea fizică periodică nu este implementată încă. Eliminarea cookie-ului pierde accesul la coș. Nu există recuperare între dispozitive sau asociere cu un cont.

## Interfață și recuperare

Interfața afișează modificările numai după confirmarea serverului. Pe durata salvării, comenzile sunt blocate și totalul anterior este retras. În caz de răspuns pierdut, eroare sau conflict, utilizatorul reîncarcă starea serverului înainte de alte modificări; salvarea nu este repetată automat. Un semnal localStorage fără date de coș notifică celelalte taburi că trebuie să reîncarce. Verificarea versiunii pe server rămâne protecția autoritativă și dacă notificarea locală lipsește.

Copia veche `surub-direct.cart.v1` este validată și importată. Liniile existente pe server prevalează pentru același ambalaj; liniile locale distincte sunt adăugate. Copia locală este eliminată numai după confirmare. Datele locale corupte necesită renunțare explicită; indisponibilitatea serverului nu este prezentată drept coș gol.

Limitele pe peer sunt 120 cereri/minut pentru citire/salvare și 30/minut pentru creare, fără coadă. Recalcularea păstrează limita 60/minut; body maximum 64 KB. În configurația locală, gateway-ul apare ca un singur peer. Politicile de proxy, retenție, cheile Data Protection persistente și dimensionarea pentru producție rămân de configurat înainte de lansare.

Checkout-ul rămâne dezactivat, datele sunt demonstrative și noindex rămâne prezent.
