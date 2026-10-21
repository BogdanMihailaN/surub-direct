# Date PF/PJ și ciorna comenzii

În pagina coșului, secțiunea „Pregătirea comenzii” permite salvarea datelor unui client persoană fizică sau firmă. Sunt disponibile datele de contact, adresa de livrare în România și, opțional, o adresă de facturare distinctă. Rezumatul datelor salvate apare sub formular; produsele și recalcularea prețurilor rămân în aceeași pagină.

Aceasta este o ciornă atașată coșului, nu o comandă plasată. Salvarea datelor nu rezervă stoc și nu fixează prețul. [Pregătirea demonstrativă separată](cart-preparation.md) salvează un snapshot și rezervă temporar stocul; nu există număr de comandă, taxă de transport, plată sau emitere de factură. Formularul solicită date fictive în etapa demonstrativă; checkout-ul și indexarea rămân dezactivate.

## Validări

- Tipul clientului: person sau company; numele persoanei de contact, emailul, telefonul și adresa de livrare sunt obligatorii.
- Firma necesită denumire și CUI. Numărul registrului comerțului este opțional.
- CUI are o verificare de format (2–10 cifre, prefix RO opțional); nu este verificat în registre externe și nu stabilește statutul fiscal.
- Emailul este verificat sintactic, telefonul acceptă 7–15 cifre și separatorii uzuali. Nu se verifică existența ori proprietatea lor.
- Adresele au stradă/număr, localitate, județ/sector, cod poștal de 6 cifre și țară RO. Nu există verificare cu un nomenclator sau serviciu de adrese.
- Textele sunt limitate și normalizate prin eliminarea spațiilor marginale; caracterele de control sunt respinse.
- Trecerea la persoană fizică elimină denumirea firmei, CUI-ul și numărul de registru. Bifarea aceleiași adrese elimină copia separată de facturare.

Regulile autoritative sunt în Ordering.Domain. Angular verifică formularul și păstrează câmpurile introduse la refuzuri de validare ale serverului.

## Persistență și concurență

Migrarea CartCustomerDraft adaugă coloana nullable JSONB Customer la ordering.Carts. Coșurile existente rămân valide, cu Customer null. Datele sunt imutabile în modelul de domeniu; configurația EF, repository-ul, handler-ul și endpoint-ul au fișiere separate.

PUT /api/v1/ordering/cart/customer primește cartId, version și customer. Customer null șterge datele salvate. Pentru salvarea unui client este necesar cel puțin un articol în coș; selecția poate fi ulterior corectată dacă recalcularea raportează produse indisponibile.

Endpoint-ul folosește cookie-ul proprietarului, CSRF și limitarea de trafic existente, inclusiv când serviciul este apelat direct. ID-ul public nu acordă acces. Răspunsurile sunt no-store. Datele nu sunt puse în localStorage, URL-uri sau evenimentele de notificare între taburi.

Datele clientului și selecțiile folosesc aceeași versiune și aceeași blocare tranzacțională a rândului. Modificarea reușită incrementează versiunea. Conflictele necesită reîncărcare; după un răspuns pierdut, UI recitește starea și nu repetă automat salvarea. O validare respinsă nu modifică ciorna sau versiunea. Editările nesalvate sunt în memoria formularului și se pierd la navigare/reîncărcare.

Utilizatorul poate șterge datele separat de articole; golirea coșului le elimină în aceeași tranzacție. Ștergerea nu elimină retroactiv copiile din backupuri. Coșul păstrează expirarea inițială de 30 de zile; salvarea clientului nu prelungește accesul. Curățarea fizică periodică a coșurilor expirate și politica de retenție a backupurilor rămân de implementat înainte de folosirea datelor reale. Coloana nu are criptare aplicativă; protecția stocării și backupurilor trebuie configurată pentru producție.

## Etapa următoare

Plasarea comenzii comerciale și consumarea rezervării, apoi coordonarea prin evenimente. Asocierea cu un cont și integrările de plată, livrare și facturare rămân în backlog.
