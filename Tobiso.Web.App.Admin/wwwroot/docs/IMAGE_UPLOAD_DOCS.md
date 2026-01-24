# Image Upload API - Dokumentace

## Přehled
Vytvořil jsem kompletní funkcionalitu pro nahrávání, zobrazování a mazání obrázků v Tobiso.Web projektu.

## Vytvořené komponenty

### 1. DTOs (Tobiso.Web.Shared/DTOs/ImageDTOs.cs)
- `ImageUploadResponse` - odpověď po úspěšném nahrání obrázku
- `ImageUploadRequest` - metadata pro upload (neobsahuje skutečný soubor)
- `ImageListResponse` - seznam obrázků s metadaty
- `ImageInfo` - informace o jednotlivém obrázku
- `DeleteImageRequest` - požadavek pro smazání obrázku

### 2. API Service (Tobiso.Web.Api/Services/ImageService.cs)
- `IImageService` - interface pro práci s obrázky
- `ImageService` - implementace pro:
  - Nahrávání obrázků do `wwwroot/images` složky
  - Validace typu souboru a velikosti (max 10MB)
  - Generování jedinečných názvů souborů
  - Získání seznamu všech obrázků
  - Mazání obrázků

### 3. API Controller (Tobiso.Web.Api/Controllers/ImagesController.cs)
- `POST /api/Images/upload` - nahrání obrázku
- `GET /api/Images` - získání seznamu obrázků
- `DELETE /api/Images` - smazání obrázku
- Všechny endpointy vyžadují autorizaci

### 4. App Controllers
- `Tobiso.Web.App/Controllers/ImagesController.cs` - proxy controller pro App projekt
- `Tobiso.Web.App.Admin/Controllers/ImagesController.cs` - proxy controller pro Admin projekt
- Oba používají Refit client pro komunikaci s API

### 5. Refit Interface (ITobisoWebApi)
Přidal jsem do `ITobisoWebApi` nové metody:
- `UploadImage` - nahrání obrázku přes Multipart
- `GetAllImages` - získání seznamu obrázků  
- `DeleteImage` - smazání obrázku

### 6. Blazor komponenty (Admin projekt)
- `ImageUpload.razor` - komplexní komponenta pro správu obrázků
- `SimpleImageUpload.razor` - jednodušší verze (/admin/images-simple)

## Podporované formáty
- .jpg, .jpeg, .png, .gif, .webp, .svg
- Maximální velikost: 10MB

## Adresářová struktura
- Obrázky se ukládají do `wwwroot/images/`
- Podporuje podsložky (parametr `subDirectory`)
- Automatické vytváření složek, pokud neexistují

## Konfigurace v Program.cs
### App projekt:
```csharp
services.AddScoped<IImageService, ImageService>();
services.AddRefitClient<ITobisoWebApi>()...
```

### Admin projekt:
```csharp  
services.AddScoped<IImageService, ImageService>();
// ITobisoWebApi client už byl nakonfigurován
```

## Použití

### API endpoint příklady:
```bash
# Nahrání obrázku
curl -X POST "https://localhost:7270/api/Images/upload" \
  -H "Authorization: Basic <credentials>" \
  -F "file=@image.jpg" \
  -F "subDirectory=gallery"

# Seznam obrázků
curl -X GET "https://localhost:7270/api/Images" \
  -H "Authorization: Basic <credentials>"

# Smazání obrázku
curl -X DELETE "https://localhost:7270/api/Images?fileName=image.jpg" \
  -H "Authorization: Basic <credentials>"
```

### Blazor komponenta (Admin):
- Navštivte `/admin/images-simple` pro jednoduchou správu obrázků
- Navštivte `/admin/images` pro pokročilou správu s náhledy

## Bezpečnost
- Všechny endpointy vyžadují Basic Authentication
- Validace typu souboru a velikosti
- Čištění názvů souborů od nebezpečných znaků
- Generování jedinečných názvů pro předcházení kolizím

## Poznámky
- Obrázky se ukládají na lokální disk do `wwwroot/images`
- Pro produkční použití doporučuji přidat externí úložiště (Azure Blob, AWS S3)
- Komponenty jsou připravené pro rozšíření o dodatečné funkce (změna velikosti, komprese, atd.)