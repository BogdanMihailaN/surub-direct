# Șurub Direct — frontend

Workspace Angular cu două aplicații:

- `src/apps/storefront`: magazinul public, cu SSR.
- `src/apps/admin`: administrarea catalogului și stocurilor.
- `src/shared`: modele, HTTP și componente folosite de ambele aplicații.
- `tests/e2e`: scenarii Playwright, inclusiv verificarea contractelor HTTP prin gateway.

Configurațiile `angular.json`, `tsconfig.json`, `playwright.config.ts` și `package.json` rămân la acest nivel. Fiecare aplicație are propriul `tsconfig.app.json`, componente cu TypeScript/HTML/SCSS separate și resurse în `public`.

Din acest director:

```sh
npm ci
npm start                 # storefront, port 4200
npm run start:admin        # administrare, port 4201
npm run build              # ambele build-uri de producție
npm run test:e2e           # necesită serviciile și interfețele pornite
npm run format
```

Toate apelurile comerciale trec prin API Gateway la portul 5099. Pentru pornirea backend-ului și baza locală, vezi [README-ul proiectului](../README.md). Testele frontend implementate în prezent sunt Playwright; nu există încă o suită de teste unitare Angular.
