# Importul produselor din CSV

Din administrare, deschide **Import CSV**, descarcă modelul și înlocuiește datele demonstrative. Categoriile trebuie create înainte de import; lista de pe pagină arată slug-urile exacte. Importul creează numai produse noi ca ciorne, fără publicare sau operații de stoc.

## Format

UTF-8 (BOM opțional), separator `;`, maximum 1 MB și 500 de rânduri de date. Un rând reprezintă un ambalaj cu preț; un produs poate avea mai multe SKU-uri, iar un SKU mai multe ambalaje. Repetă integral și identic datele produsului/SKU-ului pe rândurile corespunzătoare. Liniile goale sunt ignorate. Ordinea și denumirile coloanelor sunt fixe:

| Coloană | Conținut |
|---|---|
| `product_name` | Denumire produs, maximum 200 caractere |
| `product_slug` | URL nou, maximum 160 caractere, litere ASCII/cifre/cratime |
| `category_slug` | Slug-ul unei categorii existente |
| `description` | Descriere, maximum 4000 caractere |
| `sku` | SKU nou, maximum 80 caractere |
| `variant_label` | Denumirea variantei, maximum 160 caractere |
| `diameter_mm`, `length_mm` | Opționale; pozitive, maximum 3 zecimale |
| `material`, `finish` | Obligatorii, maximum 80 caractere fiecare |
| `offer_code` | Cod unic de ambalaj, maximum 80 caractere |
| `offer_label` | Denumire ambalaj, maximum 100 caractere |
| `units_per_pack` | Bucăți per ambalaj, întreg pozitiv |
| `min_packs`, `pack_step` | Minim și pas în ambalaje, întregi pozitivi |
| `net_price` | Preț net RON per ambalaj, pozitiv, maximum 1.000.000 și 6 zecimale |
| `tax_rate` | Fracție între 0 și 1, maximum 4 zecimale; `0.21` este doar un exemplu ilustrativ |

Numerele folosesc **punctul zecimal**, fără separatori de mii. Spațiile de la capetele câmpurilor se elimină. Slug-urile sunt normalizate la litere mici, SKU-urile și codurile ambalajelor la majuscule. Un SKU repetat este permis numai în același produs, cu aceleași caracteristici și ambalaje diferite. Codurile ambalajelor nu se repetă.

Un câmp care conține `;`, rânduri noi sau ghilimele trebuie încadrat în ghilimele duble; ghilimelele din interior se dublează. Exemplu: `"Șurub; finisaj ""zincat"""`. Erorile indică linia fizică de început a înregistrării, inclusiv când descrierea ocupă mai multe linii.

## Validare și salvare

Selectarea fișierului face doar previzualizarea; nu scrie în bază. După corectarea erorilor, selectează din nou fișierul. Verifică tabelul, bifează confirmarea și apasă **Importă produsele ca ciorne**. Schimbarea fișierului anulează previzualizarea și confirmarea anterioare.

La salvare serverul revalidează toate datele. Orice eroare blochează întregul fișier. Produsele, variantele, ambalajele și prețurile se salvează într-o singură tranzacție PostgreSQL în baza Catalog, inclusiv dacă doi administratori încearcă simultan aceleași coduri. Nu sunt modificate produsele existente și nici datele Inventory. Reîncercarea unui fișier deja salvat este respinsă prin unicitatea slug-urilor/SKU-urilor/codurilor; nu creează duplicate. Dacă răspunsul se pierde după salvare, verifică mai întâi catalogul și revalidează fișierul.

Endpoint-uri protejate prin sesiune Administrator (inclusiv acces direct la Catalog):

- `GET /api/v1/admin/products/import/template` — model CSV demonstrativ;
- `POST /api/v1/admin/products/import/preview` — JSON `{ "csv": "…" }`, cere CSRF, întoarce rânduri și erori fără scriere;
- `POST /api/v1/admin/products/import/commit` — același corp, cere CSRF, întoarce ID-urile produselor create și numerele de variante/ambalaje. Validarea comercială eșuată la commit întoarce 409; sintaxa CSV invalidă 400, fișierul prea mare 413.

## Limite actuale

Nu există actualizare în masă, mapare liberă de coloane, istoric persistent de importuri, încărcare Excel nativă, import de imagini, atribute suplimentare sau integrare cu furnizori. Procesarea este sincronă, pentru loturi mici. Fișierul nu se păstrează pe server. Atributele suplimentare și imaginile se completează ulterior, stocul prin recepții, publicarea explicit din catalog. Nu sunt necesare migrații noi pentru acest flux.
