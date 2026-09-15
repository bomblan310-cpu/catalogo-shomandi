# Shomandi · catálogo de celulares

API y storefront web para publicar celulares, consultar el catálogo y recibir pedidos por WhatsApp. El proyecto no utiliza carrito de compras: cada producto puede enviarse directamente a WhatsApp con sus datos y variante seleccionada.

Está construido con **ASP.NET Core 8**, **Entity Framework Core**, **SQLite/PostgreSQL** y **Cloudinary** para las imágenes.

## Funcionalidades

- Catálogo público con búsqueda, filtros por marca/categoría, rango de precios y destacados.
- Detalle de producto con variantes, stock e imágenes.
- Generación de mensajes y enlaces de pedido para WhatsApp.
- Panel de administración para crear, editar y desactivar productos.
- Carga y eliminación de imágenes mediante Cloudinary.
- Ajuste de stock por variante.
- SQLite local para desarrollo y PostgreSQL para producción.
- Health checks en `/health` y `/ping`.

## Páginas

La aplicación sirve sus páginas estáticas desde `wwwroot`:

| Página | Ruta |
| --- | --- |
| Inicio | `/` |
| Catálogo | `/catalog.html` |
| Panel de administración | `/admin.html` |

## Requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Git
- Opcional: una cuenta de Cloudinary para imágenes y PostgreSQL/Neon para producción.

## Ejecutar localmente

```bash
git clone <URL_DEL_REPOSITORIO>
cd catalogo-shomandi
dotnet restore
dotnet run --launch-profile http
```

Después, abre la URL que muestra la consola (normalmente `http://localhost:5xxx`). También puedes probar:

```text
http://localhost:5xxx/catalog.html
http://localhost:5xxx/health
```

En desarrollo, si no existe una cadena de conexión válida, la aplicación usa automáticamente `mobilecatalog.local.db` (SQLite), crea las tablas y carga datos de ejemplo. Para comenzar desde cero, detén la aplicación y elimina ese archivo.

## Configuración

Las opciones se leen desde `appsettings.json`, User Secrets y variables de entorno. No guardes contraseñas, claves de administrador ni secretos de Cloudinary en Git.

| Variable | Uso |
| --- | --- |
| `ConnectionStrings__CatalogDatabase` | PostgreSQL/Neon. Si está vacío o contiene `CHANGE_ME`, se usa SQLite local. |
| `WhatsApp__BaseUrl` | Base para generar enlaces; normalmente `https://wa.me`. |
| `WhatsApp__PhoneNumber` | Número con código de país, sin `+` ni espacios. |
| `Admin__Username` | Usuario del panel web. |
| `Admin__PasswordHash` | Hash generado con `PasswordHasher<string>`. |
| `Admin__ApiKey` | Clave para integraciones que usan `X-Admin-Key`. |
| `Cloudinary__CloudName` | Cloud name de Cloudinary. |
| `Cloudinary__ApiKey` | API key de Cloudinary. |
| `Cloudinary__ApiSecret` | API secret de Cloudinary. |

Para configurar credenciales locales sin escribirlas en archivos del proyecto:

```bash
dotnet user-secrets set "Admin:Username" "admin"
dotnet user-secrets set "Admin:PasswordHash" "<HASH_GENERADO>"
dotnet user-secrets set "Admin:ApiKey" "<CLAVE_LOCAL>"
```

El panel usa una sesión con cookie HttpOnly, SameSite=Strict, duración de 30 minutos y límite de intentos de inicio de sesión. Los endpoints administrativos requieren sesión o, para integraciones compatibles, el header `X-Admin-Key`.

## API principal

### Catálogo público

```http
GET /api/products
GET /api/products/{id}
GET /api/products/{id}/whatsapp?phone=595...&variantId=...
GET /api/catalog/categories
GET /api/catalog/brands
GET /api/catalog/contact
```

`GET /api/products` acepta `categoryId`, `brandId`, `minPrice`, `maxPrice`, `search`, `featured`, `page` y `pageSize`.

### Administración

```http
POST   /api/admin/auth/login
GET    /api/admin/auth/session
POST   /api/admin/auth/logout
POST   /api/admin/products
PUT    /api/admin/products/{id}
DELETE /api/admin/products/{id}
POST   /api/admin/products/{id}/images
DELETE /api/admin/products/{id}/images/{imageId}
POST   /api/admin/products/{id}/variants/{variantId}/stock/increase
POST   /api/admin/products/{id}/variants/{variantId}/stock/decrease
```

La carga de imágenes usa multipart/form-data con el campo `file`. El login recibe JSON con `username` y `password`.

El esquema inicial también está disponible en [`Database/001_catalog.sql`](Database/001_catalog.sql).

## Despliegue en Render

El archivo [`render.yaml`](render.yaml) define un servicio Docker, una base PostgreSQL y el health check `/health`.

1. Sube el repositorio a GitHub.
2. En Render, selecciona **New → Blueprint** y conecta el repositorio.
3. Completa las variables secretas: `Admin__Username`, `Admin__PasswordHash`, `Admin__ApiKey` y las tres de Cloudinary.
4. Espera el despliegue y prueba `/`, `/catalog.html` y `/health`.

La cadena de conexión de PostgreSQL se inyecta automáticamente desde la base declarada en `render.yaml`.

### Mantener activo un servicio gratuito

El workflow [`.github/workflows/keep-alive.yml`](.github/workflows/keep-alive.yml) llama a `/ping` periódicamente. Configura en GitHub la variable `APP_URL` con la URL pública, sin `/` final:

```text
https://shomandi-catalog.onrender.com
```

Esto puede reducir los tiempos de arranque, pero Render Free y GitHub Actions no garantizan disponibilidad permanente.

## Estructura del proyecto

```text
Controllers/   Endpoints de la API
Data/          DbContext y datos iniciales
Models/        Entidades y DTOs
Services/      WhatsApp, Cloudinary, slugs y seguridad
Database/      Script SQL del esquema
wwwroot/       Storefront y panel web
Program.cs     Configuración y arranque
```

## Docker

```bash
docker build -t shomandi-catalog .
docker run --rm -p 8080:8080 shomandi-catalog
```

Para producción, inyecta las variables de entorno descritas arriba y no copies secretos dentro de la imagen.

## Licencia

Actualmente este repositorio no declara una licencia. Si el proyecto será reutilizado por terceros, añade un archivo `LICENSE` con la licencia elegida.
