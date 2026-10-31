# Coșul contului — API demonstrativ

Ordering păstrează un coș separat pentru fiecare cont Identity. Coloana nullable `Carts.AccountId` și indexul unic sunt introduse de migrarea `AccountCarts`; coșurile anonime existente rămân neschimbate. Contul este identificat exclusiv din sesiunea validată de Identity, nu din corpul cererii.

## Endpoint-uri

Toate endpoint-urile sunt sub `/api/v1/ordering/account/cart`, disponibile numai în Development. Cer parametrul query obligatoriu `expectedAccountId`, care trebuie să coincidă cu identitatea autentificată. Acesta protejează confirmarea în cazul schimbării sesiunii; nu acordă acces unui cont destinație. Răspunsurile endpoint-urilor au `Cache-Control: no-store`.

| Metodă și sufix | Comportament |
| --- | --- |
| GET | Coșul contului; 204 dacă lipsește, 410 dacă a expirat. |
| POST `/session` | Creează un coș gol sau returnează coșul activ existent. Reîncepe explicit un coș expirat fără pregătire, golind selecția și ciorna și incrementând versiunea. |
| PUT | Înlocuiește selecția folosind `cartId`, `version`, `lines`. |
| PUT `/customer` | Salvează/șterge ciorna PF/PJ folosind `cartId`, `version`, `customer`. |
| POST `/import` | Transferă explicit coșul anonim în coșul gol al contului. |

Toate operațiile necesită sesiune autentificată. Mutațiile folosesc CSRF **Identity** (`X-CSRF-TOKEN`), inclusiv la acces direct pe Ordering. `X-CART-CSRF` rămâne numai pentru endpoint-urile anonime. Rolul Administrator nu este necesar. Lipsa sesiunii produce 401; CSRF invalid produce 400; contul așteptat diferit produce 409.

Nu se emite un cookie de proprietar pentru coșul contului. Identificatorul intern aleator folosit în stocare nu apare în DTO-uri; repository-ul anonim exclude coșurile de cont și la citire, și la editare. O altă sesiune autentificată în același cont poate citi și modifica selecția fără cookie-ul coșului anonim. ID-ul public al coșului nu acordă acces.

## Transferul din browser

Corpul conține `cartId`, `version` pentru sursă, `targetCartId`, `targetVersion` pentru destinație și `includeCustomer`. Sursa este identificată prin cookie-ul anonim original; destinația prin sesiunea Identity. Ambele coșuri trebuie să existe și să fie active.

Într-o singură tranzacție, Ordering blochează sursa, apoi destinația, verifică identificatorii/versiunile și mută selecția. Datele PF/PJ sunt copiate numai dacă `includeCustomer` este true. La final, sursa este golită inclusiv de datele clientului, iar versiunile ambelor coșuri cresc. Istoricul comenzilor deja finalizate rămâne pe sursa originală și se asociază contului prin fluxul separat de comenzi.

Transferul refuză o sursă goală/pregătită, o destinație nevidă/pregătită sau versiuni învechite (409). Nu există îmbinare implicită sau suprascriere a selecției unui alt dispozitiv. Două conturi care încearcă simultan transferul aceleiași surse nu pot câștiga ambele. Dacă răspunsul s-a pierdut, clientul recitește ambele coșuri; repetarea aceleiași comenzi după transfer produce 409 și nu dublează produsele.

## Concurență și expirare

Crearea inițială este serializată printr-un advisory lock tranzacțional pe cont, suplimentar indexului unic. Editările sunt serializate pe rând și verifică versiunea, astfel încât două dispozitive nu se suprascriu. Expirarea este de 30 de zile, fără prelungire implicită la citire. Reînceperea unui coș expirat este explicită; o pregătire activă o blochează. Curățarea periodică existentă poate elimina coșurile expirate după perioada de grație numai dacă nu au pregătiri sau comenzi.

## Interfața Angular — 30 octombrie 2026

Pagina `/cont` afișează coșul contului după verificarea sesiunii. Permite creare, reîncărcare, schimbarea cantităților, eliminarea articolelor și consultarea prețurilor estimate prin recalcularea publică Ordering. Datele clientului copiate pot fi consultate și șterse separat. Reînceperea după expirare cere confirmare explicită.

Transferul afișează e-mailul contului destinație și selecția browserului înainte de confirmare. Copierea datelor clientului/adreselor este debifată implicit. Renunțarea nu trimite nicio mutație. Lipsa unei destinații create, un coș de cont nevid, o pregătire sau o eroare de citire blochează transferul.

După orice mutație sunt recitite ambele coșuri. Un răspuns pierdut sau un conflict nu repetă automat salvarea: interfața afișează explicația și starea recitită. Transferul invalidează și coșul anonim din antet și notifică celelalte taburi. Notificările localStorage conțin doar un semnal aleator, fără produse, identificatori de cont sau date personale. Dacă stocarea browserului este restricționată, verificarea versiunilor pe server rămâne autoritativă.

Sesiunea se reverifică la focus, iar componentele cu date de cont sunt eliminate la reverificare/deconectare. Răspunsurile dintr-o instanță distrusă sau invalidate de alt tab nu pot restaura datele afișate. Fiecare cerere de cont trimite `expectedAccountId`. Erorile celor două coșuri sunt independente și nu sunt afișate ca selecții goale.

## Limite și continuare

Produsele adăugate din catalog și pagina `/cos` folosesc în continuare coșul anonim. Transferul în cont este explicit și disponibil numai către un coș gol; îmbinarea selecțiilor și adăugarea directă din catalog în coșul contului nu sunt implementate. Formularul PF/PJ este disponibil și în cont, cu adrese de livrare și facturare, consultare și ștergere.

API-ul și interfața de pregătire/confirmare a comenzilor din coșul contului sunt implementate (secțiunile de mai jos). Nu există sincronizare push/WebSocket; actualizările se recitesc prin API la revenirea în pagină sau prin „Reîncarcă coșurile”, iar conflictele dintre dispozitive se detectează prin versiune.

Checkout-ul comercial rămâne dezactivat. Datele sunt demonstrative, iar `noindex` rămâne activ.

## API de pregătire și comandă pe cont — 30 octombrie 2026

Rutele de mai jos sunt sub `/api/v1/ordering/account/cart/preparation`. Toate necesită sesiune Identity și parametrul `expectedAccountId`; mutațiile cer CSRF Identity. Sunt disponibile numai în Development și răspund fără cache, inclusiv prin acces direct la Ordering.

| Metodă și sufix | Comportament |
| --- | --- |
| POST | Pregătește selecția folosind `requestId`, `cartId`, `version`; revalidează Catalog, creează snapshot-ul și planul de rezervare atomic. |
| GET | Citește snapshot-ul și starea rezervării/comenzii pentru coșul contului; 204 dacă nu există pregătire. |
| POST `/{id}/cancel` | Cere eliberarea rezervării; după starea terminală, repetarea detașează pregătirea și păstrează selecția/ciorna. |
| POST `/{id}/confirm` | Cere confirmarea demonstrativă și consumul rezervării, idempotent după identificatorul pregătirii. |
| POST `/{id}/complete` | Încheie fluxul după confirmare/eșec/anulare; golește selecția și ciorna numai după confirmare sau anulare, păstrând istoricul. |

Comanda primește AccountId și AccountLinkedAt chiar la crearea intenției de confirmare, în aceeași tranzacție cu planul. Apare direct în istoricul contului, inclusiv în starea Confirming sau Failed, și nu este expusă istoricului anonim. Nu necesită asociere manuală ulterioară.

Repository-urile verifică atât identificatorul intern al coșului, cât și AccountId. Apelurile anonime folosesc obligatoriu AccountId null; chiar cunoașterea identificatorului intern nu permite citirea, anularea sau confirmarea unui coș de cont. Handler-ele de cont rezolvă coșul după identitatea validată, fără acceptarea unui owner hash din cerere.

Logica de recalculare și construire a pregătirii este comună fluxurilor anonim și autentificat. Inventory rămâne proprietarul stocului; coordonatorul și Outbox/Inbox existente păstrează idempotenta consumului/eliberării. Confirmarea ori anularea în curs blochează încheierea prematură. La confirmare eșuată, selecția și datele clientului rămân disponibile pentru corectare; comanda eșuată rămâne în istoricul contului.

## Interfața rezervărilor pe cont — 30 octombrie 2026

Secțiunea Coșul contului permite pregătirea selecției cu datele clientului salvate, consultarea snapshot-ului de prețuri și adresei, anularea rezervării și confirmarea demonstrativă. Confirmarea afișează explicit contul și totalul și permite renunțarea înaintea mutației. Comanda se păstrează automat în istoricul contului; istoricul are buton propriu de actualizare.

Starea rezervării se recitește la cinci secunde cât pagina este vizibilă, cu actualizare manuală disponibilă. Erorile ascund acțiunile bazate pe snapshot-ul vechi. Răspunsurile incerte la mutații declanșează recitirea coșului și pregătirii, fără retransmitere automată. Modificările din alt tab și distrugerea componentei invalidează citirile în curs și confirmarea selectată.

După eliberarea rezervării se poate relua editarea. Încheierea unei comenzi confirmate golește coșul; după confirmare eșuată se păstrează selecția și datele clientului. Datele PF/PJ pot fi editate direct în cont sau copiate explicit la transferul din browser. Adăugarea directă din catalog pe cont rămâne de implementat. Fluxul rămâne demonstrativ, fără plăți, livrare sau facturare.

## Editarea datelor clientului — 31 octombrie 2026

Butonul „Editează datele PF/PJ și adresele” deschide formularul reutilizat din coșul anonim. Componenta primește datele, blocarea și funcția de salvare, fără dependență de un store sau API. Validările PF/PJ și adreselor rămân comune; trecerea la persoană fizică elimină câmpurile firmei, iar facturarea la livrare elimină adresa de facturare separată.

Salvarea folosește versiunea coșului și contul așteptat, cu CSRF Identity. După mutație, inclusiv răspuns pierdut sau conflict, se recitește starea serverului fără retrimitere automată; editorul se închide și utilizatorul poate verifica datele salvate înainte de o nouă editare. Renunțarea nu scrie nimic. Coșurile pregătite și starea invalidată din alt tab blochează editarea.
