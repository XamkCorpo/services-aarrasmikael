# Vaihe 3: Service-kerros, Repository, Result Pattern ja API-dokumentaatio — Teoriakysymykset

Vastaa alla oleviin kysymyksiin omin sanoin. Kirjoita vastauksesi kysymysten alle.

> **Vinkki:** Jos jokin kysymys tuntuu vaikealta, palaa lukemaan teoriamateriaalit:
> - [Service-kerros ja DI](https://github.com/xamk-mire/Xamk-wiki/blob/main/C%23/fin/04-Advanced/WebAPI/Services-and-DI.md)
> - [Repository Pattern](https://github.com/xamk-mire/Xamk-wiki/blob/main/C%23/fin/04-Advanced/Patterns/Repository-Pattern.md)
> - [Result Pattern](https://github.com/xamk-mire/Xamk-wiki/blob/main/C%23/fin/04-Advanced/Patterns/Result-Pattern.md)

---

## Osa 1: Service-kerros

### Kysymys 1: Fat Controller -ongelma

Miksi on ongelma jos controller sisältää kaiken logiikan (tietokantakyselyt, muunnokset, validoinnin)? Anna vähintään kaksi konkreettista haittaa.

**Vastaus:**

Ensinnäkin ylläpidettävyys kärsii: kun controller hoitaa kaiken, yhdestä metodista voi kasvaa kymmenien rivien "möykky", jota on vaikea lukea tai muokata ilman sivuvaikutuksia. Jos esimerkiksi tietokantarakenne muuttuu, joudutaan kaivelemaan muutoksia controllerista sen sijaan että muutos tehtäisiin yhdessä selkeässä paikassa.

Toiseksi testaaminen on lähes mahdotonta: yksikkötesteissä ei haluta oikeaa tietokantaa mukaan, mutta jos controller kutsuu `_context.Products.ToListAsync()` suoraan, sitä ei voi testata ilman oikeaa EF Core -kontekstia. Erillinen service-kerros voidaan mockata testeissä helposti.

---

### Kysymys 2: Vastuunjako

Miten vastuut jakautuvat controller:n, service:n ja repository:n välillä tässä harjoituksessa? Kirjoita lyhyt kuvaus kunkin kerroksen tehtävästä.

**Controller vastaa:** HTTP-pyyntöjen vastaanottamisesta ja oikeiden statuskoodien palauttamisesta. Se ottaa sisään Request-DTO:ita, välittää ne servicelle ja palauttaa servicen antaman tuloksen asiakkaalle. Controller ei tunne entiteettejä eikä tietokantaa.

**Service vastaa:** Liiketoimintalogiikasta sekä DTO ↔ Entity -muunnoksista. Se ottaa vastaan Request-DTO:ita, muuntaa ne entiteeteiksi repositorylle, ja muuntaa repositoryn palauttamat entiteetit takaisin Response-DTO:iksi. Service myös lokittaa virheet ja palauttaa Result-olioita.

**Repository vastaa:** Kaikkiin tietokantaoperaatioihin liittyvästä koodista (EF Core -kyselyt, `SaveChangesAsync` jne.). Repository käsittelee pelkästään entiteettejä, ei DTO:ita.

---

### Kysymys 3: DTO-muunnokset servicessä

Miksi DTO ↔ Entity -muunnokset kuuluvat serviceen eikä controlleriin? Mitä hyötyä siitä on, että controller ei tunne `Product`-entiteettiä lainkaan?

**Vastaus:**

Kun muunnokset ovat servicessä, controller pysyy ohuena HTTP-kerroksena, joka ei tiedä mitään tietokantamallien rakenteesta. Tämä tarkoittaa, että jos entiteetin rakenne muuttuu (esim. lisätään kenttä `Product`-luokkaan), muutos tehdään vain service- ja mapping-tasolle — controlleriin ei kosketa. Lisäksi yksikkötesteissä voidaan testata koko ketju (DTO sisään → DTO ulos) suoraan serviceltä ilman controlleria, mikä tekee testeistä yksinkertaisempia.

---

## Osa 2: Interface ja Dependency Injection

### Kysymys 4: Interface vs. konkreettinen luokka

Miksi controller injektoi `IProductService`-interfacen eikä suoraan `ProductService`-luokkaa? Mitä hyötyä tästä on?

**Vastaus:**

Kun controller riippuu `IProductService`-interfacesta eikä konkreettisesta `ProductService`-luokasta, se noudattaa Dependency Inversion -periaatetta: ylätaso (controller) ei riipu alatasosta (toteutus), vaan molemmat riippuvat abstraktiosta (interface). Käytännön hyötyjä on kaksi: ensinnäkin testauksessa voidaan antaa controllerille mock-toteutus interfacesta ilman oikeaa tietokantaa. Toiseksi toteutuksen voi vaihtaa (esim. `ProductService` → `CachedProductService`) ilman, että controllerin koodia muutetaan lainkaan.

---

### Kysymys 5: DI-elinkaaret

Selitä ero näiden kolmen elinkaaren välillä ja anna esimerkki milloin kutakin käytetään:

- **AddScoped:** Uusi instanssi luodaan kerran per HTTP-pyyntö ja se jaetaan kaikkien saman pyynnön komponenttien välillä. Sopii tietokantapalveluihin, kuten `ProductService` tai `AppDbContext`, koska kaikki saman pyynnön operaatiot käyttävät samaa kontekstia.

- **AddSingleton:** Yksi instanssi luodaan koko sovelluksen elinaikana ja se jaetaan kaikkien pyyntöjen kesken. Sopii tilattomiin palveluihin kuten konfiguraatiopalveluihin, välimuistiin tai `IHttpClientFactory`:lle.

- **AddTransient:** Uusi instanssi luodaan joka kerta kun sitä pyydetään. Sopii kevyille tilattomille palveluille kuten validaattoreille tai laskureille, jotka eivät pidä tilaa muistissa.

Miksi `AddScoped` on oikea valinta `ProductService`:lle? Koska `AppDbContext` rekisteröidään oletuksena Scoped-elinkaarella, ja `ProductService` käyttää sitä. Jos `ProductService` olisi Singleton, se yrittäisi käyttää jo hävitettyä `DbContext`-instanssia myöhemmissä pyynnöissä.

---

### Kysymys 6: DI-kontti

Selitä omin sanoin mitä DI-kontti tekee kun HTTP-pyyntö saapuu ja `ProductsController` tarvitsee `IProductService`:ä. Mitä tapahtuu vaihe vaiheelta?

**Vastaus:**

1. HTTP-pyyntö saapuu ja ASP.NET Core alkaa luoda `ProductsController`-instanssia.
2. Kontti havaitsee, että konstruktori vaatii `IProductService`-parametrin.
3. Kontti katsoo rekisteriään ja löytää: `IProductService` → `ProductService`.
4. Kontti tarkistaa, tarvitseeko `ProductService` itse jotain — se tarvitsee `IProductRepository`n ja `ILogger<ProductService>`:n.
5. Kontti luo `IProductRepository`n → `ProductRepository`, joka tarvitsee `AppDbContext`in → kontti luo senkin.
6. `ILogger<ProductService>` löytyy sisäänrakennettuna ASP.NET Coresta automaattisesti.
7. Kontti rakentaa koko ketjun ja antaa valmiin `ProductService`-instanssin controllerille.
8. Controller saa `_service`-kenttään toimivan palvelun ja voi käsitellä pyynnön.

---

### Kysymys 7: Rekisteröinnin unohtaminen

Mitä tapahtuu jos unohdat rekisteröidä `IProductService`:n `Program.cs`:ssä? Milloin virhe ilmenee ja miltä se näyttää?

**Vastaus:**

Virhe ilmenee sovelluksen käynnistyessä (tai ensimmäisessä HTTP-pyynnössä riippuen ASP.NET Coren versiosta) eikä vasta käännösaikana. Sovellus kaatuu ajonaikaiseen poikkeukseen, joka näyttää suunnilleen tältä:

```
InvalidOperationException: Unable to resolve service for type
'ProductApi.Services.IProductService' while attempting to activate
'ProductApi.Controllers.ProductsController'.
```

Tämä tarkoittaa, että DI-kontti ei löydä rekisteriään `IProductService`-tyypille, joten se ei osaa luoda controlleria.

---

## Osa 3: Repository-kerros

### Kysymys 8: Miksi repository?

`ProductService` käytti aluksi `AppDbContext`:ia suoraan. Miksi se refaktoroitiin käyttämään `IProductRepository`:a? Anna vähintään kaksi syytä.

**Vastaus:**

Ensinnäkin testattavuus paranee: kun service käyttää `IProductRepository`-interfacea, yksikkötestissä voidaan antaa fake/mock-repositoryn, joka palauttaa haluttua dataa ilman oikeaa tietokantaa. `AppDbContext`:ia on paljon vaikeampi mockata.

Toiseksi teknologiariippuvuus vähenee: service ei enää tunne EF Corea lainkaan (`using Microsoft.EntityFrameworkCore` poistuu). Jos tietokantateknologia vaihtuu (esim. SQLite → PostgreSQL tai jopa NoSQL), muutos tehdään vain repositoryyn — service pysyy koskemattomana.

---

### Kysymys 9: Service vs. Repository

Mikä on `IProductService`:n ja `IProductRepository`:n välinen ero? Mitä tietotyyppejä kumpikin käsittelee (DTO vai Entity)?

**IProductService:** Käsittelee DTO:ita molempiin suuntiin — ottaa vastaan Request-DTO:ita (`CreateProductRequest`, `UpdateProductRequest`) ja palauttaa Response-DTO:ita (`ProductResponse`). Vastaa liiketoimintalogiikasta ja muunnoksista.

**IProductRepository:** Käsittelee pelkästään entiteettejä (`Product`). Se ei tiedä DTO:ista mitään — ottaa `Product`-olioita sisään ja palauttaa `Product`-olioita ulos. Vastaa ainoastaan tietokannan CRUD-operaatioista.

---

### Kysymys 10: Controllerin muuttumattomuus

Kun Vaihe 7:ssä lisättiin repository-kerros, `ProductsController` ei muuttunut lainkaan. Miksi? Mitä tämä kertoo rajapintojen (interface) hyödystä?

**Vastaus:**

Controller riippuu `IProductService`-interfacesta, ei sen toteutuksesta. Kun `ProductService`:n sisäinen toteutus muuttui (suora `AppDbContext` → `IProductRepository`), interface pysyi täsmälleen samana. Controller ei tiedä eikä välitä, miten service hakee datansa — se näkee vain `GetAllAsync()`, `GetByIdAsync()` jne. Tämä on interfacen ydinvoima: sisäinen toteutus voi muuttua vapaasti, kunhan sopimus (interface) pysyy samana. Kutsuja ei tarvitse koskaan päivittää.

---

## Osa 4: Exception-käsittely ja lokitus

### Kysymys 11: ILogger

Mikä on `ILogger` ja miksi sitä tarvitaan? Mistä lokit näkee kehitysympäristössä?

**Vastaus:**

`ILogger` on ASP.NET Coren sisäänrakennettu lokitusrajapinta, jolla voidaan kirjoittaa viestejä sovelluksen lokiin eri vakavuustasoilla (Information, Warning, Error jne.). Sitä tarvitaan erityisesti virhetilanteissa: kun tietokantaoperaatio epäonnistuu, `ILogger` kirjaa tarkan virheen (sisältäen stack tracen) talteen, jotta kehittäjä voi jälkikäteen selvittää mitä meni pieleen. Ilman lokitusta virhe katoaa eikä jää mitään jälkeä.

Kehitysympäristössä (`dotnet run`) lokit näkyvät suoraan terminaalissa/konsolissa. Visual Studiolla F5-ajossa ne näkyvät Output-ikkunassa "ASP.NET Core Web Server" -välilehdellä.

---

### Kysymys 12: Odotetut vs. odottamattomat virheet

Selitä ero "odotetun" ja "odottamattoman" virheen välillä. Anna esimerkki kummastakin ja kerro miten ne käsitellään eri tavalla servicessä.

**Odotettu virhe (esimerkki + käsittely):** Tuotetta ei löydy tietokannasta haetulla id:llä. Tämä on normaali tilanne, joka kuuluu sovelluksen toimintalogiikkaan. Käsittely: tarkistetaan `if (product == null)` ja palautetaan `Result.Failure("Tuotetta X ei löytynyt")` — ei heitetä exceptionia eikä lokiteta virheenä.

**Odottamaton virhe (esimerkki + käsittely):** Tietokantayhteys katkeaa kesken operaation tai SQLite heittää `SqliteException`-poikkeuksen ainutkertaisen kentän rikkomisesta. Tämä ei kuulu normaalin suorituksen kulkuun. Käsittely: `catch (Exception ex)` → `_logger.LogError(ex, "...")` kirjaa tarkan virheen lokiin, jonka jälkeen palautetaan `Result.Failure("...epäonnistui")` — ei heitetä exceptionia eteenpäin, jottei sovellus kaadu.

---

## Osa 5: Result Pattern

### Kysymys 13: Miksi null ja bool eivät riitä?

Alla on kaksi esimerkkiä. Selitä miksi ensimmäinen tapa on ongelmallinen ja miten toinen ratkaisee ongelman:

```csharp
// Tapa 1: null
ProductResponse? product = await _service.GetByIdAsync(id);
if (product == null)
    return NotFound();

// Tapa 2: Result
Result<ProductResponse> result = await _service.GetByIdAsync(id);
if (result.IsFailure)
    return NotFound(new { error = result.Error });
```

**Vastaus:**

Tavassa 1 `null` on monitulkintainen — se voi tarkoittaa "tuotetta ei löydy", "tietokantavirhe tapahtui" tai jopa "ei oikeuksia". Controller joutuu arvaamaan syyn ja valitsee aina `404 Not Found`, vaikka oikea statuskoodi saattaisi olla `500 Internal Server Error` tai `403 Forbidden`. Asiakas saa väärän vastauksen eikä tiedä mikä meni pieleen.

Tavassa 2 `Result`-olio kantaa mukanaan sekä tiedon onnistumisesta (`IsFailure`) että selkeän virheviestin (`result.Error`). Controller tietää täsmälleen mitä tapahtui ja voi palauttaa oikean statuskoodin oikealla virheilmoituksella. Myös API:n käyttäjä saa hyödyllisen JSON-virheviestin pelkän tyhjän vastauksen sijaan.

---

### Kysymys 14: Result.Success vs. Result.Failure

Miten `Result Pattern` muutti virheiden käsittelyä servicessä? Vertaa Vaihe 8:n `throw;`-tapaa Vaihe 9:n `Result.Failure`-tapaan: mitä eroa niillä on asiakkaan (API:n kutsuja) näkökulmasta?

**Vastaus:**

Vaihe 8:ssa (`throw;`) tietokantavirhe heitetään eteenpäin, jolloin ASP.NET Coren middleware ottaa sen kiinni ja palauttaa asiakkaalle geneerisen `500 Internal Server Error` -vastauksen ilman selittävää viestiä — ja sovellus "kaatuu" kyseisen pyynnön osalta hallitsemattomasti.

Vaihe 9:ssa (`Result.Failure`) virhe siepatataan catch-lohkossa ja palautetaan `Result.Failure("Tuotteen luominen epäonnistui")`. Controller saa tämän rauhallisesti ja palauttaa asiakkaalle `500`-statuskoodin yhdessä selkeän JSON-virheviestin kanssa: `{ "error": "Tuotteen luominen epäonnistui" }`. Sovellus pysyy hallitussa tilassa eikä kaadu, ja asiakas saa hyödyllisen vastauksen.

---

## Osa 6: API-dokumentaatio

### Kysymys 15: IActionResult vs. ActionResult\<T\>

Miksi `ActionResult<ProductResponse>` on parempi kuin `IActionResult`? Anna vähintään kaksi syytä.

**Vastaus:**

Ensinnäkin Swagger/OpenAPI-dokumentaatio paranee merkittävästi: `ActionResult<ProductResponse>` kertoo Swaggerille täsmälleen minkä tyyppisen vastauksen onnistunut pyyntö palauttaa, jolloin Swagger generoi oikean response-scheman automaattisesti. `IActionResult`:lla Swagger näyttää vain tyhjän vastauksen.

Toiseksi kääntäjä pystyy varoittamaan virheistä: jos metodissa yritetään palauttaa väärän tyyppistä dataa, kääntäjä huomaa sen käännösaikana. `IActionResult`:lla tällaisia virheitä ei huomata ennen ajonaikaa.

---

### Kysymys 16: ProducesResponseType

Mitä `[ProducesResponseType]`-attribuutti tekee? Miten se näkyy Swagger UI:ssa?

**Vastaus:**

`[ProducesResponseType]`-attribuutti dokumentoi endpointin kaikki mahdolliset HTTP-statuskoodit ja niiden palautustyypit suoraan koodissa. Esimerkiksi `[ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]` kertoo, että `200 OK` -vastauksessa palautetaan `ProductResponse`-olio, ja `[ProducesResponseType(StatusCodes.Status404NotFound)]` dokumentoi, että endpoint voi palauttaa `404`.

Swagger UI:ssa tämä näkyy jokaisen endpointin kohdalla selkeänä listana kaikista mahdollisista vastauksista statuskoodeineen ja schema-esimerkkeineen. Ennen attribuutteja Swagger näytti vain "200 OK" — nyt se näyttää esimerkiksi "200 OK (ProductResponse)", "404 Not Found" ja "400 Bad Request" omilla schemakuvauksillaan.

---

### Kysymys 18: Refaktorointi

Sovelluksen toiminnallisuus pysyi täysin samana koko harjoituksen ajan — samat endpointit, samat vastaukset. Mitä refaktorointi tarkoittaa ja miksi se kannattaa, vaikka käyttäjä ei huomaa eroa?

**Vastaus:**

Refaktorointi tarkoittaa koodin sisäisen rakenteen parantamista muuttamatta sen ulkoista toimintaa. Käyttäjä ei huomaa eroa, mutta kehittäjät ja sovelluksen elinkaari hyötyvät merkittävästi.

Refaktorointi kannattaa, koska se vähentää teknistä velkaa: ilman sitä koodi muuttuu ajan myötä yhä monimutkaisemmaksi ja vaikeammin muutettavaksi ("spaghetti-koodi"). Hyvin jäsennelty koodi on helpompi testata (yksikkötestit servicelle ja repositorylle erikseen), helpompi laajentaa (uusi tietokantateknologia → vain repository muuttuu), ja helpompi ymmärtää uusille kehittäjille (jokainen kerros tekee selkeästi yhden asian). Lyhyellä aikavälillä refaktorointi tuntuu turhalta, mutta pitkällä aikavälillä se säästää moninkertaisesti sen vaatiman ajan.

---
