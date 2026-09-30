# Demo-plan: sommerhus-sitet

Målet er et demo-site på https://demo-vac.sima.dk, hvor hovedhistorien virker fra ende til
anden: **find et hus → se en pris der passer → book → admin ser bookingen**.

Planen har fem trin. De udføres i rækkefølge, og hvert trin har sin egen tjekliste.

## Beslutninger

- **Forenkling sker i UI'et, ikke i domænet** (trin 4). Skema og entiteter bevares. Felter skjules,
  udledes eller får fornuftige standardværdier. En samlet domæneforenkling (enklere pris- og
  feature-model) er en senere, separat beslutning, der i så fald laves i RentalHome og i
  Kvaseer-rentalmodellen på én gang.
- **Priser er inkl. moms.** Forbrugerpriser skal vises som totalpriser inkl. moms. Totalen ændres
  ikke; tilbuddet viser "heraf moms" beregnet med `Pricing:VatRate`.
- **En nat uden pris er en fejl, aldrig en rabat.** Et tilbud eller en booking, hvor en eller flere
  nætter ikke har en sæsonpris, afvises med fejlnøglen `unpricedNights`.
- **Husgruppen gemmes på huset.** `UpsertHouseDto.GroupId` er det, formularen sender; `null`
  betyder ingen gruppe. Admin vælger gruppen i formularen og advares, når huset ikke har en
  sæsonkalender, der dækker de næste 12 måneder.
- **Husets kalenderfane redigerer husets effektive kalender** — dets egen, hvis det har en, ellers
  gruppens. Fanen siger tydeligt, når kalenderen deles af alle huse i gruppen, og linker til gruppen.
- **RentalHome er målebaseline for Kvaseer.** Efter trin 4 måles baselinen igen (trin 5).

## Trin 1 — Kerneflowet virker

Status: færdig 30.09.2026 (ikke committet). 356 tests grønne; gennemgået med uafhængig review og i browser.

### Pris
- [x] En nat uden sæsonpris giver fejlen `unpricedNights` i tilbud og booking.
- [x] Bookingen videregiver tilbuddets konkrete fejl i stedet for en generisk "unable to calculate".
- [x] Husgruppen gemmes (`UpsertHouseDto.GroupId`), også ved oprettelse; opret-formularen har feltet.
- [x] Ændring af gruppe, kalender, override eller prisplan rydder cachede tilbud og genberegner "fra"-prisen.
- [x] "Fra"-prisen bruger kun den aktive plan og kun sæsoner, der forekommer i husets kalender fra i dag.
- [x] Admin-formularen viser husets seneste prisplan, også når den er inaktiv; gem opretter ikke en ny.
- [x] Overlappende sæsonperioder afvises også ved enkeltvis oprettelse og redigering.
- [x] Redigering af en sæsonperiode på husets kalenderfane virker.
- [x] Admin advares, når et hus ikke har en sæsonkalender, og når kalenderen slutter inden for 12 måneder.
- [x] Negative eller nul-priser, dublerede sæsonkoder og gæster < 1 afvises med 400, ikke 500.
- [x] Antal gæster tjekkes mod husets kapacitet (`max_guests`) i tilbud og booking.
- [x] Moms vises som "heraf moms" (priser inkl. moms).
- [x] Gebyrer står eksplicit i `appsettings.json` (rengøring, ekstra gæst), og dokumentationen passer.
- [x] Prislinjer vises på dansk/engelsk med sæsonnavn i stedet for sæsonbogstav.
- [x] Seed-data: højsæson om sommeren og dyrere end lavsæson.
- [x] Tests låser eksakte totaler fast (ikke kun `> 0`).

### Kultur og decimaler
- [x] MVC læser decimaltal fra formularer og querystrings kultur-sikkert (`800.5` er 800,5 under da-DK).
- [x] MVC-klienterne sender tal til API'et i invariant format.

### Login og adgang
- [x] Login, registrering og logout lander på den offentlige husliste (admin lander i admin).
- [x] Manglende adgang viser en "Ingen adgang"-side i stedet for en redirect-løkke.
- [x] "Admin"-linket vises kun for admins.

### Bootstrap og ikoner
- [x] Bootstrap 5.3 og Bootstrap Icons ligger lokalt i `wwwroot/lib` og indlæses i layoutet.
- [x] Knapper, der kun har et ikon, har en tilgængelig tekst (`aria-label`/`title`).

### Husets side og booking
- [x] Features vises én gang og med værdier: nøgletal (fx "3 soveværelser", "210 m²") og
      faciliteter (ja/nej); `false` vises ikke.
- [x] Feature-båndet ombrydes, så siden ikke bliver bredere end skærmen.
- [x] Gæsteantal på husets side og i bookingen er begrænset af husets kapacitet og har samme standard.
- [x] "Book nu" medtager valgte datoer og gæster; bookingformularen er udfyldt og viser prisen før bekræftelse.
- [x] Afrejse sættes korrekt, når ankomst flyttes (lokal dato, ikke UTC).

### Søgning og robusthed
- [x] Sortering efter pris virker på SQLite.
- [x] Et ukendt eller ikke-publiceret hus-id giver 404 i stedet for 500.

## Trin 2 — Indhold på live-sitet

- [ ] En idempotent import af demo-indhold (ikke dev-seederen): to huse (Blåvand Strand 4,
      Almosetoften 10) og to områder (Blåvand, Ho) med tekster og billeder fra `Sommerhus resourcer`.
- [ ] De 11 feature-ikoner fra `wwwroot/images/features` knyttes til faciliteterne.
- [ ] Priskalender, der dækker de næste år.
- [ ] Rigtige billeder af Ho (de nuværende fire er kopier af Blåvands).

## Trin 3 — Visuelt løft efter de originale mockups

Mockups: `Sommerhus.Mvc/wwwroot/Sommerhus resourcer/Layout/`.

- [ ] Forside med stort foto og søgebar (område, ankomst, afrejse, gæster) og udvalgte huse/områder.
- [ ] Huskort: foto først, titel + område, nøgletal, 3–4 ikoner, "fra X kr./nat".
- [ ] Husets side: galleri (1 stort + 4 små), ikonrække med værdier, beskrivelse og faciliteter,
      prisboks der følger med ved scroll (bundbar på mobil), kort.
- [ ] Områder med forsidebillede og husene som kort.
- [ ] Rolig nordisk palet og én accentfarve; mobilmenu der kan klappes sammen.
- [ ] Dansk med æøå som primært sprog; alle views bruger oversættelsen; feature-navne på dansk.
- [ ] Beskrivelser som tekst med afsnit i stedet for rå HTML (`Html.Raw`).
- [ ] Ubrugte CSS/JS-filer fjernet; dobbelt BOM i `site-additions.css` fjernet.
- [ ] Sidetitler virker på lister (`LocalizedString` i `ViewData["Title"]`).

## Trin 4 — Forenkling i UI'et

- [ ] Features i admin: kun navn og ikon for faciliteter; nøgle, kategori, valgmuligheder og
      søgbar udledes eller skjules. Nøgletal redigeres som faste felter i husformularen.
- [ ] En feature kan redigeres, ikke kun oprettes og slettes.
- [ ] Avanceret søgning: datoer, gæster, område, pris og 4–5 faciliteter. Filtre med
      valgmuligheder, der aldrig matcher, forsvinder. Ledighed indgår. Bladring bevarer filtre.
- [ ] Pris i admin samlet på én side pr. hus ("Priser og sæsoner"): kalender, pris pr. sæson, gebyrer.
- [ ] Husadmin fra 7 faner til 4 (Hus, Billeder, Faciliteter, Priser og sæsoner).
- [ ] Admin-sider for bookinger og ejertildeling (API'et har dem allerede).
- [ ] Beslut, om en afventende booking skal holde datoerne. I dag er den en forespørgsel uden
      blokering, så flere gæster kan booke samme uge, og først bekræftelsen afviser dubletten.
      `AvailabilityStatus.Tentative` findes allerede til formålet.
- [ ] http omdirigeres til https (også login-formularen serveres i dag over http).

## Trin 5 — Baseline i ApplatosX

RentalHome er målebaselinen for Kvaseer-rentalmodellen
(`ApplatosX/tests/Integration.Tests/Data/RentalRemote/model`).

- [ ] Ret "gratis nat" også i modellen (`services/quotes.aps`): en nat uden sæsonpris er en fejl.
      Goldens først, efter ApplatosX' regler.
- [ ] Mål RentalHome igen (i dag 35.258 linjer C# i 295 filer) og opdater tallene i
      `docs/product/active/260921-licensing-and-pricing-model.md` og hvor de ellers citeres.
- [ ] Afstem de funktionelle forskelle, trin 1–4 har skabt mellem RentalHome og modellen.
