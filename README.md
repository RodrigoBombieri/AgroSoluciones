# 🚛 Sistema de Control de Balanza y Cartas de Porte (Agro-Industrial)

Este proyecto es una solución de software híbrida diseñada específicamente para optimizar la logística, el pesaje y la descarga de camiones en acopios de grano, molinos y plantas industriales. 

El sistema resuelve problemas críticos de conectividad en zonas rurales mediante un flujo de captura doble (Lector QR para Cartas de Porte Electrónicas y un módulo de IA con OCR inteligente como alternativa de contingencia cuando el QR no se puede leer).

---

## 🚀 Características Principales

* **Ingreso Híbrido de CPE:** Escaneo del código QR con la cámara o desde una imagen, o foto de la CPE con extracción automática de datos mediante **IA (Azure OpenAI con visión)**. Los datos leídos precompletan el formulario y el operario confirma.
* **Gestión de Estados por Roles:** Balanza Entrada ➡️ Playón/Descarga ➡️ Balanza Salida. Cada operario ve solo las pantallas de su rol.
* **Panel del Playón en Angular:** Pensado para celular/tablet. Muestra los camiones en espera y descargando, se actualiza solo cada 15 segundos y permite asignar silo y registrar observaciones.
* **Dashboard:** Camiones en planta por estado, ingresos y neto del día, demora promedio y alertas de diferencia contra lo declarado en la CPE.
* **Panel de Administración:** Historial de pesajes con filtros por fechas, productor, grano, patente, CPE y estado; exportación a CSV (Excel); ABM de proveedores, productos y usuarios.
* **Validaciones de negocio:** CUIT con dígito verificador, patentes AAA999 / AA999AA, CPE única, un camión no puede estar dos veces en planta, tara menor que bruto y tolerancia de diferencia configurable.
* **Funciona sin internet:** fuentes, íconos y librerías se sirven desde el propio sitio (solo la lectura por IA necesita conexión).

---

## 🛠️ Stack Tecnológico

* **Backend:** .NET 10.0 / C# (ASP.NET Core MVC + Identity con roles)
* **Frontend:** Razor Views + Angular 21 (panel del playón, montado dentro de una vista Razor)
* **Persistencia:** SQL Server mediante Entity Framework Core (Code First, migraciones)
* **Inteligencia Artificial:** Azure OpenAI (modelo con visión, respuesta en JSON estructurado)
* **QR:** html5-qrcode (en el navegador)

---

## 📐 Ciclo de vida del camión

```text
[ Camión llega ] ➡️ (Balanza Entrada: CPE + Peso Bruto) ➡️ [ En Espera ] ➡️ (Playón: silo) ➡️ [ En Descarga ]
   ➡️ (Playón: fin de descarga) ➡️ [ Listo para Salir ] ➡️ (Balanza Salida: Peso Tara) ➡️ [ Completado ] ➡️ Ticket
```

Todas las transiciones pasan por `Services/PesajeWorkflowService.cs`, que valida el estado actual y registra el usuario y la hora de cada etapa. El peso neto lo calcula SQL Server (columna calculada `PesoBruto - PesoTara`).

| Rol | Pantallas |
|---|---|
| `BalanzaEntrada` | Balanza entrada (lectura de CPE + peso bruto) |
| `Playon` | Panel del playón (Angular) |
| `BalanzaSalida` | Balanza salida (tara) y ticket imprimible |
| `Admin` | Todo, más reportes, correcciones, proveedores, productos y usuarios |

Todos los usuarios logueados ven el dashboard y la lista de camiones en planta. No hay registro público: los usuarios los crea un Admin en **Administración → Usuarios** (desactivar un usuario bloquea su ingreso sin borrar su historial).

---

## ▶️ Puesta en marcha

1. **Connection string:** en `SolucionesAgro/ControlBalanza/appsettings.json` → `ConnectionStrings:DbConnection`.
2. **Ejecutar:** abrir `SolucionesAgro/SolucionesAgro.slnx` en Visual Studio y presionar **F5**. Al arrancar, la app aplica las migraciones pendientes y crea los roles y datos iniciales.
3. **Ingresar** con `admin@admin.com` / `Admin123`.

En entorno **Development** se crean además usuarios de prueba (contraseña `Agro1234`): `entrada@agro.local`, `playon@agro.local` y `salida@agro.local`.

> La cámara para leer el QR solo funciona con **HTTPS** (o `localhost`). Usar el perfil `https` de `launchSettings.json`.

### Configurar la lectura con IA (opcional)

Se necesita un recurso de Azure OpenAI con un deployment de un modelo con visión (por ejemplo `gpt-4o-mini`). Para no subir la clave a Git, usar *user-secrets*.

**Dónde:** terminal (PowerShell) en la carpeta `SolucionesAgro\ControlBalanza`:

```powershell
dotnet user-secrets init
dotnet user-secrets set "AzureOpenAI:Endpoint" "https://TU-RECURSO.openai.azure.com/"
dotnet user-secrets set "AzureOpenAI:ApiKey" "TU-CLAVE"
dotnet user-secrets set "AzureOpenAI:Deployment" "gpt-4o-mini"
```

Sin esta configuración el botón "Leer con IA" aparece deshabilitado y el resto funciona igual.

### Parámetros de balanza

En `appsettings.json`, sección `Balanza`:

* `ToleranciaDiferenciaPorcentaje` (por defecto `0.5`): diferencia máxima entre el neto y lo declarado en la CPE antes de marcar alerta.
* `PesoMaximoKg` (por defecto `75000`): tope del peso bruto para detectar errores de tipeo.

### Recompilar el panel Angular

La app ya incluye el panel compilado en `wwwroot/angular-app`. Si se modifica el código de `ClientApp`:

**Dónde:** terminal en la carpeta `SolucionesAgro\ControlBalanza\ClientApp`:

```powershell
npm install      # solo la primera vez o si cambia package.json
npm run build    # compila a ..\wwwroot\angular-app
npm test         # tests del panel (Vitest)
```

---

## 🗂️ Estructura principal

```text
ControlBalanza/
├─ Controllers/          MVC (Pesajes, Reportes, Usuarios, Proveedores, Productos, Home)
│  └─ Api/PlayonController.cs   API JSON del panel del playón (cookie + antiforgery)
├─ Services/             Flujo de estados, reportes, lectura de CPE (QR / IA)
├─ Validacion/           CUIT y patentes
├─ Seguridad/Roles.cs    Nombres de los roles
├─ Areas/Identity/       Login en español (reemplaza al de Identity UI)
├─ Migrations/           Migraciones de EF Core
├─ ClientApp/            Código fuente Angular (panel del playón)
└─ wwwroot/              Estáticos (incluye la build de Angular, fuentes y librerías)
```

---

## 📝 Pendientes conocidos

* **Formato del QR de la CPE:** AFIP no documenta un formato único del QR. El parser (`Services/Cpe/CpeQrParser.cs`) acepta JSON, URL con parámetros (incluido Base64) y texto libre. Hay que probarlo con una CPE real y, si hace falta, agregar las claves exactas en `ClavesConocidas`.
* La lectura con IA necesita conexión a internet; ante un corte, queda el QR o la carga manual.
