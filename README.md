# skibidi service zone (Warsztat Samochodowy)

Aplikacja webowa ASP.NET Core MVC do zarządzania warsztatem samochodowym.

## Funkcje
- Rejestracja i logowanie użytkowników (rola: Admin, Recepcjonista, Mechanik, Klient)
- Panel administratora: zarządzanie użytkownikami i uprawnieniami
- Panel recepcjonisty: obsługa klientów, przyjmowanie pojazdów, zarządzanie zleceniami
- Panel mechanika: przeglądanie i realizacja zleceń, przypisywanie części, edycja kosztów usługi
- Panel klienta: zgłaszanie napraw, podgląd historii zleceń, komunikacja z serwisem
- Zarządzanie częściami i magazynem
- Przypisywanie części do zleceń, kontrola stanów magazynowych
- Komentarze do zleceń (z informacją o roli autora)
- Strona główna z powitaniem, opisem warsztatu i wideo
- Personalizowane powitanie i panel w zależności od roli użytkownika
- Strona kontaktowa i polityka prywatności

## Struktura projektu
- **Controllers/** – kontrolery MVC (logika aplikacji)
- **Models/** – modele danych (np. ServiceOrder, Part, UsedPart, User)
- **Views/** – widoki Razor (strony aplikacji)
- **wwwroot/** – pliki statyczne (style, skrypty, wideo)
- **Data/** – konfiguracja bazy danych i seeder
- **Migrations/** – migracje Entity Framework

## Wymagania
- .NET 8.0
- Entity Framework Core
- SQL Server (lub inna baza zgodna z EF Core)
- Bootstrap 5 (wbudowany)

## Uruchomienie
1. Przywróć zależności: `dotnet restore`
2. Wykonaj migracje bazy: `dotnet ef database update`
3. Uruchom aplikację: `dotnet run`
4. Otwórz w przeglądarce: `http://localhost:5161/`

## Domyślne role i panele
- **Admin**: zarządzanie użytkownikami, panel administratora
- **Recepcjonista**: obsługa klientów, panel recepcjonisty
- **Mechanik**: realizacja zleceń, panel mechanika
- **Klient**: zgłaszanie napraw, panel klienta

## Dodatkowe informacje
- Wideo na stronie głównej: `wwwroot/videos/XD.mp4`
- Polityka prywatności i kontakt dostępne w górnym menu
- Przyciski logowania/rejestracji w prawym górnym rogu
