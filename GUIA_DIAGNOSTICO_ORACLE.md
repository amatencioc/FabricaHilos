# 🔍 GUÍA DE USO - ENDPOINT DE DIAGNÓSTICO ORACLE

## ✅ **Endpoint Creado Exitosamente**

Se ha creado un endpoint de diagnóstico para probar la conexión a Oracle y validar usuarios.

---

## 🚀 **CÓMO USARLO**

### **Paso 1: Ejecutar la Aplicación**

```powershell
cd D:\.Net\FabricaHilos\FabricaHilos
dotnet run
```

Espera a ver:
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:7777
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
```

---

### **Paso 2: Abrir el Endpoint en el Navegador**

#### **Opción A: Probar con VENTAS7 (valores por defecto)**
```
https://localhost:7777/Account/TestOracle
```

#### **Opción B: Probar con cualquier usuario**
```
https://localhost:7777/Account/TestOracle?usuario=VENTAS7&password=460910
```

#### **Opción C: Probar con VICMATE**
```
https://localhost:7777/Account/TestOracle?usuario=VICMATE&password=ANGELO1006
```

---

## 📊 **QUÉ VERÁS EN EL NAVEGADOR**

El endpoint muestra una página HTML completa con:

### **1️⃣ CONFIGURACIÓN**
- ✅ Cadena de conexión encontrada
- Muestra la cadena completa a Oracle

### **2️⃣ PRUEBA DE CONEXIÓN**
- ⏱️ Tiempo de respuesta en milisegundos
- ✅ Conexión exitosa / ❌ Error de conexión

### **3️⃣ DATOS DEL USUARIO** (si existe)
- **c_user**: Nombre de usuario
- **psw_sig**: Contraseña (oculta con *)
- **c_costo**: Centro de costo

### **4️⃣ QUERY SQL EJECUTADO**
- Muestra el query SQL exacto
- Muestra los parámetros usados

### **5️⃣ RESULTADO FINAL**
- ✅ Usuario existe y funciona
- ❌ Usuario no encontrado (con posibles causas)
- ❌ Error de conexión (con detalles del error)

---

## 🎯 **EJEMPLOS DE USO**

### **Ejemplo 1: Probar VENTAS7**
```
https://localhost:7777/Account/TestOracle?usuario=VENTAS7&password=460910
```

**Si existe:**
```
✅ CONEXIÓN EXITOSA
✅ USUARIO ENCONTRADO EN ORACLE

Usuario (c_user): VENTAS7
Centro de Costo (c_costo): VENTAS
```

**Si no existe:**
```
❌ CONEXIÓN EXITOSA PERO USUARIO NO ENCONTRADO

POSIBLES CAUSAS:
- El usuario 'VENTAS7' no existe en la tabla cs_user
- La contraseña '460910' es incorrecta
- Hay diferencias de mayúsculas/minúsculas
```

---

### **Ejemplo 2: Probar VICMATE**
```
https://localhost:7777/Account/TestOracle?usuario=VICMATE&password=ANGELO1006
```

---

### **Ejemplo 3: Probar error de conexión**
Si Oracle no está disponible:
```
❌ ERROR DE CONEXIÓN

Tipo: OracleException
Mensaje: ORA-12170: TNS:Connect timeout occurred
```

---

## 🔧 **CARACTERÍSTICAS DEL ENDPOINT**

✅ **No requiere login** - Usa `[AllowAnonymous]`
✅ **Muestra información detallada** - Diagnóstico completo
✅ **Interfaz visual** - HTML con Bootstrap 5
✅ **Seguro** - No expone contraseñas (usa asteriscos)
✅ **Mide rendimiento** - Muestra tiempo de respuesta
✅ **Manejo de errores** - Muestra excepciones detalladas

---

## 📋 **FLUJO DE DIAGNÓSTICO**

```
1. Accedes al endpoint
         ↓
2. Lee la configuración (appsettings.json)
         ↓
3. Intenta conectarse a Oracle
         ↓
4. Ejecuta el query SQL con el usuario
         ↓
5. Muestra los resultados:
   - ✅ Usuario encontrado → Muestra datos
   - ❌ Usuario no encontrado → Muestra causas
   - ❌ Error de conexión → Muestra error
```

---

## 🎨 **DISEÑO VISUAL**

El endpoint muestra una página moderna con:
- 🎨 Fondo degradado morado/azul
- 📦 Card con sombras
- ✅ Badges de color (verde = éxito, rojo = error, azul = info)
- 📝 Código SQL formateado
- 📊 Tabla de datos estructurada

---

## ⚙️ **CÓDIGO DEL ENDPOINT**

El endpoint está en:
```
FabricaHilos/Controllers/AccountController.cs
```

Método:
```csharp
[HttpGet]
[AllowAnonymous]
public IActionResult TestOracle(string usuario = "VENTAS7", string password = "460910")
```

---

## 🔍 **CASOS DE USO**

### **Usar este endpoint cuando:**
1. ✅ Quieras probar si Oracle está disponible
2. ✅ Necesites verificar si un usuario existe
3. ✅ Quieras ver el tiempo de respuesta de Oracle
4. ✅ Estés diagnosticando problemas de login
5. ✅ Quieras ver los datos exactos de un usuario

---

## 🚀 **PRUEBA AHORA**

1. **Ejecuta:**
```powershell
cd D:\.Net\FabricaHilos\FabricaHilos
dotnet run
```

2. **Abre en el navegador:**
```
https://localhost:7777/Account/TestOracle
```

3. **Verás los resultados de VENTAS7/460910 automáticamente**

---

## 💡 **TIPS**

- El endpoint usa los mismos métodos que el login real
- Si funciona aquí, funcionará en el login
- Puedes probar múltiples usuarios cambiando la URL
- Los resultados se muestran en tiempo real
- Los logs también aparecen en la terminal

---

## 🎯 **SIGUIENTE PASO**

Después de probar este endpoint:

1. Si **VENTAS7 existe** → Usa esas credenciales en el login normal
2. Si **VENTAS7 no existe** → Verifica en Oracle qué usuarios existen
3. Si **hay error de conexión** → Verifica la conectividad de red a Oracle

---

**¡El endpoint está listo para usar!** 🎉
