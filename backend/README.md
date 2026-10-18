# Șurub Direct — backend

- `src/services`: Catalog, Inventory, Identity și hostul Ordering.
- `src/gateway`: API Gateway YARP.
- `src/shared`: infrastructură tehnică reutilizabilă, fără modele comerciale comune.
- `tests/SurubDirect.Tests`: teste unitare, integrare PostgreSQL/HTTP și limite de arhitectură.

Deschide `SurubDirect.sln` pentru toate proiectele backend. `Directory.Build.props` aplică regulile de compilare doar backend-ului.

Din acest director poți rula `dotnet build SurubDirect.sln` și `dotnet test SurubDirect.sln`. Scripturile din [README-ul principal](../README.md) configurează runtime-urile și secretele locale și se execută din rădăcina repository-ului.

Nu există referințe între proiectele unor servicii diferite. Comunicarea comercială trece prin HTTP; fiecare serviciu are propria bază și propriul utilizator PostgreSQL. Detalii în [arhitectură](../docs/architecture.md).
