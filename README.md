# TEMPEST · Anime & Gaming Store

E-commerce de catálogo con cierre de ventas por **WhatsApp**, construido como **monolito modular** en
**ASP.NET Core / Blazor Web App (.NET 10 LTS)** con **Clean Architecture + CQRS** en el backend,
**Feature-Sliced Design** en el frontend y **SQLite** como persistencia. Listo para **Docker**.

| Vista | Ruta | Render |
|---|---|---|
| Catálogo público (cards + skeletons + botón WhatsApp) | `/` | SSR estático + *streaming rendering* (sin WebSocket por visitante) |
| Login (cookie HttpOnly) | `/login` | Formulario SSR (el servidor emite la cookie) |
| Panel de administración | `/admin` | Shell estático (`[Authorize]`, título) + isla **Interactive Server** |

Credenciales iniciales (se crean en el primer arranque si no hay usuarios): **`admin` / `admin`**.
El panel muestra un aviso hasta que cambies esa contraseña.

---

## Inicio rápido

### Con Docker (recomendado)

Requisitos: Docker Desktop. En Windows necesita **WSL 2**: en una terminal de administrador ejecuta
`wsl --install`, reinicia y abre Docker Desktop hasta que indique *Engine running*.

```bash
docker compose up -d --build
```

Abre <http://localhost:5001>. El contenedor escucha en el puerto 80 y se publica solo en `127.0.0.1:5001`; la configuración se toma de un
archivo `.env` opcional (ver [`.env.example`](.env.example)). La base de datos y las imágenes subidas viven en
volúmenes con nombre (`tempest-data`, `tempest-uploads`): sobreviven a `docker compose down` y a
reconstrucciones de la imagen (`docker compose down -v` sí los borra).

Para un servidor con Nginx, ver [Despliegue en un VPS](#despliegue-en-un-vps-docker--nginx).

### En local

Requisitos: [.NET SDK 10](https://dotnet.microsoft.com/download).

```bash
dotnet run --project src/WebUI --launch-profile http
```

Abre <http://localhost:5080>. La base se crea en `src/WebUI/data/tempest.db` y las subidas en
`src/WebUI/wwwroot/uploads/` (ambas ignoradas por git).

---

## Arquitectura

```text
WebUI (Blazor, FSD) ──► Application (CQRS, DTOs, puertos) ──► Domain (entidades, value objects)
        │                          ▲
        └──► Infrastructure ───────┘   (EF Core SQLite, repositorios, hashing, almacenamiento)
```

- **Domain**: `Product`, `User`, `StoreSettings`; value objects `WhatsAppNumber`, `MessageTemplate`,
  `Username`; enum `Rarity`. Sin dependencias de frameworks.
- **Application**: comandos y queries con un **dispatcher propio** (sin MediatR, que desde la v13 pasó a un
  modelo de licencia comercial) y un pipeline de *behaviors*: manejo de excepciones → logging → **autorización segura
  por defecto** (todo requiere sesión salvo `IAllowAnonymous`) → validación con **FluentValidation**.
  Cada request se ejecuta en **su propio scope de DI**, así cada comando/query tiene su propio `DbContext`
  (clave en Blazor Server, donde el scope del circuito vive horas). Los resultados usan un `Result<T>` con
  errores por campo que el formulario muestra junto a cada input.
- **Infrastructure**: EF Core + SQLite con **migraciones** (aplicadas al iniciar), modo **WAL**, precios
  guardados como **centavos enteros** (SQLite no tiene `decimal`), `PasswordHasher<T>` de ASP.NET Core
  Identity (PBKDF2-HMAC-SHA512, 100 000 iteraciones), almacenamiento local de imágenes y *seeder*.
- **WebUI**: Feature-Sliced Design adaptado a Blazor. Cada capa solo importa de las inferiores
  (lo verifican pruebas de arquitectura):

```text
src/WebUI/
├── App/        Program.cs, DI, pipeline HTTP, Routes, layout, autenticación por cookie
├── Pages/      Home (/), Login (/login), AdminDashboard (/admin) + AdminConsole (isla interactiva)
├── Widgets/    NavbarWidget, ProductGridWidget, AdminProductTableWidget, AdminUserListWidget
├── Features/   AddProductForm, ProductImageInput, ProductStatusToggle, DeleteProductButton,
│               WhatsAppSettingsForm, WhatsAppButton, LoginForm, ChangePasswordForm, CreateUserForm
├── Entities/   ProductCard, ProductCardSkeleton, RarityBadge, PriceTag, SessionClaims
└── Shared/     Styles/app.css (tokens del tema), UI kit (Modal, Toasts, Icon…), formularios, formato
```

El frontend nunca usa entidades ni EF Core: solo `ISender` y los DTOs de Application.

### Botón de WhatsApp

`https://wa.me/{numero}?text={mensaje_codificado}`. El número y la plantilla se editan en
**/admin → WhatsApp** (con vista previa en vivo). La plantilla admite `{producto}` y `{precio}`; el precio
usa el mismo formato que la card. El número se normaliza (`+52 (55) 1234-5678` → `525512345678`).
Mientras no haya número configurado, el botón aparece deshabilitado.

### Seguridad

- Cookie `HttpOnly`, `SameSite=Lax`, 8 h con expiración deslizante; claves de Data Protection persistidas
  en el volumen (las sesiones sobreviven a reinicios del contenedor).
- **Security stamp**: cambiar la contraseña cierra todas las sesiones (cookies en la siguiente petición y
  pestañas del panel abiertas en menos de 1 minuto).
- Login con *rate limiting* (10 intentos/min por IP) y tiempo de respuesta uniforme para no revelar usuarios.
- Subidas validadas por **firma binaria** (JPG, PNG, WEBP, GIF; máx. 5 MB; SVG rechazado), nombres aleatorios,
  caché `immutable` y borrado del archivo cuando ningún producto lo usa.
- Antiforgery en formularios y logout por POST; cabeceras `X-Content-Type-Options`, `X-Frame-Options`,
  `Referrer-Policy`, `Permissions-Policy`.

---

## Configuración

Todas las claves se pueden sobrescribir con variables de entorno (`Seccion__Clave`). Con Docker se definen en
`.env` (`TEMPEST_*`, ver [`.env.example`](.env.example)), que `docker-compose.yml` traduce a estas claves.

| Clave | Por defecto | Descripción |
|---|---|---|
| `ConnectionStrings:Default` | `Data Source=data/tempest.db` | Ruta de SQLite (relativa a la raíz de contenido) |
| `Storage:UploadsPath` | `wwwroot/uploads` | Carpeta de imágenes subidas |
| `DataProtection:KeysPath` | `data/keys` | Claves que cifran las cookies |
| `Store:Name` / `Store:Tagline` | `TEMPEST` / `Anime · Gaming Store` | Marca en la navbar |
| `Store:Culture` | `es-MX` | Formato de precio (`es-CR` → ₡, `es-ES` → €, `es-CO`…) |
| `Seed:AdminUsername` / `Seed:AdminPassword` | `admin` / `admin` | Usuario inicial (solo si no hay usuarios) |
| `Seed:WhatsAppNumber` | vacío | Número inicial opcional |
| `Seed:SampleProducts` | `true` | Catálogo de ejemplo en el primer arranque |
| `Diagnostics:SimulatedQueryLatencyMs` | `0` | Solo para desarrollo: retrasa las queries para ver los skeletons |

> SQLite responde en ~1 ms en local, por eso los skeletons casi no se ven. Para verlos:
> `dotnet run --project src/WebUI -- --Diagnostics:SimulatedQueryLatencyMs=1500`

---

## Desarrollo

```bash
dotnet test                 # 67 pruebas: dominio, casos de uso con SQLite real y reglas de arquitectura
dotnet tool restore         # instala dotnet-ef (manifiesto local)
dotnet ef migrations add NombreMigracion --project src/Infrastructure --startup-project src/WebUI --output-dir Persistence/Migrations
```

Las migraciones se aplican automáticamente al iniciar la aplicación.

## Despliegue en un VPS (Docker + Nginx)

Pensado para un servidor que ya tiene Docker y Nginx (por ejemplo 4 núcleos / 8 GB) y lo comparte con otras
apps. El contenedor escucha solo en `127.0.0.1` y Nginx lo publica en un subdominio con HTTPS.

### Recursos

| Recurso | Límite (`.env`) | Medido |
|---|---|---|
| CPU | `TEMPEST_CPUS=1.5` | ~1 ms de CPU por página del catálogo (~2 500 páginas/s con 32 conexiones). Los picos son el arranque y los logins (hash PBKDF2). |
| RAM | `TEMPEST_MEMORY=768M` | ~125 MB en reposo y ~220 MB bajo carga. .NET ajusta su heap al 75 % del límite. |
| Logs | 3 × 10 MB | Rotación automática (definida en `docker-compose.yml`). |

Son techos, no reservas: el contenedor solo consume lo que usa. La construcción de la imagen (`--build`) usa
más CPU y RAM durante 1–3 minutos y no está sujeta a estos límites; hazla fuera de horas pico.

### Pasos

1. DNS: crea un registro **A** `tienda.tudominio.com` → IP del VPS.
2. Lleva el código al servidor:

   ```bash
   git clone https://github.com/<usuario>/<repo>.git /opt/tempest && cd /opt/tempest
   ```

3. Configura. **Antes del primer arranque** pon una contraseña larga en `TEMPEST_ADMIN_PASSWORD` (el usuario
   inicial solo se crea una vez). Si el puerto 5001 ya está ocupado, cambia `TEMPEST_HTTP_PORT`.

   ```bash
   cp .env.example .env && nano .env
   ```

4. Construye y arranca:

   ```bash
   docker compose up -d --build
   docker compose ps                         # STATUS debe pasar a "healthy" en ~20 s
   curl -s http://127.0.0.1:5001/healthz     # → Healthy
   docker stats --no-stream tempest-store    # consumo real frente al límite
   ```

5. Nginx: copia el sitio, edita `server_name` (y el puerto del `upstream` si lo cambiaste) y recarga.
   [`deploy/nginx/tempest.conf`](deploy/nginx/tempest.conf) ya incluye lo que Blazor necesita: WebSockets para el
   panel, `proxy_buffering off` para el streaming del catálogo y conexiones reutilizadas hacia Kestrel.

   ```bash
   sudo cp deploy/nginx/tempest.conf /etc/nginx/sites-available/tempest.conf
   sudo ln -s /etc/nginx/sites-available/tempest.conf /etc/nginx/sites-enabled/tempest.conf
   sudo nginx -t && sudo systemctl reload nginx
   ```

6. HTTPS con Let's Encrypt:

   ```bash
   sudo certbot --nginx -d tienda.tudominio.com
   ```

7. Entra a `https://tienda.tudominio.com/admin`, cambia la contraseña y configura el número de WhatsApp.

### Actualizar

```bash
cd /opt/tempest && git pull && docker compose up -d --build && docker image prune -f
```

Las migraciones se aplican solas al arrancar; los datos viven en los volúmenes y no se tocan.

### Persistencia y respaldos

Los datos **no viven dentro del contenedor** sino en dos volúmenes de Docker. Sobreviven a reinicios del
contenedor y del servidor, a `docker compose down` y a cada actualización (`up -d --build`):

| Volumen | Contenido |
|---|---|
| `tempest-store_tempest-data` | Base SQLite y claves de Data Protection (las sesiones siguen válidas tras reiniciar) |
| `tempest-store_tempest-uploads` | Imágenes subidas desde el panel |

Tras reiniciar el VPS, la tienda vuelve sola (`restart: unless-stopped`) si Docker arranca con el sistema:
`sudo systemctl enable docker`.

Lo único que borra los datos:

- `docker compose down -v`, `docker volume rm …`, `docker volume prune -a` o `docker system prune -a --volumes`.
- Cambiar `name:` en `docker-compose.yml`: los volúmenes cambian de nombre y la tienda arranca vacía (los datos
  siguen en los volúmenes viejos).
- Perder el disco del VPS. Para eso están los respaldos **fuera del servidor**.

**Desde el panel (/admin → Base de datos).** Muestra cuánto ocupa la base (incluido el WAL) y las imágenes,
permite **descargar un respaldo** `.zip` (copia consistente de la base + imágenes subidas, sin detener la tienda)
y **restaurarlo** subiendo ese mismo `.zip` (máx. 1 GB). Antes de restaurar valida que sea un respaldo íntegro de
esta tienda y guarda los datos actuales en `data/backups/before-restore-*.db`. El `.zip` no incluye las claves de
sesión: si el respaldo trae otros usuarios, hay que volver a iniciar sesión.

**Respaldo automático sin detener la tienda.** [`deploy/backup.sh`](deploy/backup.sh) pide a la propia app una copia
consistente de la base (SQLite `VACUUM INTO`, incluye lo que aún está en el WAL y verifica la integridad), le
suma las claves y las imágenes en un `.tar.gz` y borra los respaldos de más de 14 días:

```bash
sudo bash /opt/tempest/deploy/backup.sh      # → /var/backups/tempest/tempest-AAAAMMDD-HHMMSS.tar.gz
```

Para programarlo todos los días a las 4:00, agrega esta línea con `sudo crontab -e`:

```text
0 4 * * * bash /opt/tempest/deploy/backup.sh >> /var/log/tempest-backup.log 2>&1
```

Copia `/var/backups/tempest` fuera del VPS (otro servidor con `rsync`, almacenamiento en la nube con `rclone` o
los snapshots de Contabo). Los archivos incluyen las claves de sesión: guárdalos en un lugar privado.

**Restaurar** (detiene la tienda unos segundos y la vuelve a iniciar):

```bash
sudo bash /opt/tempest/deploy/restore.sh /var/backups/tempest/tempest-AAAAMMDD-HHMMSS.tar.gz
```

Haz una restauración de prueba al menos una vez: un respaldo que nunca se restauró no está comprobado. Guarda
también el archivo `.env` del servidor (no está en git).

### Notas

- Usa un **subdominio**: servir la app bajo una ruta (`tudominio.com/tienda`) requiere configurar `PathBase`.
- Si Nginx corre dentro de un contenedor, únelo a la red de este compose y usa `proxy_pass http://tempest-store:80`.
- Las claves de Data Protection se guardan sin cifrar dentro del volumen (aviso en el log al iniciar); para
  cifrarlas en reposo, configura un certificado con `ProtectKeysWithCertificate`.
- Imagen más pequeña y sin shell: `mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled-extra` (incluye ICU).
  Requiere crear las carpetas de los volúmenes en la etapa de build y copiarlas con `--chown`, y no admite el
  `HEALTHCHECK` actual (usa bash).
