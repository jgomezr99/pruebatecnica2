# ZiurInventario — Blazor Web App (.NET 10)

Aplicación Blazor Web App con renderizado interactivo en servidor. Consulta el endpoint REST configurado de Ziur y crea una grilla dinámica a partir del JSON recibido, sin asumir nombres de campos ni una estructura de datos no verificada.

## Requisitos

- Visual Studio con la carga de trabajo **ASP.NET y desarrollo web** y soporte para .NET 10, o SDK .NET 10.
- Acceso de red al servidor de Ziur.
- Credencial Bearer válida y permisos sobre el endpoint.

## Endpoint configurado

- URL base: `https://mainserver.ziursoftware.com/`
- Ruta: `Ziur.API/basedatos_01/ZiurServiceRest.svc/api/DocumentosFillsCombos`

La ruta y la URL base están en `appsettings.json`. El token **no** se guarda en ese archivo ni en el repositorio.

## Configurar el token en desarrollo

Abre una terminal en la carpeta donde está `ZiurInventario.csproj` y ejecuta:

```powershell
dotnet user-secrets set "ZiurApi:BearerToken" "PEGA_AQUI_TU_TOKEN_VALIDO"
```

El valor se guarda en User Secrets fuera del árbol del proyecto. No lo pegues en Razor, CSS, JavaScript, `appsettings.json` ni en Git.

En Visual Studio también puedes hacer clic derecho sobre el proyecto y elegir **Manage User Secrets** (Administrar secretos de usuario), y agregar:

```json
{
  "ZiurApi": {
    "BearerToken": "PEGA_AQUI_TU_TOKEN_VALIDO"
  }
}
```

Si una credencial real ya fue compartida en un chat o repositorio público, solicita su rotación al administrador.

## Ejecutar

### Visual Studio
1. Abre `ZiurInventario.csproj` o crea una solución y agrega el proyecto.
2. Configura el token en User Secrets.
3. Selecciona el perfil HTTPS y pulsa F5.

### CLI
```powershell
dotnet restore
dotnet user-secrets set "ZiurApi:BearerToken" "PEGA_AQUI_TU_TOKEN_VALIDO"
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --urls http://localhost:5000
```

Abre http://localhost:5000.

## Funciones incluidas

- Solicitud HTTP real al endpoint configurado.
- Token Bearer añadido desde el servidor.
- Detección dinámica de arrays/objetos JSON y creación de columnas sin nombres supuestos.
- Búsqueda en todos los valores.
- Ordenamiento ascendente/descendente por columna.
- Paginación de 10 filas.
- Estados de carga, error, resultado vacío y reintento.
- Mensajes diferenciados para HTTP 401/403, otros errores HTTP, timeout, problemas de red y JSON no válido.
- Diseño adaptable para escritorio y móvil.

## Consideración importante sobre el endpoint

El nombre `DocumentosFillsCombos` puede indicar que la respuesta contiene datos auxiliares para combos y no necesariamente movimientos de inventario. Este proyecto no inventa esa estructura: presenta dinámicamente el array de objetos más adecuado que encuentre en el JSON. Revisa el campo **Origen JSON** y el **Tipo de respuesta detectado** en pantalla. Si el servicio devuelve varios arrays independientes, hay que confirmar el contrato de Ziur y seleccionar el array correcto para mostrar el catálogo esperado.

## Seguridad para producción

- Mantén el token en variables de entorno o en un gestor de secretos de la plataforma de alojamiento.
- No expongas el token en el cliente ni en logs.
- Aplica autenticación y autorización de usuarios a la propia aplicación si se publica.
- Considera limitar el tamaño de respuestas y registrar diagnósticos sin datos sensibles.
- No publiques el token ni lo confirmes en el repositorio.

## Estructura y decisiones de diseño

| Carpeta | Responsabilidad |
|---|---|
| `Options/ZiurApiOptions.cs` | Configuración tipada (`BaseUrl`, `Endpoint`, `BearerToken`, `TimeoutSeconds`). |
| `Services/ZiurApiService.cs` | Llamada HTTP, manejo de errores y conversión del JSON a tabla. |
| `Models/ApiTableData.cs` | Resultado ya normalizado que consume la UI. |
| `Components/Pages/Home.razor` | Grilla con búsqueda, orden, paginación y estados. |

- **URL y endpoint** viven en `appsettings.json`; el **token** solo en User Secrets (desarrollo) o variable de entorno `ZiurApi__BearerToken` (producción). User Secrets solo se carga en entorno `Development`.
- El `HttpClient` se registra con `AddHttpClient<ZiurApiService>` y el token se añade en el servidor; nunca llega al navegador.
- Los errores se traducen a mensajes en español para el usuario y se registran con `ILogger` sin incluir datos sensibles.
- Las consultas se cancelan si el usuario recarga o abandona la página.
- `bin/`, `obj/` y secretos están excluidos en `.gitignore`.

## Pruebas automáticas

```powershell
dotnet test ..\ZiurInventario.Tests\ZiurInventario.Tests.csproj
```

Cubren el servicio con un `HttpMessageHandler` simulado: arrays de objetos y anidados, cuerpo vacío, HTTP 401/403/500, JSON inválido y token ausente.

## Verificación manual recomendada

1. Token válido: comprobar que se muestran los datos reales.
2. Token ausente o inválido: comprobar mensaje seguro sin exponer credenciales.
3. Endpoint inaccesible: comprobar mensaje de red o timeout y reintento.
4. Respuesta JSON vacía o con estructura inesperada: comprobar que la página sigue funcionando.
5. Buscar texto, ordenar por dos columnas distintas y navegar por páginas.
6. Probar la interfaz en una ventana estrecha y con muchas columnas.

El código se entrega como base funcional; el resultado real depende de que la API esté accesible y de que el token tenga permisos. Verificado con el servidor de Ziur: la consulta devuelve 39 registros (`Codigo`, `Descripcion`, `VActiva`).
